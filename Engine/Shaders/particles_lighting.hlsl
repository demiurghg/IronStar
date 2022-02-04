
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"


static const float  M_PI			=	3.141592f;

float3 Rayleigh( float mu )
{
    return 3.f / (16.f * M_PI) * (1 + mu * mu); 
}

float3 Mie( float mu, float g )
{
    return 3.f / (8.f * M_PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(abs(1.f + g * g - 2.f * g * mu), 1.5f)); 
}

bool IsNan(float a)
{
	return (a < 0.0 || a > 0.0 || a == 0.0) ? true : false;
}


float3 ComputeDirectLight2( GEOMETRY geometry, DIRECT_LIGHT directLight, CAMERA camera, CASCADE_SHADOW cascadeShadow, SHADOW_RESOURCES rc, float scatter )
{
	float3	viewDir		=	camera.CameraPosition.xyz - geometry.position;
	float3 	lightDir	=	normalize(-directLight.DirectLightDirection.xyz);
	float3 	intensity	=	directLight.DirectLightIntensity.xyz;
	
	float3	shadow		=	ComputeCascadedShadows( geometry, float2(1,1), cascadeShadow, rc ); 

	float3	lambert		=	Lambert(intensity, float3(1,1,1));
	float	nDotL		=	saturate( dot( lightDir, normalize(geometry.normal + lightDir * scatter) ) );
	
	return	shadow * nDotL * lambert;
}

float3 ComputePointLight2( LIGHT light, CAMERA camera, GEOMETRY geometry, SHADOW_RESOURCES rc, float scatter )
{
	float3 lighting = float3(0,0,0);
	
	uint type 	= 	light.LightType & 0x0000FFFF;
	uint shape	= 	light.LightType & 0xFFFF0000;	
	
	float3	viewDir		=	camera.CameraPosition.xyz - geometry.position;
	float3 	position0	=	light.Position0LightRange.xyz;
	float3 	position1	=	light.Position1TubeRadius.xyz;
	float  	lightRange	=	light.Position0LightRange.w;
	float  	tubeRadius	=	light.Position1TubeRadius.w;
	float3 	intensity	=	light.IntensityFar.rgb;
	
	float3 	lightDir0	= 	position0 - geometry.position;
	float3 	lightDir1	= 	position1 - geometry.position;
		
	float4 	diffuseMPR	=	ComputeDiffuseTubeMRP( lightDir0, lightDir1, viewDir, tubeRadius );
	float3 	lightDir	=	lightDir0;// diffuseMPR.xyz;
	
	float	falloff		=	LightFalloff( length(lightDir), lightRange );
	
	float3 	shadow		=	float3(1,1,1);

	float3	lambert		=	Lambert(intensity, float3(1,1,1));
	float	nDotL		=	saturate( dot( lightDir, normalize(geometry.normal + lightDir * scatter) ) );

	[branch]
	if (type==LightTypeOmni) 
	{
		//	TODO : IES profiles
	} 
	else if (type==LightTypeSpotShadow) 
	{
		shadow	=	ComputeSpotShadow( geometry, light, rc );
	}

	return	shadow * nDotL * lambert * falloff;
	
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
	
	SURFACE	surface 		= 	(SURFACE)0;
	surface.normal			=	normal;
	surface.baseColor		=	color;
	surface.metallic		=	metallic;
	surface.roughness		=	roughness;
	surface.occlusion		=	1;
	surface.emission		=	float3(0,0,0);
	
	ComputeDiffuseSpecular( surface );
	
	uint i;

#if defined (HARD)
	
	LIGHTING totalLight		=	(LIGHTING)0;
	
	//	Lightmaps :
	float3	volumeCoord	=	mul( float4(worldPos, 1), Params.WorldToLightVolume ).xyz;
	LIGHTING lightmap	=	EvaluateLightVolume( rcLightMap, geometry, surface, Camera, volumeCoord );
	
	AccumulateLighting( totalLight, lightmap, Params.IndirectLightFactor );

	//	Compute direct light :
	LIGHTING lighting = ComputeDirectLight( DirectLight, Camera, geometry, surface, CascadeShadow, rcShadow, float2(1,1) );
	AccumulateLighting( totalLight, lighting, Params.DirectLightFactor );

	//	Compute point lights :
	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light		=	GetLight( rcCluster, cluster, i );
		LIGHTING	lighting	=	ComputePointLight( light, Camera, geometry, surface, rcShadow );
		AccumulateLighting( totalLight, lighting, Params.DirectLightFactor );
	}
	
	return 	totalLight.diffuse
		+ 	totalLight.transmissive
		+	totalLight.specular
		;//*/

#else
	float3 totalLight	=	0;
	
	//	Lightmaps :
	float3	volumeCoord	=	mul( float4(worldPos, 1), Fog.WorldToVolume ).xyz;
	float3 	lightmap	=	EvaluateLightVolume( rcLightMap, volumeCoord );
	totalLight.rgb		+=	lightmap;

	//	Compute direct light :
	totalLight.rgb += ComputeDirectLight2( geometry, DirectLight, Camera, CascadeShadow, rcShadow, scatter );

	//	Compute point lights :
	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light	=	GetLight( rcCluster, cluster, i );
		totalLight.rgb		+=	ComputePointLight2( light, Camera, geometry, rcShadow, scatter );
	}//*/
	
	return 	totalLight;
	
#endif

}

