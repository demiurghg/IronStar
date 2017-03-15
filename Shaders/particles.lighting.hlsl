
/*-----------------------------------------------------------------------------
	Clustered lighting rendering :
-----------------------------------------------------------------------------*/

#include "brdf.fxi"

//
//	ComputeClusteredLighting
//	
float3 ComputeClusteredLighting ( float3 worldPos )
{
	uint i,j,k;
	float4 projPos		=	mul( float4(worldPos,1), Params.View ); // HACK: for this pass View = View * Projection
	float3 result		=	float3(0,0,0);
	float slice			= 	1 - exp(-projPos.w*0.03);
	float2 vpos			=	(projPos.xy/projPos.w)*0.5+0.5;
	int3 loadUVW		=	int3( vpos*float2(16,8), slice * 24 );
	
	uint2	data		=	ClusterTable.Load( int4(loadUVW,0) ).rg;
	uint	index		=	data.r;
	uint 	lightCount	=	(data.g & 0x000FFF) >> 0;

	float3 totalLight	=	0;

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
				
				float	penumbra	=	1;
				if (shape==LightSpotShapeRound) {
					penumbra 	=	max(0, 1 - length(lsPos.xy));
					penumbra	=	min(1, penumbra * 2 );				
					penumbra	*=	penumbra;
				}
				if (shape==LightSpotShapeSquare) {
					penumbra 	= 	max(0, 1 - length(max(abs(lsPos.x), abs(lsPos.y))));
					penumbra	=	min(1, penumbra * 2 );				
					penumbra	*=	penumbra;
				}
				
				lsPos.xy		=	mad( lsPos.xy, scaleOffset.xy, scaleOffset.zw );
						
				//	TODO : num taps <--> shadow quality
				//	TODO : kernel size <--> shadow region size
				#if 1
				float shadow	=	ShadowMap.SampleCmpLevelZero( ShadowSampler, lsPos.xy, shadowDepth ).r;
				#else
				float shadow	=	1;
				#endif
						
				float3 	lightDir	= 	position - worldPos.xyz;
				float3 	falloff		= 	LinearFalloff( length(lightDir), radius ) * shadow * penumbra;
				
				totalLight.rgb 		+= 	falloff * intensity;
			}
		}
	}
	
	return totalLight;
}

