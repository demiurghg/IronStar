
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

float3 ClosestToPoint( float3 L, float3 r, float radius)
{
	// r must be normalized
	float3	centerToRay		=	dot(L,r)*r-L;
	float3	closestPoint	=	L + centerToRay * saturate( radius / length(centerToRay) );
	return closestPoint;
}


float4 ComputeDiffuseTubeMRP( float3 L0, float3 L1, float3 n, float r )
{
	float 	a 	=	length( L0 );
	float 	b 	=	length( L1 );
	float 	t 	=	a / (b+a);
	float3 	mrp	=	L0 + t*(L1-L0);
	
	float	factor	=	1 / (1 + r / length(mrp));
	
	return float4( ClosestToPoint( mrp, n, r ), factor );
}


float4 ComputeSpecularTubeMRP( float3 L0, float3 L1, float3 normal, float3 view, float radius, float3 mrp, float roughness )
{
	float3	R		=	normalize( reflect(-normalize(view), normal) );
	float3 	Ld		=	L1 - L0;
	
	/*float t_num 	=	dot(L0, Ld) * dot(R, L0) - dot(L0, L0) * dot(R, Ld);
	float t_denom 	=	dot(L0, Ld) * dot(R, Ld) - dot(Ld, Ld) * dot(R, L0);*/
	float 	t_num 	=	dot(R, L0) * dot(Ld, R) - dot(Ld, L0);
	float 	t_denom =	dot(Ld, Ld) - dot(Ld, R) * dot(Ld, R);//*/
	float 	t 		=	saturate(t_num / t_denom);	
	float3	L		=	L0 + t * Ld;
	
	float	a		=	roughness * roughness;
	float 	a1		=	saturate( a + 0.3333f * radius / length(L) );
	float  	factor	=	sqr( a / a1 );

	return float4( ClosestToPoint( L, R, radius ), factor );
}


float3 ComputePointLight( LIGHT light, CAMERA camera, GEOMETRY geometry, SURFACE surface, SHADOW_RESOURCES rc, bool diffuse, bool specular )
{
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
		
	float3 	lighting	=	0;
	float	roughness	=	ClampRoughness( surface.roughness );
	
	float4 	diffuseMPR	=	ComputeDiffuseTubeMRP( lightDir0, lightDir1, surface.normal, tubeRadius );
	float3  tint;
	float4 	specularMRP	=	ComputeSpecularTubeMRP( lightDir0, lightDir1, surface.normal, viewDir, tubeRadius, diffuseMPR, roughness );
	float3 	lightDir	=	diffuseMPR;
	
	float	falloff		=	LightFalloff( length(lightDir), lightRange );
	float	nDotL		=	saturate( dot( surface.normal, normalize( lightDir ) ) );
	float	nDotLSpec	=	saturate( dot( surface.normal, normalize( specularMRP.xyz ) ) );
	
	//	diffuse :
	lighting	+=	nDotL * falloff * Lambert( intensity * diffuseMPR.w, surface.diffuse );
	
	//	specular :
	lighting	+=	nDotLSpec * falloff * CookTorrance( surface.normal.xyz, viewDir, specularMRP.xyz, intensity * specularMRP.w, surface.specular, roughness );
	
	[branch]
	if (type==LightTypeOmni) 
	{
		//	TODO : IES profiles
	} 
	else if (type==LightTypeSpotShadow) 
	{
		lighting		*=	ComputeSpotShadow( geometry, light, rc, true );
	}
	
	return lighting;
}



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
		light_complexity++;
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
	//	Reflection :
	//----------------------------------------------------------------------------------------------
	
	float4 reflection = float4(0,0,0,0);
	
#ifndef DIFFUSE_ONLY	
	[loop]
	for (i=0; i<cluster.NumProbes; i++) 
	{
		LIGHTPROBE	probe		=	GetLightProbe( rcCluster, cluster, i );
		float4		envLight	=	ComputeEnvironmentLighting( probe, geometry, surface, Camera, rcLightProbe );
		reflection				=	lerp( reflection, float4(envLight.rgb,1), envLight.a );
		light_complexity++;
	}
	
	totalLight.rgb	=	reflection.rgb * ssaoFactor;
#endif	

	//totalLight.rgb += frac(reflection.a * 10)*10;

	//----------------------------------------------------------------------------------------------
	//	Lightmaps :
	//----------------------------------------------------------------------------------------------
	
	#ifdef IRRADIANCE_MAP
		totalLight	+= 	EvaluateLightmap( reflection.a, rcLightMap, geometry, surface, Camera, lmCoord ) * Stage.IndirectLightFactor;
		totalLight	*=	ssaoFactor;
	#endif
	#ifdef IRRADIANCE_VOLUME
		float3 volumeCoord	=	mad( float4(geometry.position.xyz, 1), Stage.WorldToVoxelScale, Stage.WorldToVoxelOffset ).xyz;
		totalLight			+=	EvaluateLightVolume( reflection.a, rcLightMap, geometry, surface, Camera, volumeCoord ) * Stage.IndirectLightFactor;
		totalLight			*=	ssaoFactor;
	#endif

	//----------------------------------------------------------------------------------------------
	//	Compute direct light :
	//----------------------------------------------------------------------------------------------

	if (1) 
	{ 
		float2	vpos	=	input.Position.xy;
		float3	shadow	=	ComputeCascadedShadows( geometry, vpos, CascadeShadow, rcShadow, true ); 
		
		FLUX flux		=	ComputeDirectLightFlux( DirectLight );
		totalLight		+=	ComputeLighting( flux, geometry, surface, Camera ) * shadow * Stage.DirectLightFactor;
	}
	
	//----------------------------------------------------------------------------------------------
	//	Compute point lights :
	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<cluster.NumLights; i++) 
	{
		LIGHT light	=	GetLight( rcCluster, cluster, i );
		totalLight	+=	ComputePointLight( light, Camera, geometry, surface, rcShadow, true, true );
		light_complexity++;
	}
	
	//----------------------------------------------------------------------------------------------
	
	totalLight.xyz	+=	surface.emission;
	
	totalLight.xyz 	+=	Stage.ShowLightComplexity * light_complexity * float3(1,0,0.5f) * 0.25f;

	//----------------------------------------------------------------------------------------------

	return totalLight;
}

