#if 0
$ubershader DOUBLE_PASS	MASK_DEPTH|MASK_ALPHA HORIZONTAL|VERTICAL
#endif

#include "auto/bilateral.fxi"

//-------------------------------------------------------------------------------

float LinearizeDepth(float z)
{
	float a	=	Camera.LinearizeDepthScale;
	float b = 	Camera.LinearizeDepthBias;
	return 1.0f / (z * a + b);
}

Texture2D 	Source 	: register(t0); 
Texture2D 	Mask  	: register(t1); 
RWTexture2D<float4> Target  : register(u0); 

#ifdef DOUBLE_PASS
#ifdef HORIZONTAL
groupshared float4 cachedData[BilateralBlockSizeY][BilateralBlockSizeX*2];
groupshared float2 cachedMask[BilateralBlockSizeY][BilateralBlockSizeX*2];
#endif

#ifdef VERTICAL
groupshared float4 cachedData[BilateralBlockSizeY*2][BilateralBlockSizeX];
groupshared float2 cachedMask[BilateralBlockSizeY*2][BilateralBlockSizeX];
#endif
#endif


float ExtractMaskFactor( float4 mask )
{
#ifdef MASK_DEPTH
	return LinearizeDepth ( mask.r );
#endif	
#ifdef MASK_ALPHA
	return mask.a;
#endif	
	return 0;
}

float ExtractLumaFactor( float4 color )
{
	return dot( color, filterParams.LumaVector );
}


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
	
	Source.GetDimensions( texWidth, texHeight );
	
	[unroll]
	for (int i=0; i<2; i++) {
		
		int2 topLeft	=	groupId.xy * blockSize.xy - int2(overlapX,overlapY);
		int3 loadPoint 	= 	int3( topLeft + groupThreadId.xy*scale + i*bias, 0 );
		
		int2 storePoint	= 	int2( groupThreadId.xy*scale + i*bias );

		loadPoint.xy	=	clamp( loadPoint.xy, int2(0,0), int2(texWidth,texHeight) );
		
		float4	color	=	Source .Load( loadPoint );
		float4	mask	=	Mask.Load( loadPoint );
		
		float 	lf		=	ExtractLumaFactor( color );
		float 	mf		=	ExtractMaskFactor( mask );
		cachedMask[ storePoint.y ][ storePoint.x ]  = float2( lf, mf );
		cachedData[ storePoint.y ][ storePoint.x ]  = color;
	}
	
	GroupMemoryBarrierWithGroupSync();

	//---------------------------------------------
	//	bilateral filter :
	//---------------------------------------------
	
	float4 accumValue 	= 0;
	float  accumWeight	= 0;
	
	float 	maskCenter		=	ExtractMaskFactor( Mask  .Load( centerPoint ) );
	float	lumaCenter		=	ExtractLumaFactor( Source.Load( centerPoint ) );
	
	[unroll]
	for (int t=-7; t<=7; t++) {
	
		#ifdef VERTICAL
		int3 offset 	= int3(0,t,0);
		#endif
		#ifdef HORIZONTAL
		int3 offset 	= int3(t,0,0);
		#endif
		
		float	k			=	filterParams.GaussFalloff;
		float	falloff		=	t*t * k*k;
		
		int2	loadPoint	=	int2(overlapX,overlapY) + groupThreadId.xy + offset.xy;
	
		float4	localColor	=	cachedData[ loadPoint.y ][ loadPoint.x ];
		float2	localMask	=	cachedMask[ loadPoint.y ][ loadPoint.x ];
		
		float	deltaL		=	localMask.x - lumaCenter;
		float	deltaM		=	localMask.y - maskCenter;
		
		float 	powerL		=	deltaL * deltaL * filterParams.ColorFactor;
		float	powerM		=	deltaM * deltaM * filterParams.MaskFactor;
		
		float	weight		=	exp( - powerL - powerM - falloff );
		
		accumWeight			+=	weight;
		accumValue 			+=	localColor * weight;
	}//*/
	
	float4	result			=	accumValue / accumWeight;
	
	Target[location.xy]		=	result;
}

//-------------------------------------------------------------------------------

