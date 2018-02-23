

#if 0
$ubershader		(TONEMAPPING LINEAR|REINHARD|FILMIC)|MEASURE_ADAPT
$ubershader		COMPOSITION
#endif

struct PARAMS {
	float 	AdaptationRate;
	float 	LuminanceLowBound;
	float	LuminanceHighBound;
	float	KeyValue;
	float	BloomAmount;
	float	DirtMaskLerpFactor;
	float	DirtAmount;
	float	Saturation;
	float	Maximum;
	float	Minimum;
};

SamplerState	LinearSampler		: register(s0);
	
cbuffer PARAMS 		: register(b0) { 
	PARAMS Params 	: packoffset(c0); 
};


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
	float3	turbidBack		=	TurbidBackground.SampleLevel( LinearSampler, uv + distortGlass.xy + distortPrt.xy, blurFactor*4 ).rgb;
	
			hdrImageSolid	=	lerp( hdrImageSolid.rgb, turbidBack.rgb, saturate(blurFactor*8) );

	//	Compose solid and transparent objects :
			hdrImageSolid	=	lerp( hdrImageSolid.rgb, hdrImageGlass.rgb, hdrImageGlass.a );
			
	//	Get soft particles :
	float4	softPrtFront	=	SoftParticlesFront.SampleLevel( LinearSampler, uv + distortPrt.xy, 0 ).rgba;

	//	Compose solid+transparent and soft particles :
	float3	hdrImageFinal	=	lerp( hdrImageSolid.rgb, softPrtFront.rgb, softPrtFront.a );
	
	return float4( hdrImageFinal * float3(1,1,1), 1 );
}


#endif

/*-----------------------------------------------------------------------------
	Tonemapping and final color grading :
-----------------------------------------------------------------------------*/

#ifdef TONEMAPPING

Texture2D		FinalHdrImage 		: register(t0);
Texture2D		MasuredLuminance	: register(t1);
Texture2D		BloomTexture		: register(t2);
Texture2D		BloomMask1			: register(t3);
Texture2D		BloomMask2			: register(t4);
Texture2D		NoiseTexture		: register(t5);


float4 VSMain(uint VertexID : SV_VertexID, out float2 uv : TEXCOORD) : SV_POSITION
{
	uv = FSQuadUV( VertexID );
	return FSQuad( VertexID );
}

static const float dither[4][4] = {{1,9,3,11},{13,5,15,7},{4,12,2,10},{16,8,14,16}};


float3 Dither ( int xpos, int ypos, float3 color )
{
	color += dither[(xpos+ypos/7)%4][(ypos+xpos/7)%4]/256.0f/5;
	color -= dither[(ypos+xpos/7)%4][(xpos+ypos/7)%4]/256.0f/5;//*/
	return color;
}



static const float3	lumVector	=	float3(0.3f,0.6f,0.2f);


float3 SaturateColor ( float3 rgbVal, float factor )
{
	float3 grey = dot(lumVector,rgbVal);
	float3 ret = grey + factor * (rgbVal-grey);	
	return ret;
}


float3 TintColor ( float3 target, float3 blend )
{
	//return target * blend;

	float3 multiplyFactor	=	target * (blend+0.5);
	float3 screenFactor		=	(1 - (1-target) * (1-(blend-0.5)));
	return lerp( multiplyFactor, screenFactor, step( blend, 0.5 ) );

	// float3 result = 0;
	// result.r = color.r + (1-color.r) * tint.r;
	// result.g = color.g + (1-color.g) * tint.g;
	// result.b = color.b + (1-color.b) * tint.b;
	// return result;
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
	float4	bloomMask1	=	BloomMask1.SampleLevel( LinearSampler, uv, 0 );
	float4	bloomMask2	=	BloomMask2.SampleLevel( LinearSampler, uv, 0 );
	float4	bloomMask	=	lerp( bloomMask1, bloomMask2, Params.DirtMaskLerpFactor );
	float	luminance	=	clamp( MasuredLuminance.Load(int3(0,0,0)).r, 2, 10 );
	float	noiseDither	=	NoiseTexture.Load( int3(xpos%64,ypos%64,0) ).r;

	float3	bloom		=	( bloom0 * 1.000f  
							+ bloom1 * 2.000f  
							+ bloom2 * 3.000f  
							+ bloom3 * 4.000f )/7.000f;//*/
							
	if (isnan(bloom.x)) {
		bloom = 0;
	}
					
	bloom	*=	bloomMask.rgb;
	
	hdrImage			=	lerp( hdrImage * bloomMask.rgb, bloom, saturate(bloomMask.a * Params.DirtAmount + Params.BloomAmount));
	

	//
	//	Tonemapping :
	//	
	float3	exposured	=	Params.KeyValue * hdrImage / luminance;

	#ifdef LINEAR
		float3 	tonemapped	=	pow( abs(exposured), 1/2.2f );
	#endif
	
	#ifdef REINHARD
		float3 tonemapped	=	pow( abs(exposured / (1+exposured)), 1/2.2f );
	#endif
	
	#ifdef FILMIC
		float3 x = max(0,exposured-0.004);
		float3 tonemapped = (x*(6.2*x+.5))/(x*(6.2*x+1.7)+0.06);
	#endif
	
	//
	//	Color grading :
	//	
			luminance		=	dot( tonemapped, lumVector);
	float	shadows			=	saturate( 1 - 2 * luminance );
	float	midtones		=	saturate( 1-abs(luminance*2-1) );
	float	highlights		=	saturate( 2 * luminance - 1 );
	
	float3	tintShadows		=	0.4f; //float3( 0.25, 0.30, 0.35 );
	float3	tintMidtones	=	0.5f; //float3( 0.45, 0.50, 0.55 );
	float3	tintHighlights	=	0.5f; //float3( 0.55, 0.60, 0.65 );
	
	float3	colorShadows	=	TintColor( tonemapped, tintShadows	  );
	float3	colorMidtones	=	TintColor( tonemapped, tintMidtones   );
	float3	colorHighlights	=	TintColor( tonemapped, tintHighlights );
	
	float3	colorGraded 	=	colorShadows * shadows
							+	colorMidtones * midtones
							+	colorHighlights * highlights;
							
	
	colorGraded	=	SaturateColor( colorGraded, 0.75f );
	
	//	DISABLE COLOR GRADING!!!
	colorGraded	=	tonemapped;
	
	//
	//	Apply dithering :
	//
	float3 result	=	colorGraded + (noiseDither*2-1)*3/256.0;
	
	return  float4( result, dot(result,lumVector) );
}

#endif











