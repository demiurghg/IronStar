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
	float3	lightDir	=	Params.LightDirection.xyz;
	float	shadow		=	0;
	float	count		=	0;
	
	for ( int dx=-2; dx<2; dx++ ) {
		for ( int dy=-2; dy<2; dy++ ) {
			for ( int dz=-2; dz<2; dz++ ) {
				shadow		+=	ComputeShadow( worldPos.xyz + float3(dx,dy,dz) * 0.75f );
				count++;
			}
		}
	}
	
	float 	factor		=	saturate(9*shadow / count) / 512.0f;
	float4	obscurance	=	float4( normalize(lightDir.xyz) * factor, factor );
	
	TargetBuffer[location] =	prevValue + obscurance;

	
	//TargetBuffer[location] =	ShadowMap.Load( int3(location.xy*4,0) ).r;
}

#endif


