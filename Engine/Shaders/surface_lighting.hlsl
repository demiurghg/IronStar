
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( PSInput input, float2 vpSize, SURFACE surface, float3 triNormal, float2 lmCoord )
{
	SHADOW_RESOURCES	rcShadow;
	rcShadow.ShadowSampler		=	ShadowSampler;
	rcShadow.LinearSampler		=	SamplerLinear;
	rcShadow.ShadowMap			=	ShadowMap;
	rcShadow.ShadowMask			=	ShadowMapParticles;
	
	CLUSTER_RESOURCES	rcCluster;
	rcCluster.ClusterArray		=	ClusterArray;
	rcCluster.IndexBuffer		=	ClusterIndexBuffer;
	rcCluster.LightBuffer		=	ClusterLightBuffer;
	rcCluster.DecalBuffer		=	ClusterDecalBuffer;
	rcCluster.LightProbeBuffer	=	ClusterLightProbeBuffer;
	
	LIGHTPROBE_RESOURCES rcLightProbe;
	rcLightProbe.Sampler		=	SamplerLightProbe;
	rcLightProbe.EnvironmentLut	=	EnvLut;
	rcLightProbe.RadianceCache	=	RadianceCache;
	
	LIGHTMAP_RESOURCES rcLightMap;
	rcLightMap.Sampler				=	SamplerLightmap;
	rcLightMap.IrradianceMapL0		=	IrradianceMapL0;
	rcLightMap.IrradianceMapL1		=	IrradianceMapL1;
	rcLightMap.IrradianceMapL2		=	IrradianceMapL2;
	rcLightMap.IrradianceMapL3		=	IrradianceMapL3;
	rcLightMap.IrradianceVolumeL0	=	IrradianceVolumeL0;
	rcLightMap.IrradianceVolumeL1	=	IrradianceVolumeL1;
	rcLightMap.IrradianceVolumeL2	=	IrradianceVolumeL2;
	rcLightMap.IrradianceVolumeL3	=	IrradianceVolumeL3;
	
	DECAL_RESOURCES	rcDecals;
	rcDecals.DecalImages			=	DecalImages;
	rcDecals.DecalSampler			=	DecalSampler;
	
	CLUSTER cluster			=	GetCluster( rcCluster, input.ProjPos );

	uint i;
	uint light_complexity	=	0;
	LIGHTING 	totalLight	=	(LIGHTING)0;

	GEOMETRY geometry 		= 	(GEOMETRY)0;
	geometry.position		=	input.WorldPos;
	geometry.normal			=	normalize(input.Normal);
	
	//----------------------------------------------------------------------------------------------
	//	Apply decals :
	//----------------------------------------------------------------------------------------------
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<cluster.NumDecals; i++) 
	{
		DECAL decal = GetDecal( rcCluster, cluster, i );
		
		surface = ApplyDecal( geometry, surface, Camera, decal, rcDecals, INSTANCE_DATA[0].Group );
		light_complexity++;
	}
	
	ComputeDiffuseSpecular( surface );
#else
	ComputeDiffuseOnly( surface );
#endif

	float3 factor	 = saturate( surface.baseColor.rgb * 50 );

	//----------------------------------------------------------------------------------------------
	//	Ambient occlusion :
	//----------------------------------------------------------------------------------------------

#if defined (TRANSPARENT) || defined(DIFFUSE_ONLY)
	float 	ssaoFactor	=	1;
#else
	float 	ssaoFactor	=	AmbientOcclusion.Load( int3( input.Position.xy, 0 ) ).r;
	//float 	ssaoFactor	=	SRGBToLinear( AmbientOcclusion.Load( int3( input.Position.xy, 0 ) ).r );
	ssaoFactor	=	lerp( 1, ssaoFactor, Stage.SsaoWeight );
	ssaoFactor	*=	surface.occlusion;
#endif	
	
	//----------------------------------------------------------------------------------------------
	//	Reflection :
	//----------------------------------------------------------------------------------------------
	
	float4 	reflection 			=	float4(0,0,0,0);
	#ifdef IRRADIANCE_MAP
	float	roughnessThreshold	=	Stage.SpecularLightmapThreshold;
	float 	specularLMFactor	=	smoothstep( roughnessThreshold, roughnessThreshold + 0.16f, surface.roughness );
	#else
	float 	specularLMFactor	=	0;
	#endif
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<cluster.NumProbes; i++) 
	{
		LIGHTPROBE	probe		=	GetLightProbe( rcCluster, cluster, i );
		float4		envLight	=	ComputeEnvironmentLighting( probe, geometry, surface, Camera, rcLightProbe );
		reflection				=	lerp( reflection, float4(envLight.rgb,1), envLight.a );
		light_complexity++;
	}
	
	reflection.a *= ( 1 - specularLMFactor );
	
	totalLight.specular.rgb		=	reflection.rgb * ssaoFactor * ssaoFactor * reflection.a;
#endif

	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	#ifdef IRRADIANCE_MAP
		LIGHTING lightmap		= 	EvaluateLightmap( rcLightMap, geometry, surface, Camera, lmCoord );
		lightmap.specular.rgb	*=	Stage.SpecularLightmapFactor * (1 - reflection.a );
		AccumulateLighting( totalLight, lightmap, ssaoFactor * Stage.IndirectLightFactor );
	#endif
	#ifdef IRRADIANCE_VOLUME
		float3	volumeCoord		=	mul( float4(geometry.position.xyz, 1), Stage.WorldToLightVolume ).xyz;
		LIGHTING lightmap		=	EvaluateLightVolume( rcLightMap, geometry, surface, Camera, volumeCoord );
		lightmap.specular.rgb	=	0;
		AccumulateLighting( totalLight, lightmap, ssaoFactor * Stage.IndirectLightFactor );
	#endif

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------
	
	if (1)
	{
		LIGHTING lighting = ComputeDirectLight( DirectLight, Camera, geometry, surface, CascadeShadow, rcShadow, input.Position.xy );
		AccumulateLighting( totalLight, lighting, Stage.DirectLightFactor );
	}

	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light		=	GetLight( rcCluster, cluster, i );
		LIGHTING	lighting	=	ComputePointLight( light, Camera, geometry, surface, rcShadow );
		AccumulateLighting( totalLight, lighting, Stage.DirectLightFactor );
		light_complexity++;
	}
	
	//----------------------------------------------------------------------------------------------
	
	totalLight.emissive		+=	surface.emission;
	totalLight.emissive 	+=	Stage.ShowLightComplexity * light_complexity * float3(1,0,0.5f) * 0.25f;

	//----------------------------------------------------------------------------------------------

	return 	totalLight.emissive
		+ 	totalLight.diffuse * factor
		+ 	totalLight.specular * factor
		;
}

