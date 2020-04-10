
#if 0
$ubershader BITONIC_SORT|TRANSPOSE
#endif

#include "auto/bitonicSort.fxi"

#define BITONIC_BLOCK_SIZE 256

#define TRANSPOSE_BLOCK_SIZE 16

//--------------------------------------------------------------------------------------
// Structured Buffers
//--------------------------------------------------------------------------------------

StructuredBuffer<float2> Input : register( t0 );
RWStructuredBuffer<float2> Data : register( u0 );

//--------------------------------------------------------------------------------------
// Bitonic Sort Compute Shader
//--------------------------------------------------------------------------------------
#ifdef BITONIC_SORT

groupshared float2 shared_data[2][BITONIC_BLOCK_SIZE];

[numthreads(BITONIC_BLOCK_SIZE, 1, 1)]
void CSMain( uint3 Gid : SV_GroupID, 
                  uint3 DTid : SV_DispatchThreadID, 
                  uint3 GTid : SV_GroupThreadID, 
                  uint GI : SV_GroupIndex )
{
    // Load shared data
    shared_data[0][GI] = Data[DTid.x];
    GroupMemoryBarrierWithGroupSync();
	int index = 0;
    
    // Sort the shared data
    for (unsigned int j = SortData.Level >> 1 ; j > 0 ; j >>= 1) {
	
		float2 a 	  = shared_data[index][GI & ~j];
		float2 b 	  = shared_data[index][GI | j ];
		float2 c	  = shared_data[index][GI ^ j];
		float2 d 	  = shared_data[index][GI];
		
		float2 result = ((a.x <= b.x) == (bool)(SortData.LevelMask & DTid.x)) ? c : d;
		
        shared_data[index^1][GI] = result;
		
		index ^= 1;

        GroupMemoryBarrierWithGroupSync();
    }
    
    // Store shared data
    Data[DTid.x] = shared_data[index][GI];
}
#endif

//--------------------------------------------------------------------------------------
// Matrix Transpose Compute Shader
//--------------------------------------------------------------------------------------
#ifdef TRANSPOSE

groupshared float2 transpose_shared_data[TRANSPOSE_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE];

[numthreads(TRANSPOSE_BLOCK_SIZE, TRANSPOSE_BLOCK_SIZE, 1)]
void CSMain( uint3 Gid : SV_GroupID, 
                      uint3 DTid : SV_DispatchThreadID, 
                      uint3 GTid : SV_GroupThreadID, 
                      uint GI : SV_GroupIndex )
{
    transpose_shared_data[GI] = Input[DTid.y * SortData.Width + DTid.x];
    GroupMemoryBarrierWithGroupSync();
    uint2 XY = DTid.yx - GTid.yx + GTid.xy;
    Data[XY.y * SortData.Height + XY.x] = transpose_shared_data[GTid.x * TRANSPOSE_BLOCK_SIZE + GTid.y];
}
#endif

