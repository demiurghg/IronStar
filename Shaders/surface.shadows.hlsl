
static const float dither4[4][4] = {{1,9,3,11},{13,5,15,7},{4,12,2,10},{16,8,14,16}};
static const float dither2[2][2] = {{0,2},{3,1}};

float Dither ( int xpos, int ypos )
{
	//return dither4[xpos%4][ypos%4]/16.0f;
	return dither2[xpos&1][ypos&1]/4.0f;
}



float ProjectShadow ( float3 worldPos, float4x4 viewProjection, out float4 projection )
{	
	float4 temp = 	mul( float4(worldPos,1), viewProjection );
	temp.xy 	/= 	temp.w;
	temp.w   	= 	1;
	
	projection	=	temp;
	
	return	max(abs(projection.x), abs(projection.y));//length(temp.xy);
}


float3	ComputeCSM ( 
	float2 vpos,
	float3 normal,
	float3 lightDir,
	float3 worldPos, 
	STAGE stage, 
	SamplerComparisonState shadowSampler, 
	SamplerState linearSampler, 
	Texture2D csmTexture, 
	Texture2D particleShadow, 
	bool useFilter 
) {
	//	compute UVs 
	float2	csmSize;
	float2	csmSizeRcp;
	csmTexture.GetDimensions( csmSize.x, csmSize.y );
	csmSizeRcp.xy	=	1 / csmSize.xy;

	float4	projection		= float4(0,0,0,0);
	float4	bestProjection 	= float4(0,0,0,0);
	float4	bestScaleOffset = float4(0,0,0,0);
	float3	bestGradient	= float3(0,0,0);
	float4 	colorize   		= float4(1,0,2,1);
	float3 	scale			= 1;
	
	float	bias			= 0.925 + Dither( vpos.x, vpos.y ) * 0.05f;
	float	fade			= 1;

	//------------------------------------------------------
	
	//	select cascade :
	if ( ProjectShadow( worldPos, stage.CascadeViewProjection3, projection ) < 1 ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	stage.CascadeScaleOffset3;
		bestGradient	=	mul( float4(normal,0), stage.CascadeGradientMatrix3 ).xyz;
		colorize		=	float4(0,0,1,1);
		scale 			=	stage.CascadeViewProjection3._11_22_33;
		fade			=	min(1, max(abs(projection.x), abs(projection.y)));
	}
	
	if ( ProjectShadow( worldPos, stage.CascadeViewProjection2, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	stage.CascadeScaleOffset2;
		bestGradient	=	mul( float4(normal,0), stage.CascadeGradientMatrix2 ).xyz;
		colorize		=	float4(0,1,0,1);
		scale 			=	stage.CascadeViewProjection2._11_22_33;
	}
	
	if ( ProjectShadow( worldPos, stage.CascadeViewProjection1, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	stage.CascadeScaleOffset1;
		bestGradient	=	mul( float4(normal,0), stage.CascadeGradientMatrix1 ).xyz;
		colorize		=	float4(1,0,0,1);
		scale 			=	stage.CascadeViewProjection1._11_22_33;
	}
	
	if ( ProjectShadow( worldPos, stage.CascadeViewProjection0, projection ) < bias ) {
		bestProjection 	=	projection;
		bestScaleOffset	=	stage.CascadeScaleOffset0;
		bestGradient	=	mul( float4(normal,0), stage.CascadeGradientMatrix0 ).xyz;
		colorize		=	float4(1,1,1,1);
		scale 			=	stage.CascadeViewProjection0._11_22_33;
	}
	
	//------------------------------------------------------
	//	warning: no offset is required!
	float2	uv				=	mad( bestProjection.xy, bestScaleOffset.xy, bestScaleOffset.zw );
	float   depthcmp		= 	projection.z;
	float3	shadow			=	0;
	
	float	epsilon			=	1 / 8192.0f / 4.0f;
			bestGradient	=	normalize(bestGradient);
	float2	depthWeights	=	bestGradient.xy / (bestGradient.z + epsilon );
	
	
	if (true) {
		
		for( float row = -1.5; row <= 1.5; row += 1 ) {
			[unroll]for( float col = -1.5; col <= 1.5; col += 1 ) {
				float 	x	=	col * 1;
				float	y	=	row * 1;
				float	z	=	depthcmp + dot( depthWeights, float2(x,y) );
				float	sh	=	csmTexture.SampleCmpLevelZero( shadowSampler, uv + csmSizeRcp * float2(x,y), z ).r;
				shadow		+=	sh;
			}
		}
		
		shadow /= 16;

		shadow *= particleShadow.SampleLevel( linearSampler, uv, 0 ).rgb;
		
	} else {
		shadow = csmTexture.SampleCmpLevelZero( shadowSampler, uv, depthcmp );
	}
	
	return lerp(shadow, 1, saturate(fade*16-15));
}

