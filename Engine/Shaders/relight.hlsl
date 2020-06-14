#if 0
$ubershader 	RELIGHT
#endif

#include "auto/relight.fxi"
#include "rgbe.fxi"
#include "gamma.fxi"
#include "shl1.fxi"

/*-----------------------------------------------------------------------------
	Light probe relighting :
-----------------------------------------------------------------------------*/

#ifdef RELIGHT

float4	ComputeLight ( float3 dir )
{
	float	cubeId		=	RelightParams.CubeIndex;
	float4	color		=	GBufferColorData .SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	mapping		=	GBufferNormalData.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	sky			=	SkyCube.SampleLevel( LinearSampler, dir * float3(1,1,1), 0 );
	
	float3	lightmap0	=	LightMap0.SampleLevel( LinearSampler, mapping.xy, 0 ).rgb;
	float3	lightmap1	=	LightMap1.SampleLevel( LinearSampler, mapping.xy, 0 ).rgb;
	float3	lightmap	=	lightmap0 + lightmap1 * RelightParams.RadiosityLevel * 3.14f;
	
	float3	result		=	lerp( color.rgb * lightmap, sky.rgb, 1 - color.a );

	return float4( result, 1 );
}


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 	location	=	dispatchThreadId.xyz;
	
	float4 	face[6] 	= 	{ 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0 };

	float u	=	2 * (location.x + 0 * 0.25f) / (float)LightProbeSize - 1;
	float v	=	2 * (location.y + 0 * 0.25f) / (float)LightProbeSize - 1;
	
	//float4 brightPoint = (location.x == 90) && (location.y==30) ? float4(500,400,100,1) : float4(0,0,0,0);

	face[0]	+=	ComputeLight( float3(  1, -v, -u ) );
	face[1]	+=	ComputeLight( float3( -1, -v,  u ) );
	face[2]	+=	ComputeLight( float3(  u,  1,  v ) );
	face[3]	+=	ComputeLight( float3(  u, -1, -v ) );
	face[4]	+=	ComputeLight( float3(  u, -v,  1 ) );
	face[5]	+=	ComputeLight( float3( -u, -v, -1 ) );
	
	TargetCube[int3(location.xy,0)]	=	face[0];// + brightPoint;
	TargetCube[int3(location.xy,1)]	=	face[1];
	TargetCube[int3(location.xy,2)]	=	face[2];
	TargetCube[int3(location.xy,3)]	=	face[3];
	TargetCube[int3(location.xy,4)]	=	face[4];
	TargetCube[int3(location.xy,5)]	=	face[5];
}
#endif


