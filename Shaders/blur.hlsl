
#if 0
$ubershader 	GAUSSIAN PASS1|PASS2
#endif

//-----------------------------------------------------------------------------

#include "blur.auto.hlsl"

//-----------------------------------------------------------------------------

float GaussDistribution( float x, float sigma )
{
	return 1 / (sqrt(2*3.141592f*sigma*sigma)) * exp( -(x*x) / (2*sigma*sigma) );
}


//-----------------------------------------------------------------------------

#ifdef GAUSSIAN

SamplerState		linearClamp	: register(s0);
Texture2D<float4> 	source  	: register(t0); 
RWTexture2D<float4> target  	: register(u0); 

[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 globalPos		=	dispatchThreadId.xy;
	int2 localPos		=	groupThreadId.xy;
	int2 blockPos		=	groupId.xy * BlockSize;
	int2 blockSize		=	int2(BlockSize,BlockSize);
	uint threadCount 	= 	BlockSize * BlockSize; 
	int blockOffset		=	BlockSize / 2;
	
	uint width, height, mips;
	
	source.GetDimensions( 0, width, height, mips );
	
	float rcpWidth	=	1.0f / width;
	float rcpHeight	=	1.0f / height;
	
	float4	accum	=	0;
	
	float2	uv		=	globalPos.xy * float2( rcpWidth, rcpHeight );
	
#ifdef PASS1	
	float2	blurDir		=	float2(rcpWidth,0);
#endif
#ifdef PASS2	
	float2	blurDir		=	float2(0,rcpHeight);
#endif
	
	for (int x=-15; x<15; x++) {
		float 	weight 	= 	GaussDistribution( x, 3.0f );
		float4	value	=	source.SampleLevel( linearClamp, uv + blurDir * x, 0 );
				accum 	+= 	value * weight;
	}
	
	target[ globalPos ] = float4(accum);
}

#endif
