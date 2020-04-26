
#if 0
$ubershader 	LIGHTING
$ubershader		DILATE
$ubershader 	COLLAPSE
$ubershader 	INTEGRATE
#endif

#include "auto/radiosity.fxi"

/*------------------------------------------------------------------------------
	Compute direct lighting :
------------------------------------------------------------------------------*/

#ifdef LIGHTING

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	storePoint			=	dispatchThreadId.xy;
	
	RadianceUav[ storePoint.xy ]	=	float4(1,0,0.5,1);
}

#endif

/*------------------------------------------------------------------------------
	Dilate ligting results :
------------------------------------------------------------------------------*/

/*------------------------------------------------------------------------------
	Collapse lighting buffer to patches :
------------------------------------------------------------------------------*/


/*------------------------------------------------------------------------------
	Gather light from all visible patches :
------------------------------------------------------------------------------*/

