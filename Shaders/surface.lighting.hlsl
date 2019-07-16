
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define USE_SOLID_SHADOW

#include "brdf.fxi"
#include "surface.shadows.hlsl"
#include "surface.cubemap.hlsl"
#include "shl1.fxi"

float3 SaturateColor ( float3 rgbVal, float factor )
{
	float3 grey = dot(float3(0.25,0.25,0.50),rgbVal);
	float3 ret = grey + factor * (rgbVal-grey);	
	return ret;
}




float computeSpecOcclusion ( float NdotV , float AO , float roughness )
{
	return saturate (pow( NdotV + AO , exp2 ( -16.0f * roughness - 1.0f )) - 1.0f + AO );
}

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( PSInput input, Texture3D<uint2> clusterTable, float2 vpSize, float3 baseColor, float3 worldNormal, float3 triNormal, float roughness, float metallic, float occlusion, float2 lmCoord )
{
	uint i,j,k;
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-input.ProjPos.w*0.015625f);
	int3 loadUVW		=	int3( input.Position.xy/vpSize*float2(16,8), slice * 24 );

	float2	smSize;
	float2	smSizeRcp;
	ShadowMap.GetDimensions( smSize.x, smSize.y );
	smSizeRcp.xy	=	1 / smSize.xy;
	
	uint2	data		=	clusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	decalCount	=	(data.g & 0x00FFF000) >> 12;
	uint 	lightCount	=	(data.g & 0x00000FFF) >> 0;
	uint 	lpbCount	=	(data.g & 0xFF000000) >> 24;

	float3 totalLight	=	0;
	float3 totalAmbient	=	0;

	float3 	worldPos	= 	input.WorldPos.xyz;
	float3  cameraPos	=	Stage.ViewPos.xyz;
	float3 	normal 		=	worldNormal;
	
	float3	viewDir		=	Stage.ViewPos.xyz - worldPos.xyz;
	float	viewDistance=	length( viewDir );
	float3	viewDirN	=	normalize( viewDir );

	float3	geometryNormal	=	normalize(input.Normal);
	float	decalSlope		=	dot( viewDirN, geometryNormal );
	float	decalBaseMip	=	log2( input.ProjPos.w / decalSlope );

	//----------------------------------------------------------------------------------------------
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<decalCount; i++) {
		uint idx = LightIndexTable.Load( lightCount + index + i );
		
		DECAL decal = DecalDataTable[idx];

		float4x4 decalMatrixI	=	decal.DecalMatrixInv;
		float3	 decalColor		=	decal.BaseColorMetallic.rgb;
		float3	 glowColor		=	decal.EmissionRoughness.rgb;
		float	 decalR			=	decal.EmissionRoughness.a * decal.EmissionRoughness.a;
		float	 decalM			=	decal.BaseColorMetallic.a;
		float4	 scaleOffset	=	decal.ImageScaleOffset;
		float	 falloff		=	decal.FalloffFactor;
		float 	 mipDecalBias	=	decal.MipBias;
		
		float4 decalPos	=	mul(float4(worldPos,1), decalMatrixI);
		
		if ( abs(decalPos.x)<1 && abs(decalPos.y)<1 && abs(decalPos.z)<1 && Instance.Group==decal.AssignmentGroup ) {
		
			//float2 uv			=	mad(mad(decalPos.xy, float2(-0.5,0.5), float2(0.5,0.5), offsetScale.zw, offsetScale.xy); 
			float2 uv			=	mad(decalPos.xy, scaleOffset.xy, scaleOffset.zw); 
		
			float4 decalImage	= 	DecalImages.SampleLevel( DecalSampler, uv, decalBaseMip + mipDecalBias );
			float3 localNormal  = 	normalize(decalImage.xyz * 2 - 1);
			float3 decalNormal	=	localNormal.x * decal.BasisX.xyz + localNormal.y * decal.BasisY.xyz + localNormal.z * decal.BasisZ.xyz;
			float factor		=	pow(decalImage.a, 2.2f) * saturate(falloff - abs(decalPos.z)*falloff);
			
			totalLight.rgb		+=	 glowColor * factor;
		
			baseColor 	= lerp( baseColor.rgb, decalColor, decal.ColorFactor * factor );
			roughness 	= lerp( roughness, decalR, decal.SpecularFactor * factor );
			metallic 	= lerp( metallic,  decalM, decal.SpecularFactor * factor );
			normal		= lerp( normal, decalNormal, decal.NormalMapFactor * factor );

			//normal		= normal + decalNormal * decal.NormalMapFactor * factor;
			
			//baseColor	=	0;
		}
	}
#endif	
	
	//----------------------------------------------------------------------------------------------
	int3 checker  = (int3)abs(worldPos.xyz);
	int3 checker2 = (int3)abs(worldPos.xyz*5);
	
	float normalLength	=	length(normal) + 0.00001f;
	
			normal 		= 	normal / normalLength;
			
	// pow3 help to reduce white fringes in metallic PBR pipeline:
	float3  insulatorF0	=	float3(0.04f,0.04f,0.04f);
	float3	diffuse 	=	pow( lerp( pow(baseColor, 1/3.0),   float3(0,0,0),           metallic ), 3 );
	float3	specular  	=	pow( lerp( pow(insulatorF0, 1/3.0), pow(baseColor, 1/3.0f),  metallic ), 3 );
	
	#ifdef DIFFUSE_ONLY	
	diffuse	=	baseColor;
	#endif

	#ifndef DIFFUSE_ONLY
	roughness	=	saturate(roughness);
	roughness	=	clamp( roughness, 1.0f / 512.0f, 1 );
	#endif
	
	//roughness = 0.2f;
	//diffuse = 0.5f;

	//----------------------------------------------------------------------------------------------

	// TODO: check each cluster against lowres cascade	to check completly obscured ones 
	// It could be done in compute shader
	// 1 bit cluster table in of extra data is required.
	if (1) { 
	
		float2	vpos		=	input.Position.xy;
	
		float3	lightDir	=	-Stage.DirectLightDirection.xyz;
		float3	intensity	=	Stage.DirectLightIntensity.rgb;
		float3	lightDirN	=	normalize(lightDir);
		float	srcRadius	=	Stage.DirectLightAngularSize;

		float3	shadow		=	ComputeCSM( vpos, triNormal, lightDirN, worldPos, Stage, ShadowSampler, ParticleSampler, ShadowMap, ShadowMapParticles, true ); 
		
		float  nDotL		= 	max( 0, dot(normal, lightDirN) );
		
		totalLight.rgb 		+= 	shadow * Lambert ( normal.xyz,  lightDirN, intensity, diffuse );
		
		#ifndef DIFFUSE_ONLY	
		totalLight.rgb 		+= 	shadow * nDotL * CookTorrance( normal.xyz, viewDirN, lightDirN, intensity, specular, roughness, srcRadius );
		#endif
	}
	
	//return totalLight;
	
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<lightCount; i++) {
		uint idx  	= LightIndexTable.Load( index + i );
		uint type 	= LightDataTable[idx].LightType & 0x0000FFFF;
		uint shape	= LightDataTable[idx].LightType & 0xFFFF0000;	
		
		float3 position		=	LightDataTable[idx].PositionRadius.xyz;
		float  radius		=	LightDataTable[idx].PositionRadius.w;
		float3 intensity	=	LightDataTable[idx].IntensityFar.rgb;
		float  sourceRadius	=	LightDataTable[idx].SourceRadius;
		
		[branch]
		if (type==LightTypeOmni) {
			
			float3 lightDir		= 	position - worldPos.xyz;
			float3 lightDirN	=	normalize(lightDir);
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			float  nDotL		= 	max( 0, dot(normal, lightDirN) );
			
			totalLight.rgb 		+= 	falloff * Lambert ( normal.xyz,  lightDirN, intensity, diffuse );
			
			#ifndef DIFFUSE_ONLY	
			totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness, sourceRadius );
			#endif
			
		} else if (type==LightTypeAmbient) {
			
			float3 lightDir		= 	position - worldPos.xyz;
			float3 lightDirN	=	normalize(lightDir);
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			float  nDotL		= 	max( 0, 0.5+0.5*dot(normal, lightDirN) );
			
			totalAmbient		+=	falloff * nDotL * intensity;
			
		} else if (type==LightTypeSpotShadow) {
			
			float4 lsPos		=	mul(float4(worldPos+geometryNormal * 0.0,1), LightDataTable[idx].ViewProjection);
			float  shadowDepth	=	lsPos.z / LightDataTable[idx].IntensityFar.w;
				   lsPos.xyz	= 	lsPos.xyz / lsPos.w;
				   
			if ( abs(lsPos.x)<1 && abs(lsPos.y)<1 && abs(lsPos.z)<1 ) {
				float3 	position	=	LightDataTable[idx].PositionRadius.xyz;
				float  	radius		=	LightDataTable[idx].PositionRadius.w;
				float3 	intensity	=	LightDataTable[idx].IntensityFar.rgb;
				float4 	scaleOffset	=	LightDataTable[idx].ShadowScaleOffset;
				
				lsPos.xy		=	mad( lsPos.xy, scaleOffset.xy, scaleOffset.zw );
						
				float	accumulatedShadow	=	0;
						
				//	TODO : num taps <--> shadow quality
				//	TODO : kernel size <--> shadow region size
				#if 0
				accumulatedShadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, lsPos.xy, shadowDepth ).r;
				#else
				for( float row = -2; row <= 2; row += 1 ) {
					[unroll]for( float col = -2; col <= 2; col += 1 ) {
						float2	smcoord	=	mad(float2(col,row), smSizeRcp.xy, lsPos.xy);
						float	shadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, smcoord, shadowDepth ).r;
						accumulatedShadow += shadow;
					}
				}
				accumulatedShadow 	/= 	25.0f;
				//accumulatedShadow	=	max(0,mad(accumulatedShadow, 1/25.0f*2.0f, -0.5));
				#endif
						
				float3	prtShadow	=	ShadowMapParticles.SampleLevel( ParticleSampler, lsPos.xy, 0 ).rgb;
				
				float3 	lightDir	= 	position - worldPos.xyz;
				float3 	falloff		= 	LinearFalloff( length(lightDir), radius ) * accumulatedShadow * prtShadow;
				float  	nDotL		= 	max( 0, dot(normal, normalize(lightDir)) );
				
				totalLight.rgb 		+= 	falloff * Lambert ( normal.xyz,  lightDir, intensity, diffuse );
				
				#ifndef DIFFUSE_ONLY	
				totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness, sourceRadius );
				#endif
			}
		}
	}
	
	//----------------------------------------------------------------------------------------------
	//	https://github.com/demiurghg/IronStar/blob/ed5d9348552548bd7a187a436894a6b27a5d8ea9/Shaders/lighting.hlsl
	//----------------------------------------------------------------------------------------------
	float3	ambientDiffuse		=	float3(0,0,0);
	float3	ambientSpecular		=	float3(0,0,0);
	float3	ambientReflection	=	float3(0,0,0);
	float	ambientLuminance	=	1;
	
	//
	//	LIGHTMAP :
	//
	//SamplerPoint
	//SamplerLinear
	float4	irradianceR	=	float4(0,0,0,0);
	float4	irradianceG	=	float4(0,0,0,0);
	float4	irradianceB	=	float4(0,0,0,0);
	float3	volumeCoord	=	float3(0,0,0);
	
	#ifdef IRRADIANCE_MAP
		irradianceR		=	IrradianceMapR.Sample( SamplerLinear, lmCoord );
		irradianceG		=	IrradianceMapG.Sample( SamplerLinear, lmCoord );
		irradianceB		=	IrradianceMapB.Sample( SamplerLinear, lmCoord );
		float	lightR	=	EvalSHL1Smooth( irradianceR, normalize(normal) );
		float	lightG	=	EvalSHL1Smooth( irradianceG, normalize(normal) );
		float	lightB	=	EvalSHL1Smooth( irradianceB, normalize(normal) );//*/
		ambientDiffuse	=	float3( lightR, lightG, lightB );
	#endif
	#ifdef IRRADIANCE_VOLUME
		volumeCoord		=	mul(float4(worldPos.xyz,1), Stage.OcclusionGridMatrix );
		irradianceR		=	IrradianceVolumeR.Sample( SamplerLinear, volumeCoord );
		irradianceG		=	IrradianceVolumeG.Sample( SamplerLinear, volumeCoord );
		irradianceB		=	IrradianceVolumeB.Sample( SamplerLinear, volumeCoord );
		float	lightR	=	EvalSHL1Smooth( irradianceR, normalize(normal) );
		float	lightG	=	EvalSHL1Smooth( irradianceG, normalize(normal) );
		float	lightB	=	EvalSHL1Smooth( irradianceB, normalize(normal) );//*/
		ambientDiffuse	=	SaturateColor( float3( lightR, lightG, lightB ), 1.3f );
	#endif
	
	/*float	lightR		=	max(0, irradianceR.x + dot( normal, irradianceR.wyz ));
	float	lightG		=	max(0, irradianceG.x + dot( normal, irradianceG.wyz ));
	float	lightB		=	max(0, irradianceB.x + dot( normal, irradianceB.wyz ));//*/
	
	

	#ifdef IRRADIANCE_MAP
	ambientLuminance	=	saturate((lightR.x + lightG.x + lightB.x)/3);
	#endif
	#ifdef IRRADIANCE_VOLUME
	ambientLuminance	=	1;
	#endif
	
	//
	//	APPROX SPECULAR :
	//
	/*float3 approxDir		=	(irradianceR.wyz + irradianceG.wyz + irradianceB.wyz)/3;
	float  approxLength		=	length(approxDir) + 0.0001;
	float  focus			=	approxLength / ambientLuminance / 1.5f;
		   approxDir		/=	approxLength;
	float3 approxLight		=	float3( irradianceR.x, irradianceG.x, irradianceB.x );
	float  approxRoughness	=	saturate( 1 - ( 1 - roughness ) * sqrt(focus) );
	float  nDotLApprox		=	saturate(dot( normal.xyz, approxDir ));
	ambientSpecular 		= 	nDotLApprox * CookTorrance( normal.xyz, viewDirN, approxDir, approxLight, specular, roughness, focus );//*/
	
	
	//
	//	OCCLUSION :
	//
#ifdef TRANSPARENT
	float 	ssaoFactor	=	1;
#else
	float 	ssaoFactor	=	AmbientOcclusion.Load( int3( input.Position.xy, 0 ) ).r;
#endif	
	ssaoFactor	=	lerp( 1, ssaoFactor, Stage.SsaoWeight );
	ssaoFactor	*=	occlusion;
	
	//
	//	REFLECTION :
	//
	float3	reflectDir		=	reflect( -viewDirN, normal.xyz );
	float	selfOcclusion	=	saturate( dot( normalize(normal.xyz), normalize(input.Normal.xyz) ) );
	float	NoV 			= 	dot(viewDirN, normal.xyz);
	float2 	ab				=	EnvLut.SampleLevel( SamplerLinearClamp, float2(roughness, 1-NoV), 0 ).xy;
	float	ssaoFactorDiff	=	pow(ssaoFactor, 2);// < 0.7 ? 0 : 1;
	float	ssaoFactorSpec	=	pow(ssaoFactor, 4);// < 0.7 ? 0 : 1;
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<lpbCount; i++) {
		uint 		idx			= 	LightIndexTable.Load( lightCount + decalCount + index + i );
		float3		innerRange	=	float3( ProbeDataTable[idx].NormalizedWidth, ProbeDataTable[idx].NormalizedHeight, ProbeDataTable[idx].NormalizedDepth );
		float4x4	lpbMatrixI	=	ProbeDataTable[idx].MatrixInv;
		float3		cubePos		=	mul(float4(worldPos,1), lpbMatrixI ).xyz;
		float3 		cubeMapPos	=	ProbeDataTable[idx].Position.xyz;
		uint   		imageIndex	=	ProbeDataTable[idx].ImageIndex;
		
		float3 		randColor	=	float3( (imageIndex/2)%2, (imageIndex/4)%2, (imageIndex/8)%2 );
		
		float3		factor3		=	abs(cubePos);
					factor3		=	(factor3 - innerRange) / (float3(1,1,1) - innerRange);
		float		factor		=	1 - saturate( max(factor3.x, max(factor3.y, factor3.z)) );
		
		float3	reflectVector	=	SpecularParallaxCubeMap( worldPos, cameraPos, normal, cubeMapPos, lpbMatrixI ) * float3(-1,1,1);
		float3	diffuseVector	=	DiffuseParallaxCubeMap ( worldPos, cameraPos, normal, cubeMapPos, lpbMatrixI );

		//float3	diffTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(normal.xyz, imageIndex), LightProbeDiffuseMip).rgb;
		float		mipLevel	=	(sqrt(roughness) * LightProbeMaxSpecularMip);
		
		float3	specTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(reflectVector, imageIndex), mipLevel ).rgb / 2;
		float3	specTermLL	=	dot( float3(0.3,0.5,0.2), RadianceCache.SampleLevel( SamplerLinearClamp, float4(reflectVector, imageIndex), 1 ).rgb );
		
		/*if (input.Position.x>640) {
			specTerm = specTerm / 5 * SaturateColor(ambientDiffuse, 1.0f);
			//specTerm = dot( float3(0.3,0.5,0.2), specTerm ) / 5 * SaturateColor(ambientDiffuse, 1.5f);
		}*/
		
		ambientReflection	=	lerp( ambientReflection, specTerm, factor );
	}
#endif	

	//ambientReflection	*=	pow(ambientLuminance,2);

	//----------------------------------------------------------------------------------------------
	
	ambientDiffuse		=	ambientDiffuse  * ( diffuse                ) * ssaoFactorDiff;
	ambientSpecular		=	ambientSpecular	* ( specular * ab.x + ab.y ) * ssaoFactorSpec * selfOcclusion;
	ambientReflection	=	ambientReflection * ( specular /* ab.x + ab.y*/ ) * ssaoFactorSpec * selfOcclusion * ambientLuminance;
	
	totalLight.xyz	+=	ambientDiffuse + ambientSpecular + ambientReflection;

	//----------------------------------------------------------------------------------------------

	return totalLight;
}

