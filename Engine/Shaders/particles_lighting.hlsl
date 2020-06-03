
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos, float3 normal, float3 color, float scatter, float roughness, float metallic )
{
	SHADOW_RESOURCES		rcShadow;
	rcShadow.ShadowSampler	=	ShadowSampler;
	rcShadow.ShadowMap		=	ShadowMap;
	rcShadow.LinearSampler	=	LinearSampler;
	rcShadow.ShadowMask		=	ShadowMask;

	CLUSTER_RESOURCES	rcCluster;
	rcCluster.ClusterArray		=	ClusterArray;
	rcCluster.IndexBuffer		=	ClusterIndexBuffer;
	rcCluster.LightBuffer		=	ClusterLightBuffer;
	
	LIGHTMAP_RESOURCES rcLightMap;
	rcLightMap.Sampler				=	LinearSampler;
	rcLightMap.IrradianceVolumeL0	=	IrradianceVolumeL0;
	rcLightMap.IrradianceVolumeL1	=	IrradianceVolumeL1;
	rcLightMap.IrradianceVolumeL2	=	IrradianceVolumeL2;
	rcLightMap.IrradianceVolumeL3	=	IrradianceVolumeL3;

	float4 projPos			=	mul( float4(worldPos,1), Camera.ViewProjection );
	
	CLUSTER cluster			=	GetCluster( rcCluster, projPos );

	GEOMETRY geometry 		= 	(GEOMETRY)0;
	geometry.position		=	worldPos;
	geometry.normal			=	normal;
	
	SURFACE	surface 		= 	(SURFACE)0;
	surface.normal			=	normal;
	surface.baseColor		=	color;
	surface.metallic		=	metallic;
	surface.roughness		=	roughness;
	surface.occlusion		=	1;
	surface.emission		=	float3(0,0,0);
	
	ComputeDiffuseSpecular( surface );
	
	uint i;
	LIGHTING totalLight		=	(LIGHTING)0;
	
	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	float3	volumeCoord	=	mad( float4(worldPos, 1), Params.WorldToVoxelScale, Params.WorldToVoxelOffset ).xyz;
	LIGHTING lightmap	=	EvaluateLightVolume( 0.5, rcLightMap, geometry, surface, Camera, volumeCoord );
	
	AccumulateLighting( totalLight, lightmap, Params.IndirectLightFactor );

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------
	
	if (1)
	{
		LIGHTING lighting = ComputeDirectLight( DirectLight, Camera, geometry, surface, CascadeShadow, rcShadow, float2(1,1) );
		AccumulateLighting( totalLight, lighting, Params.DirectLightFactor );
	}

	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light		=	GetLight( rcCluster, cluster, i );
		LIGHTING	lighting	=	ComputePointLight( light, Camera, geometry, surface, rcShadow );
		AccumulateLighting( totalLight, lighting, Params.DirectLightFactor );
	}
	
	//----------------------------------------------------------------------------------------------
	
	return 	totalLight.diffuse
		+ 	totalLight.transmissive
		+	totalLight.specular
		;//*/
	
	
	//----------------------------------------------------------------------------------------------

	// float3	shadow		=	ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, rcShadow ); 
	// FLUX	flux		=	ComputeDirectLightFlux( DirectLight );
	// totalLight			+=	shadow * ComputeParticleLighting( flux, geometry, surface, medium, Camera );

	//----------------------------------------------------------------------------------------------

	// [loop]
	// for (i=0; i<cluster.NumLights; i++) 
	// {
		// LIGHT light	=	GetLight( rcCluster, cluster, i );
		
		// FLUX flux	=	ComputePointLightFlux( geometry, light, rcShadow );
		// totalLight	+=	ComputeParticleLighting( flux, geometry, surface, medium, Camera );
	// }
	
	//----------------------------------------------------------------------------------------------
	
	/*float3	samplePoint		=	mad( float4(worldPos, 1), Params.WorldToVoxelScale, Params.WorldToVoxelOffset ).xyz;
	
	float4	irradianceL0	=	IrradianceVolumeL0.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL1	=	IrradianceVolumeL1.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL2	=	IrradianceVolumeL2.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL3	=	IrradianceVolumeL3.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	
	return	float3( irradianceL0.rgb ) * color.rgb;*/
}

