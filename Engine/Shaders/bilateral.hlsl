#if 0
$ubershader SINGLE_PASS	MASK_DEPTH|MASK_ALPHA|MASK_INDEX
$ubershader DOUBLE_PASS	MASK_DEPTH|MASK_ALPHA|MASK_INDEX +LUMA_ONLY HORIZONTAL|VERTICAL
#endif

#include "auto/bilateral.fxi"

#include "float16.fxi"

//-------------------------------------------------------------------------------

float LinearizeDepth(float z)
{
	float a	=	Camera.LinearizeDepthScale;
	float b = 	Camera.LinearizeDepthBias;
	return 1.0f / (z * a + b);
}


float ExtractMaskFactor( float4 mask )
{
#ifdef MASK_DEPTH
	return LinearizeDepth ( mask.r );
#endif	
#ifdef MASK_ALPHA
	return mask.a;
#endif	
#ifdef MASK_INDEX
	return 0;
#endif	
	return 0;
}

float ExtractLuma( float4 color )
{
#ifndef LUMA_ONLY	
	return dot( color, filterParams.LumaVector );
#else	
	return color.r;
#endif
}

float ExtractLuma( float3 color )
{
#ifndef LUMA_ONLY	
	return dot( color.rgb, filterParams.LumaVector.rgb );
#else	
	return color.r;
#endif
}

struct PIXEL
{
#ifndef LUMA_ONLY	
	float4	Data;
#endif	
	float	Luma;
	float	Mask;
};

PIXEL ExtractPixelData( float4 color, float4 mask )
{
	PIXEL px;
	
#ifdef MASK_DEPTH
	px.Luma	=	ExtractLuma( color );
	px.Mask	= 	LinearizeDepth ( mask.r );
#endif	
#ifdef MASK_ALPHA
	px.Luma	=	ExtractLuma( color );
	px.Mask	= 	mask.a;
#endif	
#ifdef MASK_INDEX
	px.Luma	=	ExtractLuma( color );
	px.Mask	= 	mask.r;
#endif	
#ifndef LUMA_ONLY
	px.Data = color;
#endif

	return px;
}

float ComputePixelDelta( PIXEL a, PIXEL b, float falloff )
{
	float	deltaL	=	a.Luma - b.Luma;
	float	deltaM	=	a.Mask - b.Mask;
	
	float 	powerL	=	deltaL * deltaL * filterParams.ColorFactor;
	float	powerM	=	deltaM * deltaM * filterParams.MaskFactor;
	
	float	weight	=	exp( - powerL - powerM - falloff );
	
	return weight;
}

float4 GetPixelData( PIXEL px )
{
#ifndef LUMA_ONLY	
	return px.Data;
#else
	return px.Luma;
#endif
}

//-------------------------------------------------------------------------------
//	DOUBLE PASS 
//-------------------------------------------------------------------------------

#ifdef DOUBLE_PASS
#ifdef HORIZONTAL
groupshared PIXEL cache[BlockSize16][BlockSize16*2];
#endif

#ifdef VERTICAL
groupshared PIXEL cache[BlockSize16*2][BlockSize16];
#endif
#endif

#ifdef DOUBLE_PASS

[numthreads(BlockSize16,BlockSize16,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location		=	dispatchThreadId.xy;
	int2 blockSize		=	int2(BlockSize16,BlockSize16);
	int3 centerPoint	=	int3( location.xy, 0 );
	
	#ifdef HORIZONTAL
	int overlapX 	=	((BlockSize16)/2);
	int overlapY 	=	0;
	int2 scale		=	int2(2,1);
	int2 bias		=	int2(1,0);
	#endif
	
	#ifdef VERTICAL
	int overlapX 	=	0;
	int overlapY 	=	((BlockSize16)/2);
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
		
		cache[ storePoint.y ][ storePoint.x ]	=	ExtractPixelData( color, mask );
	}
	
	GroupMemoryBarrierWithGroupSync();

	//---------------------------------------------
	//	bilateral filter :
	//---------------------------------------------
	
	float4 accumValue 	= 0;
	float  accumWeight	= 0;
	
	PIXEL	centerPixel	=	ExtractPixelData( Source.Load( centerPoint ),  Mask.Load( centerPoint ) );
	
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
	
		PIXEL 	localPixel	=	cache[ loadPoint.y ][ loadPoint.x ];
		
		float	weight		=	ComputePixelDelta( localPixel, centerPixel, falloff );
		
		accumWeight			+=	weight;
		accumValue 			+=	GetPixelData( localPixel ) * weight;
	}//*/
	
	float4	result			=	accumValue / accumWeight;
	
	Target[location.xy]		=	result;
}

#endif

//-------------------------------------------------------------------------------
//	SINGLE PASS 
//-------------------------------------------------------------------------------

#ifdef SINGLE_PASS

[numthreads(BlockSize8,BlockSize8,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 loadXY		=	int3( dispatchThreadId.xy + filterParams.SourceXY.xy, 0 );
	int3 storeXY	=	int3( dispatchThreadId.xy + filterParams.TargetXY.xy, 0 );
	
	float3 accumValue 	= 0;
	float  accumWeight	= 0;
	
	GroupMemoryBarrierWithGroupSync();
	
	float3	centerColor		=	Source.Load( loadXY ).rgb;
	float	centerLuma		=	ExtractLuma( centerColor );
	int		centerIndex		=	Mask.Load( loadXY ).r;
	float	centerMask		=	ExtractMaskFactor( Source.Load( loadXY ) );

	//[unroll]
	for (int y=-2; y<=2; y++) 
	{
		for (int x=-2; x<=2; x++) 
		{
			float	k			=	filterParams.GaussFalloff;
			float	falloff		=	(x*x+y*y) * k*k;
			int3	localLoadXY	=	int3( loadXY.xy + int2(x,y), loadXY.z );
			
			float3	localColor	=	Source.Load( localLoadXY ).rgb;
			float	localLuma	=	ExtractLuma( localColor );
			float	localMask	=	ExtractMaskFactor( Source.Load( localLoadXY ) );
			int		localIndex	=	Mask.Load( localLoadXY ).r;
			
			float	deltaL		=	localLuma - centerLuma;
			float	deltaM		=	localMask - centerMask;
			
			float 	powerL		=	deltaL * deltaL * filterParams.ColorFactor;
			float	powerM		=	deltaM * deltaM * filterParams.MaskFactor;
			
			float	indexFactor	=	( localIndex == centerIndex ) ? 1 : 0;
			
			float	weight		=	exp( - powerL - powerM - falloff ) * indexFactor;
			
			accumWeight			+=	weight;
			accumValue 			+=	localColor * weight;
		}//*/
	}
	
	float3	result			=	accumValue / accumWeight;
	
	Target[storeXY.xy]		=	float4( result, 0 );
}

#endif
