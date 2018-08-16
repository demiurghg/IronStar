
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define USE_SOLID_SHADOW

#include "brdf.fxi"
#include "surface.shadows.hlsl"

float computeSpecOcclusion ( float NdotV , float AO , float roughness )
{
	return saturate (pow( NdotV + AO , exp2 ( -16.0f * roughness - 1.0f )) - 1.0f + AO );
}

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( PSInput input, Texture3D<uint2> clusterTable, float2 vpSize, float3 baseColor, float3 worldNormal, float3 triNormal, float roughness, float metallic )
{
	uint i,j,k;
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-input.ProjPos.w*0.03);
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
	float3 	normal 		=	worldNormal;
	
	float3	viewDir		=	Stage.ViewPos.xyz - worldPos.xyz;
	float	viewDistance=	length( viewDir );
	float3	viewDirN	=	normalize( viewDir );

	float3	geometryNormal	=	normalize(input.Normal);
	float	decalSlope		=	dot( viewDirN, geometryNormal );
	float	decalBaseMip	=	log2( input.ProjPos.w / decalSlope );

	//----------------------------------------------------------------------------------------------
	
	[loop]
	for (i=0; i<decalCount; i++) {
		uint idx = LightIndexTable.Load( lightCount + index + i );
		
		DECAL decal = DecalDataTable[idx];

		float4x4 decalMatrixI	=	decal.DecalMatrixInv;
		float3	 decalColor		=	decal.BaseColorMetallic.rgb;
		float3	 glowColor		=	decal.EmissionRoughness.rgb;
		float	 decalR			=	decal.EmissionRoughness.a;
		float	 decalM			=	decal.BaseColorMetallic.a;
		float4	 scaleOffset	=	decal.ImageScaleOffset;
		float	 falloff		=	decal.FalloffFactor;
		float 	 mipDecalBias	=	decal.MipBias;
		
		float4 decalPos	=	mul(float4(worldPos,1), decalMatrixI);
		
		if ( abs(decalPos.x)<1 && abs(decalPos.y)<1 && abs(decalPos.z)<1 && Instance.Group==decal.AssignmentGroup ) {
		
			//float2 uv			=	mad(mad(decalPos.xy, float2(-0.5,0.5), float2(0.5,0.5), offsetScale.zw, offsetScale.xy); 
			float2 uv			=	mad(decalPos.xy, scaleOffset.xy, scaleOffset.zw); 
		
			float4 decalImage	= 	DecalImages.SampleLevel( DecalSampler, uv, decalBaseMip + mipDecalBias );
			float3 localNormal  = 	decalImage.xyz * 2 - 1;
			float3 decalNormal	=	localNormal.x * decal.BasisX.xyz + localNormal.y * decal.BasisY.xyz + localNormal.z * decal.BasisZ.xyz;
			float factor		=	decalImage.a * saturate(falloff - abs(decalPos.z)*falloff);
			
			totalLight.rgb		+=	 glowColor * factor;
		
			baseColor 	= lerp( baseColor.rgb, decalColor, decal.ColorFactor * factor );
			roughness 	= lerp( roughness, decalR, decal.SpecularFactor * factor );
			metallic 	= lerp( metallic,  decalM, decal.SpecularFactor * factor );
			///normal		= lerp( normal, decalNormal, decal.NormalMapFactor * factor );

			normal		= normal + decalNormal * decal.NormalMapFactor * factor;
		}
	}
	
	
	//----------------------------------------------------------------------------------------------
	int3 checker  = (int3)abs(worldPos.xyz);
	int3 checker2 = (int3)abs(worldPos.xyz*5);
	
	/*metallic = (checker.x + checker.y + checker.z)%2;
	baseColor = 0.3;
	roughness = (checker2.x + checker2.y + checker2.z)%7 / 10.0f + 0.1;//*/
	//roughness = frac(worldPos.y-0.1);
	
			normal 		= 	normalize(normal);
	float3	diffuse 	=	lerp( baseColor, float3(0,0,0), metallic );
	float3	specular  	=	lerp( float3(0.04f,0.04f,0.04f), baseColor, metallic );

	roughness	=	sqrt(roughness);
	
	//roughness	=	0;
	
	/*roughness	=	0.75f;
	specular	=	1.0f;
	diffuse		=	0.0f;//*/

	/*roughness	=	0.125f;
	specular	=	0.0f;
	diffuse		=	1.0f;//*/

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
		totalLight.rgb 		+= 	shadow * nDotL * CookTorrance( normal.xyz, viewDirN, lightDirN, intensity, specular, roughness, srcRadius );
	}
	
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
			totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness, sourceRadius );
			
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
				totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness, sourceRadius );
			}
		}
	}
	
	//----------------------------------------------------------------------------------------------
	
	//
	//	https://github.com/demiurghg/IronStar/blob/ed5d9348552548bd7a187a436894a6b27a5d8ea9/Shaders/lighting.hlsl
	//
	
	float3	ambientDiffuse		=	float3(0,0,0);
	float3	ambientSpecular		=	float3(0,0,0);
	float3	ambientDiffuseSky	=	float3(0,0,0);

	//	occlusion & sky stuff :
	float 	ssaoFactor		=	AmbientOcclusion.Load( int3( input.Position.xy,0 ) ).r;
	float4	aogridValue		=	OcclusionGrid.Sample( SamplerLinear, mul( float4(worldPos + geometryNormal * 0.9f, 1), Stage.OcclusionGridMatrix ).xyz ).rgba;
			aogridValue.xyz	=	aogridValue.xyz * 2 - 1;
			
	float 	skyFactor		=	length( aogridValue.xyz );
	float3 	skyBentNormal	=	aogridValue.xyz / (skyFactor + 0.1);
	
	float 	fullSkyLight	=	max( 0, dot( skyBentNormal, normal ) * 0.5 + 0.5 );
	float 	halfSkyLight	=	max( 0, dot( skyBentNormal, normal ) * 1.0 + 0.0 );
	float3	skyLight		=	skyFactor * max(0, lerp(halfSkyLight, fullSkyLight, skyFactor ) ) * Stage.SkyAmbientLevel;

#ifdef TRANSPARENT
	ssaoFactor	=	1;
#endif
	ssaoFactor	=	lerp( 1, ssaoFactor, Stage.SsaoWeight );
	
	
	float	NoV 			= 	dot(viewDirN, normal.xyz);
	float2 	ab				=	EnvLut.SampleLevel( SamplerLinearClamp, float2(roughness, 1-NoV), 0 ).xy;
	float	ssaoFactorDiff	=	pow(ssaoFactor * aogridValue.w, 2);
	float	ssaoFactorSpec	=	pow(ssaoFactor * aogridValue.w, 2);//computeSpecOcclusion( NoV, ssaoFactor * aogridValue.w, roughness );
	
	[loop]
	for (i=0; i<lpbCount; i++) {
		uint idx  			= 	LightIndexTable.Load( lightCount + decalCount + index + i );
		float3 position		=	ProbeDataTable[idx].Position.xyz;
		float  innerRadius	=	ProbeDataTable[idx].InnerRadius;
		float  outerRadius	=	ProbeDataTable[idx].OuterRadius;
		uint   imageIndex	=	ProbeDataTable[idx].ImageIndex;
		
		float	localDist	=	distance( position.xyz, worldPos.xyz );
		float	factor		=	saturate( 1 - (localDist-innerRadius)/(outerRadius-innerRadius) );

	#if 1
		float3	diffTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(normal.xyz, imageIndex), LightProbeDiffuseMip).rgb;
		float3	specTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(reflect(-viewDir, normal.xyz), imageIndex), roughness*LightProbeMaxSpecularMip ).rgb;
	#else
		float3	diffTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(normal.xyz, imageIndex), LightProbeMaxSpecularMip).rgb;
		float3	specTerm	=	RadianceCache.SampleLevel( SamplerLinearClamp, float4(reflect(-viewDir, normal.xyz), imageIndex), roughness*LightProbeMaxSpecularMip ).rgb;
		skyFactor			=	0;
	#endif

		ambientDiffuse		=	lerp( ambientDiffuse,  diffTerm, factor );
		ambientSpecular		=	lerp( ambientSpecular, specTerm, factor );//*/
	}

	ambientDiffuse		=	ambientDiffuse  * ( diffuse                ) * ssaoFactorDiff;
	ambientSpecular		=	ambientSpecular	* ( specular * ab.x + ab.y ) * ssaoFactorSpec;
	ambientDiffuseSky	=	diffuse * skyLight * pow(ssaoFactor,2);
	
	totalLight.xyz	+=	ambientDiffuse + ambientDiffuseSky + ambientSpecular;	
	
	//----------------------------------------------------------------------------------------------

	return totalLight;
}

