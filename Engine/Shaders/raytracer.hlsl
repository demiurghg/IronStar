
#if 0
$ubershader		RAYTRACE
#endif

#include "auto/raytracer.fxi"

/*------------------------------------------------------------------------------
	UTILS :
------------------------------------------------------------------------------*/

RAY ConstructRay( float3 origin, float3 dir )
{
	RAY r;
	r.orig		=	origin;
	r.dir		=	dir;
	r.invdir	=	1.0f / dir;
	r.norm		=	float3(0,0,0);
	r.uv		=	float2(0,0);
	r.time		=	9999999;
	r.index		=	-1;
	return r;
}

void swap(inout float a, inout float b)
{
	float t = a;
	a = b;
	b = t;
}

/*------------------------------------------------------------------------------
	Ray / AABB intersection
------------------------------------------------------------------------------*/

//
//	https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-box-intersection
//
bool RayAABBIntersection(RAY r, float3 aabbMin, float3 aabbMax, out float tmin, out float tmax) 
{ 
	tmin = (aabbMin.x - r.orig.x) * r.invdir.x; 
	tmax = (aabbMax.x - r.orig.x) * r.invdir.x; 
 
	if (tmin > tmax) swap(tmin, tmax); 
 
	float tymin = (aabbMin.y - r.orig.y) * r.invdir.y; 
	float tymax = (aabbMax.y - r.orig.y) * r.invdir.y; 
 
	if (tymin > tymax) swap(tymin, tymax); 
 
	if ((tmin > tymax) || (tymin > tmax)) 
		return false; 
 
	if (tymin > tmin) 
		tmin = tymin; 
 
	if (tymax < tmax) 
		tmax = tymax; 
 
	float tzmin = (aabbMin.z - r.orig.z) * r.invdir.z; 
	float tzmax = (aabbMax.z - r.orig.z) * r.invdir.z; 
 
	if (tzmin > tzmax) swap(tzmin, tzmax); 
 
	if ((tmin > tzmax) || (tzmin > tmax)) 
		return false; 
 
	if (tzmin > tmin) 
		tmin = tzmin; 
 
	if (tzmax < tmax) 
		tmax = tzmax; 
 
	return true; 
} 

/*------------------------------------------------------------------------------
	Ray / Triangle intersection
------------------------------------------------------------------------------*/

#define EPSILON 0.00001

float3 Barycentric(float3 p, float3 a, float3 b, float3 c )
{
    float3 v0 = b - a; 
	float3 v1 = c - a; 
	float3 v2 = p - a;
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);
    float denom = d00 * d11 - d01 * d01;
    float v = (d11 * d20 - d01 * d21) / denom;
    float w = (d00 * d21 - d01 * d20) / denom;
    float u = 1.0f - v - w;
	return float3(u,v,w);
}

bool RayTriangleIntersection( inout RAY r, TRIANGLE tri, int index )
{
	float  t 		= 0;
	float2 uv 		= float2(0,0);
	
	float4 plane	=	tri.PlaneEq;
	float3 a		=	tri.Point0.xyz;
	float3 b		=	tri.Point1.xyz;
	float3 c		=	tri.Point2.xyz;
	
	// 	ray and triangle are parallel 
	//	or ray comes from behind:
	if ( dot(r.dir, plane.xyz) > EPSILON )
	{
		return false;
	}
	
	t = - (plane.w + dot(r.orig, plane.xyz)) / dot(r.dir, plane.xyz);
	
	if (t<0)
	{
		return false;
	}

	float3 p 	=	r.orig + r.dir * t;
	
	uv = Barycentric( p, a, b, c ).xy;
	
	if (uv.x<0 || uv.y<0 || uv.x + uv.y>1 )
	{
		return false;
	}
	
	r.time	=	t;
	r.uv	=	uv;
	r.index	=	index;
	r.norm	=	plane.xyz;
	
	return true;
}

/*------------------------------------------------------------------------------
	BVH UTILS :
------------------------------------------------------------------------------*/

void UnpackBVHNode( BVHNODE node, out float3 minBound, out float3 maxBound, out uint index, out uint isLeaf )
{
	minBound	=	node.BBoxMin.xyz;
	maxBound	=	node.BBoxMax.xyz;
	isLeaf		=	node.Index & 0x80000000;
	index		=	node.Index & 0x7FFFFFFF;
}

/*------------------------------------------------------------------------------
	Ray Tracer Core :
------------------------------------------------------------------------------*/

#define RT_STACKSIZE			32
#define RT_STACKPUSH(index) 	stack[stackIndex++] = index
#define RT_STACKPOP 			stack[--stackIndex]
#define RT_STACKEMPTY			(stackIndex==0)
#define RT_STACKGUARD			if (stackIndex>=RT_STACKSIZE) return false;

bool RayTrace( inout RAY ray, StructuredBuffer<TRIANGLE> tris, StructuredBuffer<BVHNODE> tree )
{
	uint stack[RT_STACKSIZE];
	uint stackIndex = 0;
	uint maxIndex = 0;
	
	RT_STACKPUSH(0);
	
	while (!RT_STACKEMPTY)
	{
		RT_STACKGUARD
		
		maxIndex = max(maxIndex, stackIndex);
		
		uint current = RT_STACKPOP;
		BVHNODE node = tree[current];
		float tmin, tmax;
		float3 minBound, maxBound;
		uint index, isLeaf;
		
		UnpackBVHNode( node, minBound, maxBound, index, isLeaf );
		
		if ( RayAABBIntersection( ray, minBound, maxBound, tmin, tmax ) )
		{
			if (tmax>0 && tmin < ray.time) 
			{
				if (isLeaf) 
				{
					TRIANGLE tri = tris[ index ];
					
					if ( RayTriangleIntersection( ray, tri, index ) )
					{
					}
				}
				else
				{
					RT_STACKPUSH(index);
					RT_STACKPUSH(current+1);
				}
			}
		}
	}
	
	return (ray.index>=0);
}

/*------------------------------------------------------------------------------
	Raytracer utilities :
------------------------------------------------------------------------------*/

#ifdef RAYTRACE

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
	
	RayTrace( ray, RtTriangles, RtBvhTree );
	
	GroupMemoryBarrierWithGroupSync();
	
	if (ray.index>=0)
	{
		RaytraceImage[ storeXY ] = float4((ray.norm*0.5+0.5) * float3(ray.uv,1),1);
	}
}

#endif




