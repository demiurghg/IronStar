
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"
#include "particles.shadows.hlsl"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos )
{
	uint i,j,k;
	float4 projPos		=	mul( float4(worldPos,1), Params.ViewProjection );
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-projPos.w*0.03);
	float2 vpos			=	(projPos.xy/projPos.w)*float2(0.5,-0.5)+0.5;
	int3 loadUVW		=	int3( vpos*float2(16,8), slice * 24 );
	
	uint2	data		=	ClusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	lightCount	=	(data.g & 0x000FFF) >> 0;

	float3 totalLight	=	Params.AmbientLevel.rgb;

	//----------------------------------------------------------------------------------------------

	float3	shadow		=	ComputeCSM( worldPos, Params, ShadowSampler, ShadowMap, Sampler, ShadowMask ); 
	totalLight.rgb 		+= 	shadow * Params.DirectLightIntensity.rgb;

	//----------------------------------------------------------------------------------------------

	[loop]
	for (i=0; i<lightCount; i++) {
		uint idx  	= LightIndexTable.Load( index + i );
		uint type 	= LightDataTable[idx].LightType & 0x0000FFFF;
		uint shape	= LightDataTable[idx].LightType & 0xFFFF0000;	
		
		float3 position		=	LightDataTable[idx].PositionRadius.xyz;
		float  radius		=	LightDataTable[idx].PositionRadius.w;
		float3 intensity	=	LightDataTable[idx].IntensityFar.rgb;
		
		[branch]
		if (type==LightTypeOmni) {
			
			float3 lightDir		= 	position - worldPos.xyz;
			float  falloff		= 	LinearFalloff( length(lightDir), radius );
			
			totalLight.rgb 		+= 	falloff * intensity;
			
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
						
				float3 	lightDir	= 	position - worldPos.xyz;
				float3 	falloff		= 	LinearFalloff( length(lightDir), radius ) * accumulatedShadow;
				
				totalLight.rgb 		+= 	falloff * intensity;
			}
		}
	}
	
	return totalLight * 1;
}

