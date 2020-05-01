#if 0
$ubershader	MASK_ALPHA
#endif

#include "auto/dilate.fxi"

//-------------------------------------------------------------------------------

#define EPSILON (0.5f/256.0f)

float ExtractMask( float4 mask )
{
#ifdef MASK_ALPHA
	return max( 0, mask.a - Dilate.Threshold ) * Dilate.GainMask;
#else
	return 0;
#endif
}

[numthreads(BilateralBlockSizeX,BilateralBlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY_C	=	dispatchThreadId.xy + int2( 0, 0);
	int2	loadXY_T	=	dispatchThreadId.xy + int2( 0, 1);
	int2	loadXY_B	=	dispatchThreadId.xy + int2( 0,-1);
	int2	loadXY_R	=	dispatchThreadId.xy + int2( 1, 0);
	int2	loadXY_L	=	dispatchThreadId.xy + int2(-1, 0);
	int2	storeXY		=	dispatchThreadId.xy;
	
	float 	mask_C		=	ExtractMask( Mask[ loadXY_C ] );
	float	mask_T		=	ExtractMask( Mask[ loadXY_T ] );
	float	mask_B		=	ExtractMask( Mask[ loadXY_B ] );
	float	mask_R		=	ExtractMask( Mask[ loadXY_R ] );
	float	mask_L		=	ExtractMask( Mask[ loadXY_L ] );
	float 	mask_Total	=	mask_T + mask_B + mask_R + mask_L;
	float	rcp_mask	=	1.0f / mask_Total;
	
	float4	source_C	=	Source[ loadXY_C ];
	float4	source_T	=	Source[ loadXY_T ];
	float4	source_B	=	Source[ loadXY_B ];
	float4	source_R	=	Source[ loadXY_R ];
	float4	source_L	=	Source[ loadXY_L ];

	if ( mask_C > EPSILON || mask_Total < EPSILON )
	{
		Target[ storeXY ]	=	source_C;
	}
	else
	{
		Target[ storeXY ]	=	source_T * mask_T * rcp_mask 
							+	source_B * mask_B * rcp_mask
							+	source_R * mask_R * rcp_mask
							+	source_L * mask_L * rcp_mask
							;
	}
}

