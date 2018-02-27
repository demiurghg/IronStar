
#if 0
$ubershader 	GAUSSIAN PASS1|PASS2
#endif

//-----------------------------------------------------------------------------

#include "blur.auto.hlsl"

//-----------------------------------------------------------------------------

#ifdef GAUSSIAN

Texture2D<float4> 	source  : register(t0); 
RWTexture2D<float4> target  : register(u0); 

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 location		=	dispatchThreadId.xy;
	int2 blockSize		=	int2(BlockSizeX,BlockSizeY);
	uint threadCount 	= 	BlockSizeX * BlockSizeY; 
	
	float4 value		=	source[ location ];
	
	target[ location ] = value;	
}

#endif
