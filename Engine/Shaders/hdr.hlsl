

#if 0
$ubershader		(TONEMAPPING LINEAR|REINHARD|FILMIC)|MEASURE_ADAPT +SHOW_HISTOGRAM
$ubershader		COMPOSITION
$ubershader		COMPUTE_HISTOGRAM|AVERAGE_HISTOGRAM
#endif

#include "auto/hdr.fxi"
//#include "colorGrading.fxi"

SamplerState	LinearSampler		: register(s0);
SamplerState	AnisotropicSampler	: register(s0);


/*
**	FSQuad
*/
float4 FSQuad( uint VertexID ) 
{
	return float4((VertexID == 0) ? 3.0f : -1.0f, (VertexID == 2) ? 3.0f : -1.0f, 1.0f, 1.0f);
}

float2 FSQuadUV ( uint VertexID )
{
	return float2((VertexID == 0) ? 2.0f : -0.0f, 1-((VertexID == 2) ? 2.0f : -0.0f));
}

float GetLuminance(float3 color)
{
    return dot(color, float3(0.2127f, 0.7152f, 0.0722f));
}


float3 ColorSaturation( float3 rgbVal, float3 sat )
{
	float3 grey = GetLuminance(rgbVal);
	return grey + sat * (rgbVal-grey);
}

float EvalLogContrastFunc(float x, float midpoint, float contrast)
{
	float logMidpoint = log2(midpoint);
	float logX = log2(x+Epsilon);
	float adjX = logMidpoint + (logX - logMidpoint) * contrast;
	float ret  = max(0.0f, exp2(adjX) - Epsilon);
	return ret;
}

float NormalizeEV( float ev )
{
	return saturate( (ev - Params.EVMin) * Params.EVRangeInverse );
}

float ComputeEV( float luminance )
{
    if ( luminance < Epsilon ) {
        return 0;
    } else {
		return log2(luminance);
	}
}

float EVToLuminance( float ev )
{
	return exp2(ev);
}

float HDRToLogNormalizedEV(float3 hdrColor)
{
	return NormalizeEV( ComputeEV( GetLuminance(hdrColor) ) );
}

uint HDRToHistogramBin(float3 hdrColor)
{
    float logLuminance = HDRToLogNormalizedEV(hdrColor);
    return (uint)(logLuminance * 254.0 + 1.0);
}

/*-----------------------------------------------------------------------------
	HISTOGRAM
-----------------------------------------------------------------------------*/

#ifdef COMPUTE_HISTOGRAM
Texture2D HDRTexture : register(t0);
RWByteAddressBuffer LuminanceHistogram : register(u0);


groupshared uint histogramShared[NumHistogramBins];
                        

[numthreads(BlockSizeX, BlockSizeY, 1)]
void CSMain(uint groupIndex : SV_GroupIndex, uint3 threadId : SV_DispatchThreadID)
{
    histogramShared[groupIndex] = 0;
    
    GroupMemoryBarrierWithGroupSync();
    
    if(threadId.x < Params.Width && threadId.y < Params.Height) {
		
        float3 hdrColor = HDRTexture.Load(int3(threadId.xy, 0)).rgb;
        uint binIndex = HDRToHistogramBin(hdrColor);
        InterlockedAdd(histogramShared[binIndex], 1);
    }
    
    GroupMemoryBarrierWithGroupSync();
    
    LuminanceHistogram.InterlockedAdd(groupIndex * 4, histogramShared[groupIndex]);
}
#endif


#ifdef AVERAGE_HISTOGRAM
RWByteAddressBuffer LuminanceHistogram : register(u0);
RWTexture2D<float> LuminanceOutput : register(u1);


groupshared float HistogramShared[NumHistogramBins];

[numthreads(BlockSizeX, BlockSizeX, 1)]
void CSMain(uint groupIndex : SV_GroupIndex)
{
    float countForThisBin = (float)LuminanceHistogram.Load(groupIndex * 4);
    HistogramShared[groupIndex] = countForThisBin * (float)groupIndex;
    
    GroupMemoryBarrierWithGroupSync();
    
    [unroll]
    for(uint histogramSampleIndex = (NumHistogramBins >> 1); histogramSampleIndex > 0; histogramSampleIndex >>= 1) {
		
        if(groupIndex < histogramSampleIndex) {
            HistogramShared[groupIndex] += HistogramShared[groupIndex + histogramSampleIndex];
        }

        GroupMemoryBarrierWithGroupSync();
    }
    
    if(groupIndex == 0)
    {
		float minLogLuminance			= 	Params.EVMin;
		float logLuminanceRange			=	Params.EVRange;
			
		float pixelCount				= 	Params.Width * Params.Height;
        float weightedLogAverage 		= 	(HistogramShared[0].x / max((float)pixelCount - countForThisBin, 1.0)) - 1.0;
		//float weightedLogAverage 		= 	(HistogramShared[0].x / max((float)pixelCount, 1.0)) - 1.0;
        float weightedAverageLuminance	= 	exp2(((weightedLogAverage / 254.0) * logLuminanceRange) + minLogLuminance);
        float luminanceLastFrame 		=	LuminanceOutput[uint2(0, 0)];
		
		weightedAverageLuminance		=	clamp( ComputeEV(weightedAverageLuminance), Params.EVMin, Params.EVMax );
		
        float adaptedLuminance 			=	lerp( luminanceLastFrame, weightedAverageLuminance, Params.AdaptationRate );
        LuminanceOutput[uint2(0, 0)] 	=	adaptedLuminance;
    }
}
#endif


/*-----------------------------------------------------------------------------
	Luminance Measurement and Adaptation:
	Assumed 128x128 input image.
-----------------------------------------------------------------------------*/
#if MEASURE_ADAPT

Texture2D	SourceHdrImage 		: register(t0);
Texture2D	MasuredLuminance	: register(t1);


float4 VSMain(uint VertexID : SV_VertexID) : SV_POSITION
{
	return FSQuad( VertexID );
}


float4 PSMain(float4 position : SV_POSITION) : SV_Target
{
	float sumLum = 0;
	const float3 lumVector = float3(0.213f, 0.715f, 0.072f );
	
	float oldLum = MasuredLuminance.Load(int3(0,0,0)).r;
	
	for (int x=0; x<32; x++) {
		for (int y=0; y<32; y++) {
			
			sumLum += log( dot( lumVector, SourceHdrImage.Load(int3(x,y,3)).rgb ) + 0.0001f );
		}
	}
	sumLum = clamp( exp(sumLum / 1024.0f), Params.LuminanceLowBound, Params.LuminanceHighBound );
	
	return lerp( oldLum, max(0.5,min(100,sumLum)), Params.AdaptationRate );
}

#endif


/*-----------------------------------------------------------------------------
	Frame composition
-----------------------------------------------------------------------------*/

#ifdef COMPOSITION

Texture2D		HdrImageSolid 		: register(t0);
Texture2D		HdrImageGlass		: register(t1);
Texture2D		DistortionGlass		: register(t2);
Texture2D		DistortionParticles	: register(t3);
Texture2D		SoftParticlesFront	: register(t4);
Texture2D		SoftParticlesBack	: register(t5);
Texture2D		TurbidBackground	: register(t6);
Texture2D		ParticleVelocity	: register(t7);


float4 VSMain(uint VertexID : SV_VertexID, out float2 uv : TEXCOORD) : SV_POSITION
{
	uv = FSQuadUV( VertexID );
	return FSQuad( VertexID );
}


float4 PSMain(float4 position : SV_POSITION, float2 uv : TEXCOORD0 ) : SV_Target
{
	uint width;
	uint height;
	uint xpos = position.x;
	uint ypos = position.y;
	HdrImageSolid.GetDimensions( width, height );
	
	int3 intUV	=	int3(position.xy, 0);

	//	Compute particle distortion :
	float4	distortPrtSrc	=	DistortionParticles.SampleLevel( LinearSampler, uv, 0 ).rgba;
	float2	distortPrt		=	(distortPrtSrc.xy - distortPrtSrc.zw) * 0.02;
	
	float2	velocityPrt		=	ParticleVelocity.SampleLevel( LinearSampler, uv, 0 ).rg * 2 - 1;
	velocityPrt.y *= -1;
	
	//return float4(velocityPrt.xy*2-1, 0, 1);
	
	//	Compute transparent object distortion :
	float4	distortGlass	=	DistortionGlass	.SampleLevel( LinearSampler, uv, 0 ).rgba;
			distortGlass.xy	=	(distortGlass.xy * 2 - float2(1,1));
	float	distortAmount	=	length(distortGlass.xy);
			//	fix little blurriness :
			distortGlass.xy	=	distortGlass.xy / (distortAmount+0.001) * pow(saturate(distortAmount*1.05-0.05), 2);
			distortGlass.xy *=	float2( 0.15, -0.15 );

	//	Get transparent objects for composing :
	float4	hdrImageGlass	=	HdrImageGlass	.SampleLevel( LinearSampler, uv, 0 ).rgba;
	float	blurFactor		=	pow(lerp(distortGlass.z, hdrImageGlass.a, 0.5f), 2);

	//	Get solid background :
	float3	hdrImageSolid	=	HdrImageSolid	.SampleLevel( LinearSampler, uv + distortGlass.xy + distortPrt.xy, 0 ).rgb;
	float3	turbidBack		=	TurbidBackground.SampleLevel( LinearSampler, uv + distortGlass.xy + distortPrt.xy, blurFactor*3 ).rgb;
	
			hdrImageSolid	=	lerp( hdrImageSolid.rgb, turbidBack.rgb, saturate(blurFactor*8) );

	//	Compose solid and transparent objects :
			hdrImageSolid	=	lerp( hdrImageSolid.rgb, hdrImageGlass.rgb, hdrImageGlass.a );
			
	//	Get soft particles :
	#if 1
	float4	softPrtFront	=	SoftParticlesFront.SampleLevel( LinearSampler, uv + distortPrt.xy, 0 ).rgba;
	#else
	float4	softPrtFront	=	0;
	for (float t=-0.5; t<=0.5; t+=0.25f) {
		softPrtFront += SoftParticlesFront.SampleLevel( LinearSampler, uv + distortPrt.xy + velocityPrt.xy * (t / 60.0f), 0 ).rgba;
	}
	softPrtFront.rgba /= 9.0f;
	//softPrtFront.a = 1;
	#endif

	//	Compose solid+transparent and soft particles :
	float3	hdrImageFinal	=	lerp( hdrImageSolid.rgb, softPrtFront.rgb, softPrtFront.a );
	
	return float4( hdrImageFinal * float3(1,1,1), 1 );
}


#endif

/*-----------------------------------------------------------------------------
	Tonemapping and final color grading :
-----------------------------------------------------------------------------*/

#ifdef TONEMAPPING

Texture2D			FinalHdrImage 		: register(t0);
Texture2D			MasuredLuminance	: register(t1);
Texture2D			BloomTexture		: register(t2);
Texture2D			BloomMask1			: register(t3);
Texture2D			BloomMask2			: register(t4);
Texture2D			NoiseTexture		: register(t5);
Texture2D			VignmetteTexture	: register(t6);
ByteAddressBuffer 	Histogram			: register(t9);
Texture2D			DepthBuffer 		: register(t10);


float3 LinearToSRGB(float3 LinearRGB) {
    return (LinearRGB<=0.0031308f)?(12.92f*LinearRGB):mad(1.055f,pow(LinearRGB,1.0f/2.4f),-0.055f);
}

float3 SRGBToLinear(float3 SRGB) {
    return (SRGB<=0.04045f)?(SRGB/12.92f):pow(mad(SRGB,1.0f/1.055f,0.055f/1.055f),2.4f);
}

float3 Dither ( float3 color, uint x, uint y, float amount )
{
	uint width;
	uint height;
	NoiseTexture.GetDimensions( width, height );
	
	uint xx = x % 256;
	uint yy = y % 256;
	
	float3 	noiseValue	=	NoiseTexture.Load( int3(xx, yy, 0) ).rgb;
			
    noiseValue	=	mad ( noiseValue, 2.0f, -1.0f );
    noiseValue	=	sign( noiseValue ) * (1.0f - sqrt( 1.0f-abs(noiseValue) ) );
			
	color	=	saturate(color + noiseValue * amount);
	//color	=	SRGBToLinear( LinearToSRGB( color ) + noiseValue * amount );
	//color	=	pow( pow( color, 1/2.2f ) + noiseValue * amount, 2.2f );
	
	return color;
}


float4 VSMain(uint VertexID : SV_VertexID, out float2 uv : TEXCOORD) : SV_POSITION
{
	uv = FSQuadUV( VertexID );
	return FSQuad( VertexID );
}


static const float3	lumVector	=	float3(0.3f,0.6f,0.2f);


float3 SaturateColor ( float3 rgbVal, float factor )
{
	float3 grey = dot(lumVector,rgbVal);
	float3 ret = grey + factor * (rgbVal-grey);	
	return ret;
}


float3 TintColor ( float3 color, float3 tint )
{
	//return target * blend;

	/*float3 multiplyFactor	=	target * (blend+0.5);
	float3 screenFactor		=	(1 - (1-target) * (1-(blend-0.5)));
	return lerp( multiplyFactor, screenFactor, step( blend, 0.5 ) );*/

	float3 result = 0;
	result.r = color.r + (1-color.r) * tint.r/2.0;
	result.g = color.g + (1-color.g) * tint.g/2.0;
	result.b = color.b + (1-color.b) * tint.b/2.0;
	return result;
}


float3 Tonemap ( float3 exposured )
{
	#ifdef LINEAR
		float3 	tonemapped	=	saturate(pow( abs(exposured), 1/2.2f ));
	#endif
	
	#ifdef REINHARD
		float3 tonemapped	=	pow( abs(exposured / (1+exposured)), 1/2.2f );
	#endif
	
	#ifdef FILMIC
		float3 x = max(0,exposured-0.004);
		float3 tonemapped = (x*(6.2*x+.5))/(x*(6.2*x+1.7)+0.06);
	#endif
	return tonemapped;
}


float3 ShowHistogram ( uint x, uint y, float3 image, float lumActual, float lumAdapt )
{
	if (y>32 && y<64 && x>32 && x<64) {
		return float3( 0.5, 0.5, 0.5 );
	}
	
	if (y>Params.Height-128 || y<Params.Height-256) {
		return image;
	}
	
	image *= 0.9;
	
	uint 	bin			=	clamp(x*256 / Params.Width, 0,255);
	uint	hvalue		=	Histogram.Load(bin*4);
	uint 	height1		=	(uint)(10 * log10(hvalue+1));
	uint 	height2		=	(uint)(hvalue / Params.Width);
	float4 	shade1		=	(Params.Height-128 - y) <= height1 ? float4(0.2,0.2,0.2,0.7f) : float4(0,0,0,0);
	float4 	shade2		=	(Params.Height-128 - y)/2 == height2/2 ? float4(1.0,0.0,0.0,1.0f) : float4(0,0,0,0);
	
	float	nrmLogLum	=	HDRToLogNormalizedEV(float3(lumActual,lumActual,lumActual));
	uint		width		=	(uint)(nrmLogLum*Params.Width);
	
	if (x>width-1 && x<width+1) {
		return float3(1,0,0);
	}

	float	nrmLogLum2	=	HDRToLogNormalizedEV(float3(lumAdapt,lumAdapt,lumAdapt));
	uint	width2		=	(uint)(nrmLogLum2*Params.Width);
	
	if (x>width2-2 && x<width2+1) {
		return float3(1,1,0);
	}
	
	float  	exposured	=	pow( 2, (x / (float)Params.Width) * Params.EVRange + Params.EVMin );
	uint 	curve		=	(uint)(Tonemap( float3(exposured,exposured,exposured) * Params.KeyValue / lumAdapt ).r * 128);
	float4 	shade3		=	((Params.Height-128 - y)==curve) ? float4(0,1,0,1) : float4(0,0,0,0);
	
	uint	adaptMin	=	(uint)(NormalizeEV( Params.AdaptEVMin ) * Params.Width);
	uint	adaptMax	=	(uint)(NormalizeEV( Params.AdaptEVMax ) * Params.Width);
	if (x>=adaptMin && x<=adaptMax) {
		image *= 0.8;
	}
			
	image	=	lerp( image, shade1.rgb, shade1.a );
	image	=	lerp( image, shade2.rgb, shade2.a );
	image	=	lerp( image, shade3.rgb, shade3.a );
			
	return 	image;
}


float3 CellShading(float3 a)
{
	float3 lum = abs(dot( a, float3(0.3,0.5,0.2))) + 0.0001;
	float3 chr = a / lum;
	if (lum.r>0.66) lum = 1.0 * float3(1,1,1.0f); else
	if (lum.r>0.33) lum = 0.8 * float3(1,1,1.0f); else
	if (lum.r>0.11) lum = 0.7 * float3(1,1,1.0f); else
		lum = 0.5;
	
	return chr * lum;
}

float4 PSMain(float4 position : SV_POSITION, float2 uv : TEXCOORD0 ) : SV_Target
{
	uint width;
	uint height;
	uint xpos = position.x;
	uint ypos = position.y;
	FinalHdrImage.GetDimensions( width, height );

	//
	//	Read images :
	//
	float3	hdrImage	=	FinalHdrImage	.SampleLevel( LinearSampler, uv, 0 ).rgb;
	float3	bloom0		=	BloomTexture  	.SampleLevel( LinearSampler, uv, 0 ).rgb;
	float3	bloom1		=	BloomTexture  	.SampleLevel( LinearSampler, uv, 1 ).rgb;
	float3	bloom2		=	BloomTexture  	.SampleLevel( LinearSampler, uv, 2 ).rgb;
	float3	bloom3		=	BloomTexture  	.SampleLevel( LinearSampler, uv, 3 ).rgb;
	float3	bloom4		=	BloomTexture  	.SampleLevel( LinearSampler, uv, 4 ).rgb;
	float4	bloomMask1	=	BloomMask1.SampleLevel( LinearSampler, uv, 0 );
	float4	bloomMask2	=	BloomMask2.SampleLevel( LinearSampler, uv, 0 );
	float4	bloomMask	=	lerp( bloomMask1, bloomMask2, Params.DirtMaskLerpFactor );
	float	luminanceEV	=	MasuredLuminance.Load(int3(0,0,0)).r;
	float	noiseDither	=	NoiseTexture.Load( int3(xpos%64,ypos%64,0) ).r;
	float3	vignette	=	VignmetteTexture.SampleLevel( LinearSampler, uv, 0 ).rgb;
			vignette	=	lerp( float3(1,1,1), vignette, 0*Params.VignetteAmount );
	
	float 	luminanceAdaptEV		=	((luminanceEV - Params.EVMin) * Params.EVRangeInverse) * (Params.AdaptEVMax - Params.AdaptEVMin) + Params.AdaptEVMin;
	float	luminanceAdaptLinear	=	EVToLuminance( luminanceAdaptEV );
	float	luminanceLinear			=	EVToLuminance( luminanceEV );

	float3	bloom		=	( bloom0 * 1.000f  
							+ bloom1 * 2.000f  
							+ bloom2 * 3.000f  
							+ bloom3 * 4.000f 
							+ bloom4 * 5.000f 
							)/15.000f;//*/
							
	if (isnan(bloom.x)) {
		bloom = 0;
	}
					
	bloom	*=	bloomMask.rgb;
	
	hdrImage			=	lerp( hdrImage * bloomMask.rgb, bloom, saturate(bloomMask.a * Params.DirtAmount + Params.BloomAmount));
	
	hdrImage			*=	vignette;

	//
	//	Tonemapping :
	//	
	float3	exposured	=	Params.KeyValue * hdrImage / luminanceAdaptLinear;
			exposured.r	=	EvalLogContrastFunc( exposured.r, 0.18, 1.2f );
			exposured.g	=	EvalLogContrastFunc( exposured.g, 0.18, 1.2f );
			exposured.b	=	EvalLogContrastFunc( exposured.b, 0.18, 1.2f );
	float3	tonemapped	=	Tonemap( exposured );

	
	//
	//	Color grading :
	//	
	float	brightness		=	sqrt(dot( tonemapped, lumVector));
	float	shadows			=	saturate( 1 - 2 * brightness );
	float	midtones		=	saturate( 1-abs(brightness*2-1) );
	float	highlights		=	saturate( 2 * brightness - 1 );
	
	
	float3	tintShadows		=	float3( 0.50, 0.50, 1.00 );
	float3	tintMidtones	=	float3( 1.50, 1.50, 1.50 );
	float3	tintHighlights	=	float3( 1.00, 1.00, 0.90 );
	
	float3	colorShadows	=	tonemapped * shadows	* tintShadows	 ;
	float3	colorMidtones	=	tonemapped * midtones	* tintMidtones   ;
	float3	colorHighlights	=	tonemapped * highlights	* tintHighlights ;
	
	float3 	colorGraded		=	(colorShadows + colorMidtones + colorHighlights);
	colorGraded				=	ColorSaturation( colorGraded, 0.75 );
	
	//colorGraded 	=	tonemapped;
	
	//colorGraded		=	CellShading(colorGraded) * contour + noiseDither*0.1;
	
	//
	//	Apply dithering :
	//
	float3 	result	=	Dither( colorGraded, xpos, ypos, Params.DitherAmount / 256.0f );


	#ifdef SHOW_HISTOGRAM
	result	=	ShowHistogram( xpos, ypos, result, luminanceLinear, luminanceAdaptLinear );
	#endif
	
	return  float4( result, dot(result,lumVector) );
}

#endif











