#if 0
----$ubershader SINGLE_PASS	MASK_DEPTH|MASK_ALPHA
$ubershader DOUBLE_PASS	MASK_DEPTH|MASK_ALPHA +LUMA_ONLY HORIZONTAL|VERTICAL
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

Texture2D 	Source 	: register(t0); 
Texture2D 	Mask  	: register(t1); 
RWTexture2D<float4> Target  : register(u0); 


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

float ExtractLuma( float4 color )
{
#ifndef LUMA_ONLY	
	return dot( color, filterParams.LumaVector );
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

groupshared uint2 cache[16][16];

void CacheStore( uint2 location, float4 value )
{
	cache[ location.y ][ location.x ]	=	pack_color4( value );
}


float4 CacheLoad( uint2 location )
{
	return unpack_color4( cache[ location.y ][ location.x ] );
}

[numthreads(BlockSize8,BlockSize8,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 loadXY		=	dispatchThreadId.xy + filterParams.SourceXY;
	int2 storeXY	=	dispatchThreadId.xy + filterParams.TargetXY;
	
	float3 accumValue 	= 0;
	float  accumWeight	= 0;
	int3   centerPoint	= int3( loadXY.xy, 0 );
	
	//	load cache :
	uint2 offset = uint2( 4, 4 );
	uint2 scale  = uint2( BlockSize8, BlockSize8 );
	
	// for (uint i=0; i<3; i++) // 144 / 64 round up
	// {
		// uint  	base	=	i * BlockSize8 * BlockSize8;
		// uint  	addr	=	base + groupIndex;
		// uint2 	xy		=	uint2( addr%12, addr/12 );
		// uint3 	uv 		=	uint3( groupId.xy*scale + xy - offset, 0 );
		// float3	source	=	Source.Load( uv ).rgb;
		// float	mask	=	ExtractMaskFactor( Mask.Load( uv ) );
		// CacheStore( xy, float4( source, mask ) );
	// }
	
	uint3 uv00	 = uint3( groupId.xy*scale + groupThreadId.xy*2 + uint2(0,0) + filterParams.SourceXY - offset, 0 );
	uint3 uv01	 = uint3( groupId.xy*scale + groupThreadId.xy*2 + uint2(0,1) + filterParams.SourceXY - offset, 0 );
	uint3 uv10	 = uint3( groupId.xy*scale + groupThreadId.xy*2 + uint2(1,0) + filterParams.SourceXY - offset, 0 );
	uint3 uv11	 = uint3( groupId.xy*scale + groupThreadId.xy*2 + uint2(1,1) + filterParams.SourceXY - offset, 0 );
	
	CacheStore( groupThreadId.xy*2 + uint2(0,0), float4( Source.Load( uv00 ).rgb, ExtractMaskFactor( Mask.Load( uv00 ) ) ) );
	CacheStore( groupThreadId.xy*2 + uint2(0,1), float4( Source.Load( uv01 ).rgb, ExtractMaskFactor( Mask.Load( uv01 ) ) ) );
	CacheStore( groupThreadId.xy*2 + uint2(1,0), float4( Source.Load( uv10 ).rgb, ExtractMaskFactor( Mask.Load( uv10 ) ) ) );
	CacheStore( groupThreadId.xy*2 + uint2(1,1), float4( Source.Load( uv11 ).rgb, ExtractMaskFactor( Mask.Load( uv11 ) ) ) );
	
	GroupMemoryBarrierWithGroupSync();
	
	float4	valueCenter	=	CacheLoad( groupThreadId.xy + offset );
	float	lumaCenter	=	ExtractLumaFactor( valueCenter );
	float	maskCenter	=	valueCenter.a;

	//[unroll]
	for (int y=-2; y<=2; y++) {
		for (int x=-2; x<=2; x++) {
	
			float	k			=	filterParams.GaussFalloff;
			float	falloff		=	(x*x+y*y) * k*k;
			
			float4 	localValue	=	CacheLoad( groupThreadId.xy + offset + int2(x,y) );
			float3	localColor	=	localValue.rgb;
			float	localLuma	=	ExtractLumaFactor( localColor );
			float	localMask	=	localValue.a;
			
			float	deltaL		=	localLuma - lumaCenter;
			float	deltaM		=	localMask - maskCenter;
			
			float 	powerL		=	deltaL * deltaL * filterParams.ColorFactor;
			float	powerM		=	deltaM * deltaM * filterParams.MaskFactor;
			
			float	weight		=	exp( - powerL - powerM - falloff );
			
			accumWeight			+=	weight;
			accumValue 			+=	localColor * weight;
		}//*/
	}
	
	float3	result			=	accumValue / accumWeight;
	
	Target[storeXY.xy]		=	float4( result, 0 );
}

#endif
