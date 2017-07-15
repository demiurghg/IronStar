
#if 0
$ubershader 	INTERLEAVE	
$ubershader 	DEINTERLEAVE
$ubershader 	HDAO		
$ubershader 	BILATERAL_X	
$ubershader 	BILATERAL_Y	
#endif


#include "hdao.auto.hlsl"


Texture2D<float4> 	source  : register(t0); 
RWTexture2D<float4> target  : register(u0); 


#define BLOCK_SIZE_X 16 
#define BLOCK_SIZE_Y 16 
[numthreads(BLOCK_SIZE_X,BLOCK_SIZE_Y,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location		=	int2( dispatchThreadId.x, dispatchThreadId.y );
	uint threadCount 	= 	BLOCK_SIZE_X * BLOCK_SIZE_Y; 

	//GroupMemoryBarrierWithGroupSync();

	target[location.xy] = float4(1,0,1,1);
}