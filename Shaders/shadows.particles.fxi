

float ProjectShadow ( float3 worldPos, float4x4 viewProjection, out float4 projection )
{	
	float4 temp = 	mul( float4(worldPos,1), viewProjection );
	temp.xy 	/= 	temp.w;
	temp.w   	= 	1;
	
	projection	=	temp;
	
	return	max(abs(projection.x), abs(projection.y));//length(temp.xy);
}


float3	ComputeCSM ( 
	float3 worldPos, 
	PARAMS params, 
	SamplerComparisonState shadowSampler, 
	Texture2D csmTexture,
	SamplerState linearSampler, 
	Texture2D prtTexture
) {
	float4	projection		= float4(0,0,0,0);
	float4	bestProjection 	= float4(0,0,0,0);
	float4	bestScaleOffset = float4(0,0,0,0);
	float3	bestGradient	= float3(0,0,0);
	float4 	colorize   		= float4(1,0,2,1);
	
	float	bias			= 0.95;
	float	fade			= 1;

	//------------------------------------------------------
	//	select cascade :
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection3, projection ) < 1 ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset3;
		colorize		=	float4(0,0,1,1);
		fade			=	min(1, max(abs(projection.x), abs(projection.y)));
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection2, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset2;
		colorize		=	float4(0,1,0,1);
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection1, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset1;
		colorize		=	float4(1,0,0,1);
	}
	
	if ( ProjectShadow( worldPos, params.CascadeViewProjection0, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	params.CascadeScaleOffset0;
		colorize		=	float4(1,1,1,1);
	}
	
	//------------------------------------------------------
	
	float2	uv				=	mad( bestProjection.xy, bestScaleOffset.xy, bestScaleOffset.zw );
	float   depthcmp		= 	projection.z;
	float3	shadow			=	0;
	
	shadow = csmTexture.SampleCmpLevelZero( shadowSampler, uv, depthcmp );
	
	//------------------------------------------------------

	return lerp(shadow, 1, saturate(fade*16-15));
}

