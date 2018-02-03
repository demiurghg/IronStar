
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"
#include "particles.shadows.hlsl"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos, float3 normal )
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

	float3 totalLight	=	0;

	//----------------------------------------------------------------------------------------------

	float3	shadow		=	ComputeCSM( worldPos, Params, ShadowSampler, ShadowMap, Sampler, ShadowMask ); 
	float3	lambert		=	max( 0, dot( -Params.DirectLightDirection.rgb, normal ) * 0.9 + 0.1 );
	totalLight.rgb 		+= 	shadow * Params.DirectLightIntensity.rgb * lambert;

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
		float	nDotL		=	max(0, dot( normalize(lightDir), normal ) * 0.9 + 0.1 );
		
		[branch]
		if (type==LightTypeOmni) {
			
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			
			totalLight.rgb 		+= 	falloff * intensity * nDotL;
			
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
				
				
				totalLight.rgb 		+= 	falloff * intensity * nDotL;
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
	
	
	totalLight += skyLight;

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
	
		totalLight += ambientTerm;
	}
	
	return totalLight * 1;
}

