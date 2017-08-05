#if 0
$ubershader	DEPTH HORIZONTAL|VERTICAL
#endif

#include "bilateral.auto.hlsl"

//-------------------------------------------------------------------------------

cbuffer CBParams : register(b0) {
	FilterParams	filterParams;
};


float LinearizeDepth(float z)
{
	float a	=	filterParams.LinDepthScale;
	float b = 	filterParams.LinDepthBias;
	return 1.0f / (z * a + b);
}

Texture2D 	hdao 	: register(t0); 
Texture2D 	depth  	: register(t1); 
RWTexture2D<float4> target  : register(u0); 

#ifdef HORIZONTAL
groupshared float2 cachedData[BilateralBlockSizeY][BilateralBlockSizeX*2];
#endif

#ifdef VERTICAL
groupshared float2 cachedData[BilateralBlockSizeY*2][BilateralBlockSizeX];
#endif



[numthreads(BilateralBlockSizeX,BilateralBlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location		=	dispatchThreadId.xy;
	int2 blockSize		=	int2(BilateralBlockSizeX,BilateralBlockSizeY);
	int3 centerPoint	=	int3( location.xy, 0 );
	
	#ifdef HORIZONTAL
	int overlapX 	=	((BilateralBlockSizeX)/2);
	int overlapY 	=	0;
	int2 scale		=	int2(2,1);
	int2 bias		=	int2(1,0);
	#endif
	
	#ifdef VERTICAL
	int overlapX 	=	0;
	int overlapY 	=	((BilateralBlockSizeY)/2);
	int2 scale		=	int2(1,2);
	int2 bias		=	int2(0,1);
	#endif
	
	//---------------------------------------------
	//	load data to shared memory :
	//---------------------------------------------

	int texWidth;
	int texHeight;
	
	hdao.GetDimensions( texWidth, texHeight );
	
	[unroll]
	for (int i=0; i<2; i++) {
		
		int2 topLeft	=	groupId.xy * blockSize.xy - int2(overlapX,overlapY);
		int3 loadPoint 	= 	int3( topLeft + groupThreadId.xy*scale + i*bias, 0 );
		
		int2 storePoint	= 	int2( groupThreadId.xy*scale + i*bias );

		loadPoint.xy	=	clamp( loadPoint.xy, int2(0,0), int2(texWidth,texHeight) );
		
		float 	d		=	LinearizeDepth ( depth.Load(loadPoint).r );
		float 	ao		=	hdao.Load( loadPoint ).r;
		cachedData[ storePoint.y ][ storePoint.x ] = float2( ao, d );
	}
	
	GroupMemoryBarrierWithGroupSync();

	//---------------------------------------------
	//	bilateral filter :
	//---------------------------------------------
	
	float accumValue 	= 0;
	float accumWeight	= 0;
	
	float 	depthCenter		=	LinearizeDepth ( depth.Load( centerPoint ).r );
	float	hdaoCenter		=	hdao.Load( centerPoint ).x;
	
	[unroll]
	for (int t=-7; t<=7; t++) {
	
		#ifdef VERTICAL
		int3 offset 	= int3(0,t,0);
		#endif
		#ifdef HORIZONTAL
		int3 offset 	= int3(t,0,0);
		#endif
		
		int2	loadPoint	=	int2(overlapX,overlapY) + groupThreadId.xy + offset.xy;
	
		float2	depthAO		=	cachedData[ loadPoint.y ][ loadPoint.x ].xy;
		float 	localDepth	=	depthAO.y;
		float	localHdao	=	depthAO.x;

		float	deltaD		=	localDepth - depthCenter;
		float	powerD		=	deltaD * deltaD * filterParams.DepthFactor;

		float	deltaC		=	hdaoCenter - localHdao;
		float 	powerC		=	deltaC * deltaC * filterParams.ColorFactor;
		
		float	weight		=	exp( - powerD - powerC );
		
		accumWeight			+=	weight;
		accumValue 			+=	localHdao * weight;
	}
	
	float	result			=	accumValue / accumWeight;
	
	target[location.xy]		=	result;
}

//-------------------------------------------------------------------------------

