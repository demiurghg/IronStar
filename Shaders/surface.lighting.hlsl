
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"
#include "shadows.fxi"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( PSInput input, Texture3D<uint2> clusterTable, float2 vpSize, float3 baseColor, float3 worldNormal, float roughness, float metallic )
{
	uint i,j,k;
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-input.ProjPos.w*0.03);
	int3 loadUVW		=	int3( input.Position.xy/vpSize*float2(16,8), slice * 24 );
	
	uint2	data		=	clusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	decalCount	=	(data.g & 0xFFF000) >> 12;
	uint 	lightCount	=	(data.g & 0x000FFF) >> 0;

	float3 totalLight	=	0;

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
		float3	 decalR			=	decal.EmissionRoughness.a;
		float3	 decalM			=	decal.BaseColorMetallic.a;
		float4	 scaleOffset	=	decal.ImageScaleOffset;
		float	 falloff		=	decal.FalloffFactor;
		float 	 mipDecalBias	=	decal.MipBias;
		
		float4 decalPos	=	mul(float4(worldPos,1), decalMatrixI);
		
		if ( abs(decalPos.x)<1 && abs(decalPos.y)<1 && abs(decalPos.z)<1 && Instance.AssignmentGroup==decal.AssignmentGroup ) {
		
			//float2 uv			=	mad(mad(decalPos.xy, float2(-0.5,0.5), float2(0.5,0.5), offsetScale.zw, offsetScale.xy); 
			float2 uv			=	mad(decalPos.xy, scaleOffset.xy, scaleOffset.zw); 
		
			float4 decalImage	= 	DecalImages.SampleLevel( DecalSampler, uv, decalBaseMip + mipDecalBias );
			float3 localNormal  = 	decalImage.xyz * 2 - 1;
			float3 decalNormal	=	localNormal.x * decal.BasisX + localNormal.y * decal.BasisY + localNormal.z * decal.BasisZ;
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

			normal 		= 	normalize(normal);
	float3	diffuse 	=	lerp( baseColor, float3(0,0,0), metallic );
	float3	specular  	=	lerp( float3(0.04f,0.04f,0.04f), baseColor, metallic );

	//----------------------------------------------------------------------------------------------

	// TODO: check each cluster against lowres cascade	to check completly obscured ones 
	// It could be done in compute shader
	// 1 bit cluster table in of extra data is required.
	if (1) { 
	
		float2	vpos		=	input.Position.xy;
		float3	shadow		=	ComputeCSM( vpos, geometryNormal, worldPos, Stage, ShadowSampler, ParticleSampler, ShadowMap, ShadowMapParticles, true ); 
	
		float3	lightDir	=	-Stage.DirectLightDirection.xyz;
		float3	intensity	=	Stage.DirectLightIntensity.rgb;
		float3	lightDirN	=	normalize(lightDir);
	
		float  nDotL		= 	max( 0, dot(normal, lightDirN) );
		
		totalLight.rgb 		+= 	shadow * Lambert ( normal.xyz,  lightDirN, intensity, diffuse );
		totalLight.rgb 		+= 	shadow * nDotL * CookTorrance( normal.xyz, viewDirN, lightDirN, intensity, specular, roughness );
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
		
		[branch]
		if (type==LightTypeOmni) {
			
			float3 lightDir		= 	position - worldPos.xyz;
			float3 lightDirN	=	normalize(lightDir);
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			float  nDotL		= 	max( 0, dot(normal, lightDirN) );
			
			totalLight.rgb 		+= 	falloff * Lambert ( normal.xyz,  lightDirN, intensity, diffuse );
			totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDirN, intensity, specular, roughness );
			
		} else if (type==LightTypeSpotShadow) {
			
			float4 lsPos		=	mul(float4(worldPos+geometryNormal * 0.1,1), LightDataTable[idx].ViewProjection);
			float  shadowDepth	=	lsPos.z / LightDataTable[idx].IntensityFar.w;
				   lsPos.xyz	= 	lsPos.xyz / lsPos.w;
				   
			if ( abs(lsPos.x)<1 && abs(lsPos.y)<1 && abs(lsPos.z)<1 ) {
				float3 	position	=	LightDataTable[idx].PositionRadius.xyz;
				float  	radius		=	LightDataTable[idx].PositionRadius.w;
				float3 	intensity	=	LightDataTable[idx].IntensityFar.rgb;
				float4 	scaleOffset	=	LightDataTable[idx].ShadowScaleOffset;
				
				float	penumbra	=	1;
				if (shape==LightSpotShapeRound) {
					penumbra 	=	max(0, 1 - length(lsPos.xy));
					penumbra	=	min(1, penumbra * 2 );				
					penumbra	*=	penumbra;
				}
				if (shape==LightSpotShapeSquare) {
					penumbra 	= 	max(0, 1 - length(max(abs(lsPos.x), abs(lsPos.y))));
					penumbra	=	min(1, penumbra * 2 );				
					penumbra	*=	penumbra;
				}
				
				lsPos.xy		=	mad( lsPos.xy, scaleOffset.xy, scaleOffset.zw );
						
				float	accumulatedShadow	=	0;
						
				//	TODO : num taps <--> shadow quality
				//	TODO : kernel size <--> shadow region size
				#if 0
				accumulatedShadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, lsPos.xy, shadowDepth ).r;
				#else
				for( float row = -3; row <= 3; row += 1 ) {
					[unroll]for( float col = -3; col <= 3; col += 1 ) {
						float	shadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, mad(float2(col,row), 0.5/1024.0f, lsPos.xy), shadowDepth ).r;
						accumulatedShadow += shadow;
					}
				}
				//accumulatedShadow 	/= 	49.0f;
				accumulatedShadow	=	max(0,mad(accumulatedShadow, 1/49.0f*2.0f, -0.5));
				#endif
						
				float3	prtShadow	=	ShadowMapParticles.SampleLevel( ParticleSampler, lsPos.xy, 0 ).rgb;
				
				float3 	lightDir	= 	position - worldPos.xyz;
				float3 	falloff		= 	LinearFalloff( length(lightDir), radius ) * accumulatedShadow * penumbra * prtShadow;
				float  	nDotL		= 	max( 0, dot(normal, normalize(lightDir)) );
				
				totalLight.rgb 		+= 	falloff * Lambert ( normal.xyz,  lightDir, intensity, diffuse );
				totalLight.rgb 		+= 	falloff * nDotL * CookTorrance( normal.xyz, viewDirN, lightDir, intensity, specular, roughness );
			}
		}
	}
	
	//
	//	Ambient
	//
	totalLight.rgb += (diffuse + specular).rgb * Stage.Ambient;
	
	return totalLight;
}

