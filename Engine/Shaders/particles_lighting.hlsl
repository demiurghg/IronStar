
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"

// // // // #ifdef SOFT_LIGHTING
// // // // float3 ComputeLight ( float3 intensity, float3 dir, float3 vdir, float3 normal, float3 color, float scatter, float roughness, float metallic )
// // // // {
	// // // // float a = 1.0 - 0.5*scatter;
	// // // // float b = 1 - a;
	// // // // return intensity * max( 0, dot( dir, normal ) * a + b );
// // // // }
// // // // #endif

// // // // #ifdef HARD_LIGHTING
// // // // float3 ComputeLight ( float3 intensity, float3 dir, float3 vdir, float3 normal, float3 color, float scatter, float roughness, float metallic )
// // // // {
	// // // // float 	a 			= 	1.0 - 0.5*scatter;
	// // // // float 	b 			= 	1 - a;
	// // // // float3	diffuse 	=	lerp( color, float3(0,0,0), metallic );
	// // // // float3	specular  	=	lerp( float3(0.04f,0.04f,0.04f), color, metallic );
			// // // // roughness	=	sqrt(roughness);
			
	// // // // float	nDotL		=	max(0, dot(dir, normal));
	// // // // float	nDotLSoft	=	max(0, dot(dir, normal) * a + b);
			
	// // // // return 	intensity * nDotLSoft * diffuse
		// // // // +	nDotL * CookTorrance( normal, vdir, dir, intensity, specular, roughness, 0.05 )
		// // // // ;
// // // // }
// // // // #endif

float3 ComputeParticleLighting( FLUX flux, GEOMETRY geometry, SURFACE surface, MEDIUM medium, CAMERA camera )
{
	float3 lighting = 0;
#ifdef SOFT_LIGHTING
	lighting	=	ComputeLighting( flux, geometry, medium, camera );
#endif	
#ifdef HARD_LIGHTING
	lighting	=	ComputeLighting( flux, geometry, surface, camera );
#endif
	return lighting;
}


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
	
	MEDIUM medium;
	medium.Color			=	color;
	
	SURFACE	surface 		= 	(SURFACE)0;
	surface.normal			=	normal;
	surface.baseColor		=	color;
	surface.metallic		=	metallic;
	surface.roughness		=	roughness;
	surface.occlusion		=	1;
	surface.emission		=	float3(0,0,0);
	
	uint i;
	float3 totalLight	=	0;
	
	//----------------------------------------------------------------------------------------------

	float3	shadow		=	ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, rcShadow, false ); 
	FLUX	flux		=	ComputeDirectLightFlux( DirectLight );
	totalLight			+=	shadow * ComputeParticleLighting( flux, geometry, surface, medium, Camera );

	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT light	=	GetLight( rcCluster, cluster, i );
		
		FLUX flux	=	ComputePointLightFlux( geometry, light, rcShadow );
		totalLight	+=	ComputeParticleLighting( flux, geometry, surface, medium, Camera );
	}
	
	//----------------------------------------------------------------------------------------------
	
	float3	samplePoint		=	mad( float4(worldPos, 1), Params.WorldToVoxelScale, Params.WorldToVoxelOffset ).xyz;
	
	float4	irradianceL0	=	IrradianceVolumeL0.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL1	=	IrradianceVolumeL1.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL2	=	IrradianceVolumeL2.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	float4	irradianceL3	=	IrradianceVolumeL3.SampleLevel( LinearSampler, samplePoint, 0 ).rgba;
	
	totalLight.rgb			+=	float3( irradianceL0.rgb ) * color.rgb;
	
	return totalLight;
}

