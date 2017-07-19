
#if 0
// $ubershader 	INTERLEAVE	
// $ubershader 	DEINTERLEAVE
$ubershader 	HDAO		
// $ubershader 	BILATERAL_X	
// $ubershader 	BILATERAL_Y	
#endif

//-----------------------------------------------------------------------------

#include "hdao.auto.hlsl"

#define OverlapX ((BlockSizeX)/2)
#define OverlapY ((BlockSizeY)/2)

cbuffer CBParams : register(b0) {
	Params	params;
};

Texture2D<float4> 	source  : register(t0); 
RWTexture2D<float4> target  : register(u0); 

struct CachedPosition {
	uint xy;
	float z;
};

groupshared CachedPosition cachedPosition[BlockSizeX*2][BlockSizeY*2];

//-----------------------------------------------------------------------------

float LinearizeDepth(float z)
{
	return 1.0f / (z * params.LinDepthScale + params.LinDepthBias);
}

uint Float2ToUint ( float2 xy )
{
	return f32tof16(xy.x) | (f32tof16(xy.y)<<16);
}

float2 UintToFloat2( uint xy )
{
	float x = f16tof32((xy)     & 0x0000FFFF);
	float y = f16tof32((xy>>16) & 0x0000FFFF);
	return float2(x,y);
}

float3 FetchPositionFromCache ( int2 xy )
{
	CachedPosition cp = cachedPosition[xy.y][xy.x];
	return float3( UintToFloat2(cp.xy), cp.z );
}

float3 GetViewspacePosition( float z, float2 xy )
{
	float2	tg	=	float2( params.CameraTangentX, params.CameraTangentY );
	return float3( xy * tg * z, z );
}


# define NUM_VALLEYS	16
static const int2		samplePattern[NUM_VALLEYS] =
{
  {0, -7}, {4, -7}, {2, -6}, {6, -6},
  {0, -3}, {4, -3}, {8, -3}, {2, 0},
  {6, 0}, {7, 0}, {4, 3}, {8, 3},
  {2, 6}, {6, 6}, {7, 6}, {4, 7},
};

//-----------------------------------------------------------------------------


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location		=	dispatchThreadId.xy;
	int2 blockSize		=	int2(BlockSizeX,BlockSizeY);
	uint threadCount 	= 	BlockSizeX * BlockSizeY; 
	
	float distanceScale	=	length( float3( params.CameraTangentX, params.CameraTangentY, 1 ) );
	
	float2	projLocation	=	location.xy * params.InputSize.zw * 2 - 1;
			projLocation.y	*=	-1;
	
	//	PLAN: 
	//	+	location -> [-1,+1]
	//	+	restore view position
	//	+	debug view position (to texture)
	//	+	store in cache and debug again
	//	-	perform HDAO kernel
	
	//----------------------------------------------
	//	Load 64x64 block to group-shared memory :
	//----------------------------------------------
	
	[unroll]
	for (int i=0; i<2; i++) {
		
		[unroll]
		for (int j=0; j<2; j++) {
		
			int2 topLeft	=	groupId.xy * blockSize.xy - int2(OverlapX,OverlapY);
			int3 loadPoint 	= 	int3( topLeft + groupThreadId.xy*2 + int2(i,j), 0 );
			int2 storePoint	= 	int2( groupThreadId.xy*2    + int2(i,j) );
			
			float2	xy		=	loadPoint.xy * params.InputSize.zw * 2 - 1;
			
			float 	z		=	LinearizeDepth ( source.Load(loadPoint).r );
			float3 	xyz		=	GetViewspacePosition ( z, xy );
			
			CachedPosition cp;
			
			cp.xy	=	Float2ToUint( xyz.xy );
			cp.z	=	z;

			cachedPosition[ storePoint.y ][ storePoint.x ] = cp;
		}
	}
	
	GroupMemoryBarrierWithGroupSync();
	
	
	//----------------------------------------------
	//	Find valleys :
	//----------------------------------------------
	

	int2 	cacheCenter		=	groupThreadId.xy + int2(OverlapX, OverlapY);
	float3	centerPosition	=	FetchPositionFromCache( cacheCenter );
	//float	centerDistance	=	length(centerPosition);
	float	centerDistance	=	centerPosition.z * distanceScale;
	float	occlusion		=	0.0f;

	[branch]
	if ( centerPosition.z < params.DiscardDistance ) {

		[unroll]
		for (int i=0; i<NUM_VALLEYS; i++) {

			int2 	fetch0		=	cacheCenter + samplePattern[i];
			int2 	fetch1		=	cacheCenter - samplePattern[i];
			float3	position0	=	FetchPositionFromCache( fetch0 );
			float3	position1	=	FetchPositionFromCache( fetch1 );
			// float	distance0	=	length(position0);
			// float	distance1	=	length(position1);
			float	distance0	=	position0.z * distanceScale;
			float	distance1	=	position1.z * distanceScale;
			
			float	delta0		=	centerDistance - distance0;
			float	delta1		=	centerDistance - distance1;
			
			float	weight0		=	(delta0 > params.AcceptRadius) ? saturate( (params.RejectRadius - delta0) * params.RejectRadiusRcp ) : 0;
			float	weight1		=	(delta1 > params.AcceptRadius) ? saturate( (params.RejectRadius - delta1) * params.RejectRadiusRcp ) : 0;
			
			float3	direction0	=	normalize(centerPosition - position0);
			float3	direction1	=	normalize(centerPosition - position1);
			
			float	valleyDot	=	saturate(dot(direction0, direction1) + 0.9f) * 1.2f;
			
			occlusion	+=	( weight0 * weight1 * valleyDot * valleyDot * valleyDot);
		}
		
		occlusion /= (float)NUM_VALLEYS;
		
	} else {
	
		occlusion	=	0.5f;
	
	}
	
	target[location.xy] = 1-occlusion.xxxx;
	
	//target[location.xy]	= float4( frac(GetViewspacePosition( LinearizeDepth(source.Load(int3(location,0))), projLocation )), 1 );
}



