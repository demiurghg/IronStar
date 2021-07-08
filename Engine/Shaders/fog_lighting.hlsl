
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#define NO_DECALS
#define NO_CUBEMAPS
#include "ls_core.fxi"
#include "math.fxi"


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


float3 ComputeDirectLight2( GEOMETRY geometry, DIRECT_LIGHT directLight, CAMERA camera, CASCADE_SHADOW cascadeShadow, SHADOW_RESOURCES rc, float2 vpos )
{
	float3	viewDir		=	camera.CameraPosition.xyz - geometry.position;
	float3 	lightDir	=	normalize(-directLight.DirectLightDirection.xyz);
	float3 	intensity	=	directLight.DirectLightIntensity.xyz;
	
	float3	shadow		=	ComputeCascadedShadows( geometry, vpos, cascadeShadow, rc ); 

	float	mu			=	dot( normalize(viewDir), -lightDir );
			mu			=	isnan(mu) ? 0 : mu; // temporal fix for fist frame
	float3	phaseM		=	Mie( mu, 0.76f );
	
	return	intensity * shadow * phaseM;
}


float3 ComputePointLight2( LIGHT light, CAMERA camera, GEOMETRY geometry, SHADOW_RESOURCES rc )
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
	float	fadeout		=	saturate( 1 - dot(viewDir,viewDir) * Fog.FadeoutDistanceInvSqr);
	
	float3 	shadow		=	float3(1,1,1);

	float	mu			=	dot( normalize(viewDir), -normalize(lightDir) );
	float3	phaseM		=	Mie( mu, 0.76f );

	[branch]
	if (type==LightTypeOmni) 
	{
		//	TODO : IES profiles
	} 
	else if (type==LightTypeSpotShadow) 
	{
		shadow	=	ComputeSpotShadow( geometry, light, rc );
	}

	lighting	=	shadow * falloff * fadeout * phaseM * intensity;
	
	return lighting;
}



float2 ComputeSkyShadow( float3 worldPos )
{
	SHADOW_RESOURCES		rcShadow;
	rcShadow.ShadowSampler	=	ShadowSampler;
	rcShadow.ShadowMap		=	ShadowMap;
	rcShadow.LinearSampler	=	LinearClamp;
	rcShadow.ShadowMask		=	ShadowMask;

	LIGHTMAP_RESOURCES rcLightMap;
	rcLightMap.Sampler				=	LinearClamp;
	rcLightMap.IrradianceVolumeL0	=	IrradianceVolumeL0;
	rcLightMap.IrradianceVolumeL1	=	IrradianceVolumeL1;
	rcLightMap.IrradianceVolumeL2	=	IrradianceVolumeL2;
	rcLightMap.IrradianceVolumeL3	=	IrradianceVolumeL3;

	GEOMETRY geometry 	= 	(GEOMETRY)0;
	geometry.position	=	worldPos;
	geometry.normal		=	float3(0,0,0);

	float	shadow		=	ComputeCascadedShadows( geometry, float2(1,1), CascadeShadow, rcShadow ).r; 

	float3	volumeCoord	=	mul( float4(worldPos, 1), Fog.WorldToVolume ).xyz;
	float 	skyFactor	=	IrradianceVolumeL1.SampleLevel( LinearClamp, volumeCoord, 0 ).a;
	
	skyFactor =	 saturate(skyFactor);
	
	//return float2( shadow, skyFactor );
	return float2( shadow, skyFactor );
}


//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos )
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
	float3 totalLight = 0;
	
	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	float3	volumeCoord	=	mul( float4(worldPos, 1), Fog.WorldToVolume ).xyz;
	float3 	lightmap	=	EvaluateLightVolume( rcLightMap, volumeCoord );
	
	totalLight.rgb		+=	lightmap;// * 6.28;

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------
	
	totalLight.rgb += ComputeDirectLight2( geometry, DirectLight, Camera, CascadeShadow, rcShadow, float2(1,1) );

	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT 		light	=	GetLight( rcCluster, cluster, i );
		totalLight.rgb		+=	ComputePointLight2( light, Camera, geometry, rcShadow );
	}//*/
	
	//----------------------------------------------------------------------------------------------
	
	return 	totalLight;
}

