
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"


static const float  M_PI			=	3.141592f;
static const float3	BetaRayleigh	=	float3( 3.8e-6f, 13.5e-6f, 33.1e-6f );
static const float3	BetaMie			=	float3( 21e-6f, 21e-6f, 21e-6f );



float3 Rayleigh( float mu )
{
    return 3.f / (16.f * M_PI) * (1 + mu * mu); 
}


float3 Mie( float mu, float g )
{
    return 3.f / (8.f * M_PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(1.f + g * g - 2.f * g * mu, 1.5f)); 
}


float4 ComputeDirectLight2( GEOMETRY geometry, DIRECT_LIGHT directLight, CAMERA camera, CASCADE_SHADOW cascadeShadow, SHADOW_RESOURCES rc, float2 vpos, float density )
{
	float3	viewDir		=	camera.CameraPosition.xyz - geometry.position;
	float3 	lightDir	=	normalize(-directLight.DirectLightDirection.xyz);
	float3 	intensity	=	directLight.DirectLightIntensity.xyz;
	
	float3	shadow		=	ComputeCascadedShadows( geometry, vpos, cascadeShadow, rc ); 

	float	mu			=	dot( normalize(viewDir), -lightDir );
	float	phaseM		=	Mie( mu, 0.76f );
	float3 	emission	=	intensity * shadow * density * phaseM * BetaMie;
	float	extinction	=	density * BetaMie.r * 1.1f;
	
	return float4(emission, extinction);
}


//
//	ComputeClusteredLighting
//	
float4 ComputeClusteredLighting ( float3 worldPos, float density )
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
	geometry.normal			=	float3(0,0,0);
	
	uint i;
	float4 totalLight = 0;
	
	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	float3	volumeCoord	=	mad( float4(worldPos, 1), Fog.WorldToVoxelScale, Fog.WorldToVoxelOffset ).xyz;
	float3 	lightmap	=	EvaluateLightVolume( rcLightMap, volumeCoord );
	
	totalLight.rgb	+=	lightmap * BetaMie * 3.14 * 4;

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------
	
	if (1)
	{
		totalLight.rgba += ComputeDirectLight2( geometry, DirectLight, Camera, CascadeShadow, rcShadow, float2(1,1), density );
	}

	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	/*[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light		=	GetLight( rcCluster, cluster, i );
		LIGHTING	lighting	=	ComputePointLight( light, Camera, geometry, surface, rcShadow );
		AccumulateLighting( totalLight, lighting, Fog.DirectLightFactor );
	}//*/
	
	//----------------------------------------------------------------------------------------------
	
	return 	totalLight;
}

