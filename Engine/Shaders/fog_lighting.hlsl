
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"

float OffCenter( float NoL, float g )
{
	return abs(sign(NoL) + g);
}

float PhaseFunc( float NoL, float g, float p, float b )
{
	return b + (1-b) * ( pow(abs(NoL),p) * OffCenter(NoL,g) ) ;
}

LIGHTING ComputeDirectLight2( DIRECT_LIGHT directLight, CAMERA camera, GEOMETRY geometry, SURFACE surface, float scatter, float density, CASCADE_SHADOW cascadeShadow, SHADOW_RESOURCES rc, float2 vpos )
{
	LIGHTING lighting 	= 	(LIGHTING)0;
	
	float3	shadow		=	ComputeCascadedShadows( geometry, vpos, cascadeShadow, rc ); 
	
	float3	viewDir		=	camera.CameraPosition.xyz - geometry.position;
	float3 	lightDir	=	normalize(-directLight.DirectLightDirection.xyz);
	float3 	intensity	=	directLight.DirectLightIntensity.xyz;
	float	angularSize	=	directLight.DirectLightAngularSize;
	
	float	roughness	=	ClampRoughness( surface.roughness );
	
	float4 	specularMRP	=	ComputeSpecularTubeMRP( lightDir, lightDir, surface.normal, viewDir, angularSize, roughness );

	float	nDotL		=	saturate( dot( surface.normal, lightDir ) );

	lighting.diffuse	=	shadow * nDotL * Lambert( intensity, surface.diffuse );
	lighting.specular	=	shadow * nDotL * CookTorrance( surface.normal.xyz, viewDir, specularMRP.xyz, intensity * specularMRP.w, surface.specular, roughness );

	float3 phase;
	phase.r	=	PhaseFunc( nDotL, 1.00f, 64, 0.1f );
	phase.g	=	PhaseFunc( nDotL, 0.55f, 16, 0.2f );
	phase.b	=	PhaseFunc( nDotL, 0.25f,  2, 0.5f );
	
	lighting.transmissive	=	surface.baseColor * density * intensity * shadow * PhaseFunc( nDotL, 0.75f, 16, 0.1f );
	
	return lighting;
}


//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos, float3 normal, float3 color, float scatter, float density )
{
	SHADOW_RESOURCES		rcShadow;
	rcShadow.ShadowSampler	=	ShadowSampler;
	rcShadow.ShadowMap		=	ShadowMap;
	rcShadow.LinearSampler	=	LinearClamp;
	rcShadow.ShadowMask		=	ShadowMask;

	CLUSTER_RESOURCES	rcCluster;
	rcCluster.ClusterArray		=	ClusterArray;
	rcCluster.IndexBuffer		=	ClusterIndexBuffer;
	rcCluster.LightBuffer		=	ClusterLightBuffer;
	
	LIGHTMAP_RESOURCES rcLightMap;
	rcLightMap.Sampler				=	LinearClamp;
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
	surface.metallic		=	0;
	surface.roughness		=	1;
	surface.occlusion		=	1;
	surface.emission		=	float3(0,0,0);
	
	ComputeDiffuseSpecular( surface );
	
	uint i;
	LIGHTING totalLight		=	(LIGHTING)0;
	
	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	float3	volumeCoord	=	mad( float4(worldPos, 1), Fog.WorldToVoxelScale, Fog.WorldToVoxelOffset ).xyz;
	LIGHTING lightmap	=	EvaluateLightVolume( 0, rcLightMap, geometry, surface, Camera, volumeCoord );
	lightmap.transmissive *= 3.15 * 4;
	
	AccumulateLighting( totalLight, lightmap, Fog.IndirectLightFactor );

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------
	
	if (1)
	{
		LIGHTING lighting = ComputeDirectLight2( DirectLight, Camera, geometry, surface, scatter, density, CascadeShadow, rcShadow, float2(1,1) );
		AccumulateLighting( totalLight, lighting, Fog.DirectLightFactor );
	}

	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light		=	GetLight( rcCluster, cluster, i );
		LIGHTING	lighting	=	ComputePointLight( light, Camera, geometry, surface, rcShadow );
		AccumulateLighting( totalLight, lighting, Fog.DirectLightFactor );
	}
	
	//----------------------------------------------------------------------------------------------
	
	return 	totalLight.transmissive;//*/
}

