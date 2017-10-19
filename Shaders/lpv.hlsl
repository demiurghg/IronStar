#if 0
$ubershader 	INJECT|PROPAGATE
#endif

#include "lpv.auto.hlsl"

/*-----------------------------------------------------------------------------
	Samplers and buffers :
-----------------------------------------------------------------------------*/

SamplerState				Sampler			: 	register(s0);
SamplerComparisonState		ShadowSampler	: 	register(s1);

Texture3D<float4> 			Source  			:	register(t0); 
RWTexture3D<float4> 		Target  			:	register(u0); 

Texture3D<uint2>			ClusterTable		: 	register(t1);
Buffer<uint>				LightIndexTable		: 	register(t2);
StructuredBuffer<LIGHT>		LightDataTable		:	register(t3);
Texture2D					ShadowMap			:	register(t4);
Texture2D					ShadowMask			:	register(t5);

cbuffer CB1 : register(b0) { 
	LPVDATA lpvData; 
};


/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef INJECT

[numthreads(BlockSize,BlockSize,BlockSize)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
	int3 blockSize		=	int3(BlockSize,BlockSize,BlockSize);
	
	Target[ location.xyz ] = Source[ location ];
}

#endif

/*-----------------------------------------------------------------------------
	Pre-intagrate flux and fog :
-----------------------------------------------------------------------------*/

#ifdef PROPAGATE

[numthreads(BlockSize,BlockSize,BlockSize)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
}

#endif


#if 0

struct Point {
	float4 coord;
	float4 color;
}

RWTexture3D<float4> 		Target  			:	register(u0); 
StructuredBuffer<Point>		PointCloud			:	register(b0);

[numthreads(BlockSize,1,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int location		=	dispatchThreadId.x;
	
	int3 dst		=	floor( PointCloud[location].coord );
	
	Target[ dst ] += PointCloud[location].color;
	
}



#endif