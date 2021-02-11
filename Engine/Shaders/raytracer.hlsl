
#if 0
$ubershader		RAYTRACE
#endif

#include "auto/raytracer.fxi"

#include "collision.fxi"

/*------------------------------------------------------------------------------
	Raytracer utilities :
------------------------------------------------------------------------------*/

uint wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

RAY CreateRay( uint2 xy )
{
	float 	x 	=	( xy.x )		/ 800.0 * 2 - 1;
	float 	y 	=	( 600-xy.y ) 	/ 600.0 * 2 - 1;
	float 	z	=	1.0f;//(wang_hash( 199*xy.x + 2999*xy.y ) & 0xF) / 64.0f + 1.0f;
	float3 	p 	=	Camera.CameraPosition.xyz;
	float3  d 	=	Camera.CameraForward.xyz * z + Camera.CameraRight.xyz * x + Camera.CameraUp.xyz * y;
	return ConstructRay( p, normalize(d) );
}

void UnpackBVHNode( BvhNode node, out float3 minBound, out float3 maxBound, out uint index, out uint isLeaf )
{
#if 0
	minBound.x	=	f16tof32( node.PackedMinMaxIndex.x >> 16 );
	minBound.y	=	f16tof32( node.PackedMinMaxIndex.x >>  0 );
	minBound.z	=	f16tof32( node.PackedMinMaxIndex.y >> 16 );
	maxBound.x	=	f16tof32( node.PackedMinMaxIndex.y >>  0 );
	maxBound.y	=	f16tof32( node.PackedMinMaxIndex.z >> 16 );
	maxBound.z	=	f16tof32( node.PackedMinMaxIndex.z >>  0 );
	isLeaf		=	node.PackedMinMaxIndex.w & 0x80000000;
	index		=	node.PackedMinMaxIndex.w & 0x7FFFFFFF;
#else
	minBound	=	node.BBoxMin.xyz;
	maxBound	=	node.BBoxMax.xyz;
	isLeaf		=	node.Index & 0x80000000;
	index		=	node.Index & 0x7FFFFFFF;
#endif	
}


/*------------------------------------------------------------------------------
	Raytracer utilities :
------------------------------------------------------------------------------*/

#ifdef RAYTRACE

#define STACKSIZE			32
#define STACKPUSH(index) 	stack[stackIndex++] = index
#define STACKPOP 			stack[--stackIndex]
#define STACKEMPTY			(stackIndex==0)
#define STACKGUARD			if (stackIndex>=STACKSIZE) return;

[numthreads(TileSize,TileSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint2 storeXY	=	dispatchThreadId.xy;
	RAY ray			=	CreateRay( dispatchThreadId.xy );
	uint dummy;
	
	float4 result	=	float4(0,0,0,9999999);
	
	uint stack[STACKSIZE];
	uint stackIndex = 0;
	uint maxIndex = 0;
	STACKPUSH(0);
	
	while (!STACKEMPTY)
	{
		STACKGUARD
		
		maxIndex = max(maxIndex, stackIndex);
		
		uint current = STACKPOP;
		BvhNode node = RtBvhTree[current];
		float tmin, tmax;
		float3 minBound, maxBound;
		uint index, isLeaf;
		
		UnpackBVHNode( node, minBound, maxBound, index, isLeaf );
		
		if ( RayAABBIntersection( ray, minBound, maxBound, tmin, tmax ) )
		{
			if (tmax>0 && tmin < result.w) 
			{
				if (isLeaf) 
				{
					Triangle tri = RtTriangles[ index ];
					
					float t;
					float2 uv;
	
					if ( RayTriangleIntersection( ray, tri.Point0.xyz, tri.Point1.xyz, tri.Point2.xyz, tri.PlaneEq, t, uv ) )
					{
						if (result.w>t)
						{
							result.xyz 	= lerp(result.xyz, (tri.PlaneEq.xyz*0.5+0.5) * float3(uv,1), 1);
							result.w	= t;
						}
					}
				}
				else
				{
					STACKPUSH(index);
					STACKPUSH(current+1);
				}
			}
		}
	}
	
	GroupMemoryBarrierWithGroupSync();
	
	RaytraceImage[ storeXY ] = float4(result.rgb,1);
}

#endif













