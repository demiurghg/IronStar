
#if 0
$ubershader 	INTERLEAVE	
$ubershader 	BILATERAL VERTICAL|HORIZONTAL
$ubershader 	HDAO LOW|MEDIUM|HIGH|ULTRA
#endif

//-----------------------------------------------------------------------------

#include "hdao.auto.hlsl"

#define OverlapX ((BlockSizeX)/2)
#define OverlapY ((BlockSizeY)/2)

#ifdef HDAO
cbuffer CBParams : register(b0) {
	HdaoParams	hdaoParams;
};
#endif

#ifdef BILATERAL
cbuffer CBParams : register(b0) {
	FilterParams	filterParams;
};
#endif

struct CachedPosition {
	uint xy;
	float z;
};

#ifdef HDAO
groupshared CachedPosition cachedPosition[BlockSizeX*2][BlockSizeY*2];
#endif

#if defined(HIGH) || defined(ULTRA)

static const int NUM_VALLEYS	=	16;

static const int2 samplingPattern[16][16] = {
	{ 8, -7,0, -7,5, -2,11, 2,-3, 3,13, -2,6, 6,2, 13,-7, 12,8, 11,-12, 0,-8, -3,-8, -10,-1, -1,0, -14,-11, 7 },
	{ -14, -1,-6, -1,-11, -8,-6, 4,0, -8,-6, -13,6, -8,0, -14,8, 0,2, 0,-13, 4,0, 5,8, 9,-4, 10,12, -5,2, 10 },
	{ 13, 6,7, 1,13, -4,6, 11,-4, 7,1, 13,10, -10,3, -4,1, -11,-14, 3,-11, -2,0, 0,-5, -2,-10, -8,-8, 11,-6, -13 },
	{ -11, 3,-3, 8,-5, -2,-12, -7,0, -8,3, 1,-8, -12,6, -5,5, -12,9, 2,12, -5,-9, 10,0, 14,-1, -14,3, 7,8, 10 },
                                                                                                               
	{ 7, 12,12, 8,-1, 10,6, 2,-2, 0,-10, 3,-11, 9,1, 5,-10, -8,0, -7,6, -4,12, 1,-3, -13,7, -10,2, -14,-14, -2 },
	{ 13, -5,14, 2,4, -13,7, -5,7, 2,-1, -6,-1, 1,-10, -8,-7, -2,-1, -14,-4, 6,-9, 9,4, 9,-11, 2,-1, 12,-14, -2 },
	{ -3, 4,3, 12,0, -4,6, -1,-12, 1,-6, -2,3, 4,-9, 8,10, 9,-7, -8,5, -9,0, -13,11, -7,-13, -5,14, 0,-3, 10 },
	{ -3, -8,4, -13,0, 1,5, -6,-6, -1,-13, -2,-1, -14,12, -8,8, 0,9, 7,0, 10,-7, 9,-9, -11,6, 13,14, 3,-13, 5 },
                                                                                                               
	{ 1, 6,13, 7,1, 0,3, 14,-4, 7,6, -4,0, -9,14, 0,12, -6,8, 2,-6, -5,-9, 3,-13, -3,-6, -13,7, -11,-5, 13 },
	{ -2, 10,-6, 4,5, 2,-8, 11,3, 14,-12, 4,-14, -1,-1, -1,-9, -4,4, -4,4, -10,-4, -12,10, -1,13, 5,8, 9,10, -8 },
	{ -14, -2,-6, 5,-10, -10,-6, -3,0, -12,2, -5,0, 1,-13, 3,9, -9,0, 9,-3, 14,10, 6,7, -3,13, 0,6, 11,4, 4 },
	{ 3, 13,2, 7,9, 7,-2, 14,-6, 9,-3, 0,4, -2,5, -9,12, -5,0, -7,10, 1,-5, -12,-11, -7,-11, 5,-13, 0,1, -13 },
                                                                                                               
	{ 6, 4,0, 4,2, -2,11, 1,3, 12,-2, 11,-10, 9,8, 9,-7, 0,-12, 3,-14, -2,-3, -9,-9, -6,7, -8,3, -12,13, -4 },
	{ -9, -10,-7, -1,0, -8,-13, -3,2, 0,-2, 6,-10, 6,9, -10,12, -3,10, 7,14, 2,-1, -14,2, 10,8, 1,-4, 12,5, -5 },
	{ 8, -5,1, 3,3, -11,13, 3,0, -4,-5, -2,-9, -8,-3, -11,-4, 4,-14, 0,-11, 6,7, 0,1, 11,-8, 12,13, -2,7, 9 },
	{ -8, -1,-2, 0,-10, -9,-1, -9,-12, 5,4, -10,-3, 8,-14, -3,9, -6,0, 13,7, 10,4, 3,12, 6,2, -4,-8, 11,11, 0 },
};
#endif

#if defined(LOW) || defined(MEDIUM)

static const int NUM_VALLEYS	=	8;

static const int2 samplingPattern[16][8] = {
	{ -7, -12,-10, -1,-1, -2,1, 9,7, -1,5, -11,-9, 9,9, 6 },
	{ 0, 10,-9, 2,0, -4,11, 7,9, -2,-12, -7,4, -10,-5, -13 },
	{ 8, 1,1, 7,3, -8,-5, 1,-10, -6,-4, -13,-6, 12,11, -6 },
	{ 3, 2,-1, -8,-5, 11,2, 10,-10, 1,7, -6,12, 0,-12, -6 },
      
	{ -3, 10,-1, -1,-12, 5,8, 10,-9, -7,0, -11,10, -6,8, 1 },
	{ 7, -3,-1, 9,8, 5,-5, -10,4, -14,-8, 0,0, 0,-12, -6 },
	{ 0, -3,0, 12,8, 3,9, -9,-10, -3,-9, 5,-5, -13,9, 11 },
	{ -7, -1,2, -1,-9, 8,1, -11,1, 7,-10, -10,9, 2,11, -8 },
      
	{ 0, -3,12, -6,-11, -2,2, 9,-7, 7,2, -14,10, 2,-8, -10 },
	{ -8, 0,-9, 8,2, -8,-7, -9,6, 0,1, 7,11, 8,11, -6 },
	{ -11, -4,-3, -9,-7, 6,1, -2,0, 12,5, -12,8, 8,11, -4 },
	{ 7, -7,-3, 0,8, 3,-1, -8,-11, -3,-9, 6,-1, 8,5, 13 },
      
	{ 4, -6,-1, -11,7, 8,-3, -1,13, 0,-9, -8,-5, 9,-11, 0},
	{ 4, 7,4, -5,-2, 0,-3, 12,13, 2,-10, 6,-7, -10,-11, -2 },
	{ -7, -8,3, -9,-1, 3,-13, 1,9, 10,9, 1,-8, 8,0, 12 },
	{ -8, -7,-14, 0,0, 0,-5, 7,5, 11,0, -9,9, -10,13, 4 },
};
#endif


//-----------------------------------------------------------------------------

float LinearizeDepth(float z)
{
	#ifdef HDAO
	float a	=	hdaoParams.LinDepthScale;
	float b = 	hdaoParams.LinDepthBias;
	return 1.0f / (z * a + b);
	#endif
	#ifdef BILATERAL
	float a	=	filterParams.LinDepthScale;
	float b = 	filterParams.LinDepthBias;
	return 1.0f / (z * a + b);
	#endif
	return 0;
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

#ifdef HDAO
float3 FetchPositionFromCache ( int2 xy )
{
	CachedPosition cp = cachedPosition[xy.y][xy.x];
	return float3( UintToFloat2(cp.xy), cp.z );
}

float3 GetViewspacePosition( float z, float2 xy )
{
	float2	tg	=	float2( hdaoParams.CameraTangentX, hdaoParams.CameraTangentY );
	return float3( xy * tg * z, z );
}
#endif


uint Interleave4X ( uint value, uint size )
{
	return (value%(size/4))*4 + (value%4);
}

//-----------------------------------------------------------------------------

#ifdef HDAO

Texture2D<float4> 	source  : register(t0); 
RWTexture2D<float4> target  : register(u0); 

/*	http://www.reedbeta.com/blog/quick-and-easy-gpu-random-numbers-in-d3d11/ */
uint wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

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
	
	float distanceScale	=	length( float3( hdaoParams.CameraTangentX, hdaoParams.CameraTangentY, 1 ) );
	
	float2	projLocation	=	location.xy * hdaoParams.InputSize.zw * 2 - 1;
			projLocation.y	*=	-1;
	
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
			
			float2	xy		=	loadPoint.xy * hdaoParams.InputSize.zw * 2 - 1;
			
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
	float	centerDistance	=	centerPosition.z * distanceScale;
	float	occlusion		=	0.0f;
	
	int2	coords			=	location.xy*2 + hdaoParams.WriteOffset;
	int		offset			=	(wang_hash(199 * coords.x + 2999 * coords.y) & 0xF);
	//int		offset			=	((199 * coords.x + 179 * coords.y) & 0x1F);

	[branch]
	if ( centerPosition.z < hdaoParams.DiscardDistance ) {

		[unroll]
		for (int i=0; i<NUM_VALLEYS; i++) {

			int2 	fetch0		=	cacheCenter + samplingPattern[ offset ][ i ];
			int2 	fetch1		=	cacheCenter - samplingPattern[ offset ][ i ];
			float3	position0	=	FetchPositionFromCache( fetch0 );
			float3	position1	=	FetchPositionFromCache( fetch1 );
			// float	distance0	=	length(position0);
			// float	distance1	=	length(position1);
			float	distance0	=	position0.z * distanceScale;
			float	distance1	=	position1.z * distanceScale;
			
			float	delta0		=	centerDistance - distance0;
			float	delta1		=	centerDistance - distance1;
			
			float	weight0		=	(delta0 > hdaoParams.AcceptRadius) ? saturate( (hdaoParams.RejectRadius - delta0) * hdaoParams.RejectRadiusRcp ) : 0;
			float	weight1		=	(delta1 > hdaoParams.AcceptRadius) ? saturate( (hdaoParams.RejectRadius - delta1) * hdaoParams.RejectRadiusRcp ) : 0;
			
			float3	direction0	=	normalize(centerPosition - position0);
			float3	direction1	=	normalize(centerPosition - position1);
			
			float	valleyDot	=	saturate(dot(direction0, direction1) + 0.9f) * 1.2f;
			
			occlusion	+=	( weight0 * weight1 * valleyDot * valleyDot * valleyDot);
		}
		
		occlusion /= (float)NUM_VALLEYS;
		
	} else {
	
		occlusion	=	0;
	
	}
	//*/
	
	target[location.xy*2 + hdaoParams.WriteOffset] = pow(abs(1-occlusion.xxxx * hdaoParams.LinearIntensity), hdaoParams.PowerIntensity);
}

#endif

//-----------------------------------------------------------------------------

#ifdef BILATERAL

Texture2D<float4> 	hdao 	: register(t0); 
Texture2D<float4> 	depth  	: register(t1); 
RWTexture2D<float4> target  : register(u0); 

groupshared float2 cachedData[BilateralBlockSizeX*2][BilateralBlockSizeX*2];


[numthreads(BilateralBlockSizeX,BilateralBlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location	=	dispatchThreadId.xy;
	int2 blockSize	=	int2(BilateralBlockSizeX,BilateralBlockSizeY);
	int3 loadPoint	=	int3( location.xy, 0 );

	int overlapX 	=	((BilateralBlockSizeX)/2);
	int overlapY 	=	((BilateralBlockSizeY)/2);
	
	//---------------------------------------------
	//	load data to shared memory :
	//---------------------------------------------

	float accumValue 	= 0;
	float accumWeight	= 0;
	
	float depthCenter	=	LinearizeDepth ( depth.Load( loadPoint ).r );
	
	[unroll]
	for (int t=-7; t<=7; t++) {
	
		#ifdef VERTICAL
		int3 offset = int3(0,t,0);
		#endif
		#ifdef HORIZONTAL
		int3 offset = int3(t,0,0);
		#endif
	
		float 	localDepth	=	LinearizeDepth ( depth.Load( loadPoint + offset ).r );
		float	localHdao	=	hdao.Load( loadPoint + offset ).r;

		float	delta		=	localDepth - depthCenter;
		float 	weight		=	exp( - delta * delta * filterParams.DepthFactor );

		accumValue	+=	localHdao * weight;
		accumWeight	+= 	weight;
	}

	target[location.xy]		=	accumValue / accumWeight;
}

#endif


//-----------------------------------------------------------------------------

#ifdef INTERLEAVE

Texture2D<float4> 	source  : register(t0); 
RWTexture2D<float> 	target0	: register(u0); 
RWTexture2D<float> 	target1	: register(u1); 
RWTexture2D<float> 	target2	: register(u2); 
RWTexture2D<float> 	target3	: register(u3); 

[numthreads(InterleaveBlockSizeX,InterleaveBlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	storePoint			=	dispatchThreadId.xy;
	int3	loadPoint			=	int3(dispatchThreadId.xy*2,0);
	
	target0[ storePoint.xy ]	=	source.Load( loadPoint + int3(0,0,0) ).r;
	target1[ storePoint.xy ]	=	source.Load( loadPoint + int3(1,0,0) ).r;
	target2[ storePoint.xy ]	=	source.Load( loadPoint + int3(0,1,0) ).r;
	target3[ storePoint.xy ]	=	source.Load( loadPoint + int3(1,1,0) ).r;
}

#endif

