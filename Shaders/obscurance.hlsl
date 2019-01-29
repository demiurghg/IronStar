#if 0
$ubershader 	BAKE|COPY
#endif

#include "obscurance.auto.hlsl"

cbuffer CBParams :  register(b0) { BAKE_PARAMS Params : packoffset( c0 ); }	


/*-----------------------------------------------------------------------------
	COPY
-----------------------------------------------------------------------------*/

#ifdef COPY

RWTexture3D<float4>		Target	: register(u0);
Texture3D<float4>		Source	: register(t0);

[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{	
	uint3	location	=	dispatchThreadId;
	Target[location] =	Source.Load( int4( location, 0 ) );
}

#endif



/*-----------------------------------------------------------------------------
	Light probe relighting :
-----------------------------------------------------------------------------*/

#ifdef BAKE

RWTexture3D<float4>		TargetBuffer	: register(u0);
SamplerComparisonState	ShadowSampler	: register(s0);
Texture2D				ShadowMap 		: register(t0);
Texture3D				SourceBuffer	: register(t1);


float ComputeShadow ( float3 worldPos )
{	
	float4 projectedPos = 	mul( float4(worldPos,1), Params.ShadowViewProjection );
	projectedPos.xy 	/= 	projectedPos.w;
	projectedPos.w   	= 	1;
	
	float2	shadowUV	=	mad( projectedPos.xy, float2(0.5f, -0.5f), float2(0.5f,0.5f) );
	float   depthCmp	= 	projectedPos.z;

	float	shadow		=	ShadowMap.SampleCmpLevelZero( ShadowSampler, shadowUV, depthCmp ).r;
	
	return	shadow;
}


[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{	
	uint3	location	=	dispatchThreadId;
	float4	prevValue	=	SourceBuffer.Load( int4(location,0) );
	float4	worldPos	=	float4( location.xyz * 2 - float3(128,0,128), 1 );
	
	float 	size		=	2.5f;
	float	dd			=	0.5f;
	float 	count		=	0;
	float	shadow		=	0;
	
	for ( float dx=-size; dx<size; dx += dd ) {
		for ( float dy=-size; dy<size; dy += dd ) {
			for ( float dz=-size; dz<size; dz += dd ) {
				shadow		+=	ComputeShadow( worldPos + float3(dx,dy,dz) );
				count++;
			}
		}
	}
	
	TargetBuffer[location] =	prevValue  + saturate(2.5*shadow / count) / 256.0f;

	
	//TargetBuffer[location] =	ShadowMap.Load( int3(location.xy*4,0) ).r;
}

#endif


