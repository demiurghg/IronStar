#if 0
$ubershader 	RELIGHT
#endif

#include "relight.auto.hlsl"

TextureCubeArray	GBufferColorData		:	register(t0);
TextureCubeArray	GBufferNormalData		:	register(t1);
TextureCubeArray	SkyEnvironment			:	register(t2);

SamplerState		PointSampler			: 	register(s0);

RWTexture2D<float4>  TargetFacePosX : register(u0); 
RWTexture2D<float4>  TargetFaceNegX : register(u1); 
RWTexture2D<float4>  TargetFacePosY : register(u2); 
RWTexture2D<float4>  TargetFaceNegY : register(u3); 
RWTexture2D<float4>  TargetFacePosZ : register(u4); 
RWTexture2D<float4>  TargetFaceNegZ : register(u5); 

cbuffer CBRelightParams :  register(b0) { RELIGHT_PARAMS RelightParams : packoffset( c0 ); }	


/*-----------------------------------------------------------------------------
	TODO:
	1.	Write position in surface.hlsl and read position here.
	2.	Use shadomap for direct light.
	3.  [Optional] Use sky occlusion map for more ambient light.
	4. 	[Optional] Get 3-5 closest spot-lights without shadows and inject light.
	5.	Retrive color data from megatexture.
	6.	Move prefilter shader here.
	7.	Prefilter sky.
	8.	Apply specular and diffuse terms.
	9.	Implement better occlusion grid.
	10.	Store occlusion grid as separate content asset file.
-----------------------------------------------------------------------------*/

float4	ComputeLight ( float3 dir )
{
	float	cubeId	=	RelightParams.CubeIndex;
	float4	gbuf0	=	GBufferColorData .SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	gbuf1	=	GBufferNormalData.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	sky		=	SkyEnvironment.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	
	float3	color	=	gbuf0.rgb;
	float3	normal	=	gbuf1.xyz * 2 - 1;
	
	float	skyFactor	=	(gbuf0.xyz==float3(0,0,0)) ? 1 : 0;
	
	float3	lightDir	=	-normalize( RelightParams.DirectLightDirection.xyz );
	float3	lightColor	=	RelightParams.DirectLightIntensity.rgb;
	
	float3	lighting	=	saturate(dot(normal, lightDir)) * color * lightColor;
	
	return float4( lerp(lighting, sky.rgb, skyFactor), 1 );
}


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
	
	float	u			=	2 * (location.x) / (float)LightProbeSize - 1;
	float	v			=	2 * (location.y) / (float)LightProbeSize - 1;
	
	TargetFacePosX[location]	=	ComputeLight( float3( -1, -v, -u ) );
	TargetFaceNegX[location]	=	ComputeLight( float3(  1, -v,  u ) );
	TargetFacePosY[location]	=	ComputeLight( float3( -u,  1,  v ) );
	TargetFaceNegY[location]	=	ComputeLight( float3( -u, -1, -v ) );
	TargetFacePosZ[location]	=	ComputeLight( float3( -u, -v,  1 ) );
	TargetFaceNegZ[location]	=	ComputeLight( float3(  u, -v, -1 ) );

	// TargetFacePosX[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4( -1, -v, -u,  cubeId ), 0 ) * 10;
	// TargetFaceNegX[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4(  1, -v,  u,  cubeId ), 0 ) * 10;
	// TargetFacePosY[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4( -u,  1,  v,  cubeId ), 0 ) * 10;
	// TargetFaceNegY[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4( -u, -1, -v,  cubeId ), 0 ) * 10;
	// TargetFacePosZ[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4(  u, -v,  1,  cubeId ), 0 ) * 10;
	// TargetFaceNegZ[location]	=	GBufferNormalData.SampleLevel( PointSampler, float4(  u, -v, -1,  cubeId ), 0 ) * 10;
}
