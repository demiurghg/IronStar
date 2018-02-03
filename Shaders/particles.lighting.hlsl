
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"
#include "particles.shadows.hlsl"

#ifdef SOFT_LIGHTING
float3 ComputeLight ( float3 intensity, float3 dir, float3 vdir, float3 normal, float3 color, float scatter, float roughness, float metallic )
{
	float a = 1.0 - 0.5*scatter;
	float b = 1 - a;
	return intensity * max( 0, dot( dir, normal ) * a + b );
}
#endif

#ifdef HARD_LIGHTING
float3 ComputeLight ( float3 intensity, float3 dir, float3 vdir, float3 normal, float3 color, float scatter, float roughness, float metallic )
{
	float 	a 			= 	1.0 - 0.5*scatter;
	float 	b 			= 	1 - a;
	float3	diffuse 	=	lerp( color, float3(0,0,0), metallic );
	float3	specular  	=	lerp( float3(0.04f,0.04f,0.04f), color, metallic );
			roughness	=	sqrt(roughness);
			
	float	nDotL		=	max(0, dot(dir, normal));
	float	nDotLSoft	=	max(0, dot(dir, normal) * a + b);
			
	return 	intensity * nDotLSoft * diffuse
		+	nDotL * CookTorrance( normal, vdir, dir, intensity, specular, roughness, 0.05 )
		;
}
#endif

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos, float3 normal, float3 color, float scatter, float roughness, float metallic )
{
//	return normal;

	uint i,j,k;
	float4 projPos		=	mul( float4(worldPos,1), Params.ViewProjection );
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-projPos.w*0.03);
	float2 vpos			=	(projPos.xy/projPos.w)*float2(0.5,-0.5)+0.5;
	int3 loadUVW		=	int3( vpos*float2(16,8), slice * 24 );
	
	uint2	data		=	ClusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	lightCount	=	(data.g & 0x00000FFF) >> 0;
	uint 	decalCount	=	(data.g & 0x00FFF000) >> 12;
	uint 	lpbCount	=	(data.g & 0xFF000000) >> 24;
	
	float3	vdir		=	normalize( Params.CameraPosition.xyz - worldPos.xyz );

	float3 totalLight	=	0;
	
	//----------------------------------------------------------------------------------------------
			normal		=	normalize(normal);
	float3	shadow		=	ComputeCSM( worldPos, Params, ShadowSampler, ShadowMap, Sampler, ShadowMask ); 
	float3	dirLightDir	=	normalize( -Params.DirectLightDirection.xyz );
	float3	dirLightInt = 	Params.DirectLightIntensity.rgb;
	totalLight.rgb 		+= 	shadow * ComputeLight( dirLightInt, dirLightDir, vdir, normal, color, scatter, roughness, metallic );

	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<lightCount; i++) {
		uint idx  	= LightIndexTable.Load( index + i );
		uint type 	= LightDataTable[idx].LightType & 0x0000FFFF;
		uint shape	= LightDataTable[idx].LightType & 0xFFFF0000;	
		
		float3 	position	=	LightDataTable[idx].PositionRadius.xyz;
		float  	radius		=	LightDataTable[idx].PositionRadius.w;
		float3 	intensity	=	LightDataTable[idx].IntensityFar.rgb;
		float3 	lightDir	= 	position - worldPos.xyz;
		
		float3	lighting	=	ComputeLight( intensity, normalize(lightDir), vdir, normal, color, scatter, roughness, metallic );
		
		[branch]
		if (type==LightTypeOmni) {
			
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			totalLight.rgb 		+= 	falloff * lighting;
			
		} else if (type==LightTypeSpotShadow) {
			
			float4 lsPos		=	mul(float4(worldPos,1), LightDataTable[idx].ViewProjection);
			float  shadowDepth	=	lsPos.z / LightDataTable[idx].IntensityFar.w;
				   lsPos.xyz	= 	lsPos.xyz / lsPos.w;
				   
			if ( abs(lsPos.x)<1 && abs(lsPos.y)<1 && abs(lsPos.z)<1 ) {
				float3 	position	=	LightDataTable[idx].PositionRadius.xyz;
				float  	radius		=	LightDataTable[idx].PositionRadius.w;
				float3 	intensity	=	LightDataTable[idx].IntensityFar.rgb;
				float4 	scaleOffset	=	LightDataTable[idx].ShadowScaleOffset;
				
				lsPos.xy		=	mad( lsPos.xy, scaleOffset.xy, scaleOffset.zw );
						
				float3 	accumulatedShadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, lsPos.xy, shadowDepth ).rrr;
						accumulatedShadow	*=	ShadowMask.SampleLevel( Sampler, lsPos.xy, 0 ).rgb;
						
				float3 	falloff		= 	LinearFalloff( length(lightDir), radius ) * accumulatedShadow;
				totalLight.rgb 		+= 	falloff * lighting;
			}
		}
	}
	
	//----------------------------------------------------------------------------------------------
	
	float4	aogridValue		=	OcclusionGrid.SampleLevel( Sampler, mul( float4(worldPos, 1), Params.OcclusionGridMatrix ).xyz, 0 ).rgba;
			aogridValue.xyz	=	aogridValue.xyz * 2 - 1;
			
	float	aoFactor		=	aogridValue.w;
	float 	skyFactor		=	length( aogridValue.xyz );
	
	//aoFactor = skyFactor = 1;
	
	float3 	skyBentNormal	=	aogridValue.xyz / (skyFactor + 0.1) * aoFactor;
	float3	skyLight		=	skyFactor * Params.SkyAmbientLevel.rgb;
	
	
	totalLight += skyLight * color;

	[loop]
	for (i=0; i<lpbCount; i++) {
		uint idx  			= 	LightIndexTable.Load( lightCount + decalCount + index + i );
		float3 position		=	ProbeDataTable[idx].Position.xyz;
		float  innerRadius	=	ProbeDataTable[idx].InnerRadius;
		float  outerRadius	=	ProbeDataTable[idx].OuterRadius;
		uint   imageIndex	=	ProbeDataTable[idx].ImageIndex;
		
		float	localDist	=	distance( position.xyz, worldPos.xyz );
		float	factor		=	saturate( 1 - (localDist-innerRadius)/(outerRadius-innerRadius) );

		float3	ambientTerm	=	RadianceCache.SampleLevel( Sampler, float4(0,0,1, imageIndex), 6).rgb;
				ambientTerm *=	aoFactor;
	
		totalLight += ambientTerm * color;
	}
	
	return totalLight;
}

