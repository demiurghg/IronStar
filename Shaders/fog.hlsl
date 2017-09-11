#if 0
$ubershader 	COMPUTE|INTEGRATE
#endif

#include "fog.auto.hlsl"

Texture3D<float4> 	source  : register(t0); 
RWTexture3D<float4> target  : register(u0); 


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
	int3 blockSize		=	int3(BlockSizeX,BlockSizeY,BlockSizeZ);
	
	target[ location.xyz ] = float4(1,0,1,1);
}