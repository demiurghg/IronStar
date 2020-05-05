
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
	rcLightProbe.Sampler		=	SamplerLinearClamp;
	rcLightProbe.EnvironmentLut	=	EnvLut;
	rcLightProbe.RadianceCache	=	RadianceCache;
	
	LIGHTMAP_RESOURCES rcLightMap;
	rcLightMap.Sampler				=	SamplerLightmap;
	rcLightMap.IrradianceMapL0		=	IrradianceMapL0;
	rcLightMap.IrradianceMapL1		=	IrradianceMapL1;
	rcLightMap.IrradianceMapL2		=	IrradianceMapL2;
	rcLightMap.IrradianceMapL3		=	IrradianceMapL3;
	rcLightMap.IrradianceVolumeR	=	IrradianceVolumeR;
	rcLightMap.IrradianceVolumeG	=	IrradianceVolumeG;
	rcLightMap.IrradianceVolumeB	=	IrradianceVolumeB;
	
	DECAL_RESOURCES	rcDecals;
	rcDecals.DecalImages			=	DecalImages;
	rcDecals.DecalSampler			=	DecalSampler;
	
	CLUSTER cluster			=	GetCluster( rcCluster, input.ProjPos );

	uint i;
	float3 	totalLight		=	0;

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
		
		surface = ApplyDecal( geometry, surface, Camera, decal, rcDecals, Instance.Group );
	}
	
	ComputeDiffuseSpecular( surface );
#else
	ComputeDiffuseOnly( surface );
#endif

	//----------------------------------------------------------------------------------------------
	//	Ambient occlusion :
	//----------------------------------------------------------------------------------------------

#if defined (TRANSPARENT) || defined(DIFFUSE_ONLY)
	float 	ssaoFactor	=	1;
#else
	float 	ssaoFactor	=	SRGBToLinear( AmbientOcclusion.Load( int3( input.Position.xy, 0 ) ).r );
	ssaoFactor	=	lerp( 1, ssaoFactor, Stage.SsaoWeight );
	ssaoFactor	*=	surface.occlusion;
#endif	

	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	#ifdef IRRADIANCE_MAP
		totalLight	+= 	EvaluateLightmap( rcLightMap, geometry, surface, Camera, lmCoord );
		totalLight	*=	ssaoFactor;
	#endif
	#ifdef IRRADIANCE_VOLUME
		float3 volumeCoord		=	mul(float4(geometry.position.xyz,1), Stage.OcclusionGridMatrix ).xyz;
		totalLight				+=	EvaluateLightVolume( rcLightMap, surface, volumeCoord );
	#endif

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------

	if (1) 
	{ 
		float2	vpos	=	input.Position.xy;
		float3	shadow	=	ComputeCascadedShadows( geometry, vpos, CascadeShadow, rcShadow, true ); 
		
		FLUX flux		=	ComputeDirectLightFlux( DirectLight );
		totalLight		+=	ComputeLighting( flux, geometry, surface, Camera ) * shadow;
	}
	
	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT light	=	GetLight( rcCluster, cluster, i );
		
		FLUX flux	=	ComputePointLightFlux( geometry, light, rcShadow );
		totalLight	+=	ComputeLighting( flux, geometry, surface, Camera );
	}
	
	totalLight *= Stage.DirectLightFactor;
	
	float3	ambientDiffuse		=	float3(0,0,0);
	float3	ambientSpecular		=	float3(0,0,0);
	
	//----------------------------------------------------------------------------------------------
	//	Reflection :
	//----------------------------------------------------------------------------------------------
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<cluster.NumProbes; i++) 
	{
		LIGHTPROBE	probe		=	GetLightProbe( rcCluster, cluster, i );
		float4		envLight	=	ComputeEnvironmentLighting( probe, geometry, surface, Camera, rcLightProbe );
		
		ambientSpecular			=	lerp( ambientSpecular, envLight.rgb, envLight.a );
	}
#endif	

	//----------------------------------------------------------------------------------------------
	
	totalLight.xyz	+=	ambientDiffuse	* ssaoFactor;
	totalLight.xyz	+=	ambientSpecular * ssaoFactor;
	
	totalLight.xyz	+=	surface.emission;

	//----------------------------------------------------------------------------------------------

	return totalLight;
}

