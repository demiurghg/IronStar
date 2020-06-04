
#if 0
$ubershader	COMPUTE_COC
$ubershader	EXTRACT BACKGROUND
$ubershader BLUR BACKGROUND
$ubershader APPLY_DOF
#endif

#include "auto/dof.fxi"

float GetLinearDepth(float z)
{
	return 9999999; //return 1.0f / (z * LinDepthScale + LinDepthBias);
}

/*------------------------------------------------------------------------------
	Compute circle of confusion
------------------------------------------------------------------------------*/

#ifdef COMPUTE_COC

[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;
	
	CocTarget[ storeXY.xy ]	=	float4(frac(loadXY.xy/10.0f),0.5f,1);
}

#endif
