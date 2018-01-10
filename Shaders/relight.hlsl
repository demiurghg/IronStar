#if 0
$ubershader 	RELIGHT
$ubershader		PREFILTER
#endif

#include "relight.auto.hlsl"
#include "rgbe.fxi"

TextureCubeArray	GBufferColorData		:	register(t0);
TextureCubeArray	GBufferNormalData		:	register(t1);
TextureCubeArray	SkyEnvironment			:	register(t2);
Texture2D			ShadowMap				:	register(t3);
TextureCube			LightProbe				:	register(t4);

SamplerState			PointSampler		: 	register(s0);
SamplerState			LinearSampler		: 	register(s1);
SamplerComparisonState	ShadowSampler		: 	register(s2);


RWTexture2DArray<float4>  TargetFacePosX : register(u0); 
RWTexture2DArray<float4>  TargetFaceNegX : register(u1); 
RWTexture2DArray<float4>  TargetFacePosY : register(u2); 
RWTexture2DArray<float4>  TargetFaceNegY : register(u3); 
RWTexture2DArray<float4>  TargetFacePosZ : register(u4); 
RWTexture2DArray<float4>  TargetFaceNegZ : register(u5); 

cbuffer CBRelightParams :  register(b0) { RELIGHT_PARAMS RelightParams : packoffset( c0 ); }	


/*-----------------------------------------------------------------------------
	TODO:
	1.	[*] Write position in surface.hlsl and read position here.
	2.	[*] Use shadomap for direct light.
	3.  [Optional] Use sky occlusion map for more ambient light.
	4. 	[Optional] Get 3-5 closest spot-lights without shadows and inject light.
	5.	Retrive color data from megatexture.
	6.	[*] Move prefilter shader here.
	7.	Prefilter sky.
	8.	Apply specular and diffuse terms.
	9.	Implement better occlusion grid.
	10.	Store occlusion grid as separate content asset file.
-----------------------------------------------------------------------------*/

/*-----------------------------------------------------------------------------
	Light probe relighting :
-----------------------------------------------------------------------------*/

#ifdef RELIGHT
float ComputeShadow ( float3 worldPos )
{	
	float4 scaleOffset	=	RelightParams.ShadowRegion;
	float4 projectedPos = 	mul( float4(worldPos,1), RelightParams.ShadowViewProjection );
	projectedPos.xy 	/= 	projectedPos.w;
	projectedPos.w   	= 	1;
	
	float2	shadowUV	=	mad( projectedPos.xy, scaleOffset.xy, scaleOffset.zw );
	float   depthCmp	= 	projectedPos.z;

	float	shadow		=	ShadowMap.SampleCmpLevelZero( ShadowSampler, shadowUV, depthCmp ).r;
	
	return	shadow;
	
	//max(abs(projection.x), abs(projection.y));//length(temp.xy);
}


float4	ComputeLight ( float3 dir )
{
	float	cubeId	=	RelightParams.CubeIndex;
	float4	gbuf0	=	GBufferColorData .SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	gbuf1	=	GBufferNormalData.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	sky		=	SkyEnvironment.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	
	float	dist	=	DecodeRGBE8( float4( gbuf0.w, 0, 0, gbuf1.w ) );
	float3	pos		=	dir * dist + RelightParams.LightProbePosition.xyz;
	
	float3	color	=	gbuf0.rgb;
	float3	normal	=	normalize(gbuf1.xyz * 2 - 1);
	
	float	skyFactor	=	(gbuf0.xyz==float3(0,0,0)) ? 1 : 0;
	
	float3	lightDir	=	-normalize( RelightParams.DirectLightDirection.xyz );
	float3	lightColor	=	RelightParams.DirectLightIntensity.rgb;
	
	float	shadow		=	ComputeShadow( pos + normal * 0.1f );
	
	float3	lighting	=	saturate(dot(normal, lightDir)) * color * lightColor * shadow;
	
	//return float4(frac(pos/5.0f), 0);
	
	return float4( lerp(lighting, sky.rgb, skyFactor), 1 );
}

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 	location	=	dispatchThreadId.xyz;
	
	float	u			=	2 * (location.x+0.5f) / (float)LightProbeSize - 1;
	float	v			=	2 * (location.y+0.5f) / (float)LightProbeSize - 1;
	
	TargetFacePosX[location]	=	ComputeLight( float3( -1, -v, -u ) );
	TargetFaceNegX[location]	=	ComputeLight( float3(  1, -v,  u ) );
	TargetFacePosY[location]	=	ComputeLight( float3( -u,  1,  v ) );
	TargetFaceNegY[location]	=	ComputeLight( float3( -u, -1, -v ) );
	TargetFacePosZ[location]	=	ComputeLight( float3( -u, -v,  1 ) );
	TargetFaceNegZ[location]	=	ComputeLight( float3(  u, -v, -1 ) );
}
#endif


/*-----------------------------------------------------------------------------
	Light probe prefiltering :
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

float Beckmann( float3 N, float3 H, float roughness)
{
	float 	m		=	roughness*roughness;
	float	cos_a	=	dot(N,H);
	float	sin_a	=	sqrt(abs(1 - cos_a * cos_a)); // 'abs' to avoid negative values
	return	exp( -(sin_a*sin_a) / (cos_a*cos_a) / (m*m) ) / (3.1415927 * m*m * cos_a * cos_a * cos_a * cos_a );
}


float4	PrefilterFace ( float3 dir )
{
	float weight 	= 0;
	float3 result 	= 0;

	float3 dirN		= normalize(dir);
	float3 upVector = abs(dirN.z) < 0.71 ? float3(0,0,1) : float3(1,0,0);
	float3 tangentX = normalize( cross( upVector, dirN ) );
	float3 tangentY = cross( dirN, tangentX );

	float dxy	=	1 / RelightParams.TargetSize;
	
	//	11 steps is perfect number of steps to pick every texel 
	//	of cubemap with initial size 256x256 and get all important 
	//	samples of Beckmann distrubution.
	for (float x=-5; x<=5; x+=1 ) {
		for (float y=-5; y<=5; y+=1 ) {
			float3 H 	= 	normalize(dirN + tangentX * x * dxy + tangentY * y * dxy);
			float d 	= 	Beckmann( H, dirN, RelightParams.Roughness.x );
			weight 		+= 	d;
			result.rgb 	+= 	LightProbe.SampleLevel(LinearSampler, H, 0).rgb * d;
		}
	}
	
	return float4(result/weight, 0);
}


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location	=	dispatchThreadId.xyz;
	
	// if (location.x>=RelightParams.TargetSize) return;
	// if (location.y>=RelightParams.TargetSize) return;
	
	float	u		=	2 * (location.x) / RelightParams.TargetSize - 1;
	float	v		=	2 * (location.y) / RelightParams.TargetSize - 1;
	
	TargetFacePosX[location]	=	PrefilterFace( float3(  1, -v, -u ) );
	TargetFaceNegX[location]	=	PrefilterFace( float3( -1, -v,  u ) );
	TargetFacePosY[location]	=	PrefilterFace( float3(  u,  1,  v ) );
	TargetFaceNegY[location]	=	PrefilterFace( float3(  u, -1, -v ) );
	TargetFacePosZ[location]	=	PrefilterFace( float3(  u, -v,  1 ) );
	TargetFaceNegZ[location]	=	PrefilterFace( float3( -u, -v, -1 ) );
}
#endif