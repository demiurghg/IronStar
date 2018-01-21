#if 0
$ubershader 	RELIGHT
$ubershader		PREFILTER (SPECULAR ROUGHNESS_025|ROUGHNESS_050|ROUGHNESS_075|ROUGHNESS_100)|DIFFUSE
#endif

#include "relight.auto.hlsl"
#include "rgbe.fxi"

TextureCubeArray	GBufferColorData		:	register(t0);
TextureCubeArray	GBufferNormalData		:	register(t1);
TextureCubeArray	SkyEnvironment			:	register(t2);
Texture2D			ShadowMap				:	register(t3);
TextureCubeArray	LightProbe				:	register(t4);
Texture3D			OcclusionGrid			:	register(t5);

SamplerState			PointSampler		: 	register(s0);
SamplerState			LinearSampler		: 	register(s1);
SamplerComparisonState	ShadowSampler		: 	register(s2);


RWTexture2DArray<float4>  TargetCube : register(u0); 

cbuffer CBRelightParams :  register(b0) { RELIGHT_PARAMS RelightParams : packoffset( c0 ); }	


/*-----------------------------------------------------------------------------
	TODO:
	1.	[X] Write position in surface.hlsl and read position here.
	2.	[X] Use shadomap for direct light.
	3.  [X] Use sky occlusion map for more ambient light.
	4. 	[ ] Get 3-5 closest spot-lights without shadows and inject light.
	5.	[ ] Retrive color data from megatexture (use Hammersley point set to sample texture)
	6.	[X] Move prefilter shader here.
	7.	[X] Prefilter sky.
	8.	[X] Apply specular and diffuse terms.
	9.	[ ] Implement better occlusion grid (offset, grid density, better local occlusion)
	10.	[ ] Store occlusion grid as separate content asset file.
	11. [?] Better roughness distribution
	
	------------------------
	
	Perfromance:
	1. [X] Check CPU timing 	(+/- OK, ResetDeviceState is quite expensive)
	2. [X] See CoD relighting	(fast approach with prefiltering)
	3. [X] CopyFromRenderTargetCube is too slooooow
	4. [X] Batched & coherent light-probe prefiltering
	4. [ ] Batched & coherent light-probe relighting
	
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
	float	cubeId		=	RelightParams.CubeIndex;
	float4	gbuf0		=	GBufferColorData .SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	gbuf1		=	GBufferNormalData.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
	float4	sky			=	SkyEnvironment.SampleLevel( PointSampler, float4( dir,  cubeId ), 0 );
		
	float	dist		=	DecodeRGBE8( float4( gbuf0.w, 0, 0, gbuf1.w ) );
	float3	worldPos	=	float3(-1,1,1) * dir * dist + RelightParams.LightProbePosition.xyz;
	
	float3	color		=	gbuf0.rgb;
	float3	normal		=	normalize(gbuf1.xyz * 2 - 1);
	
	float	skyFactor	=	(gbuf0.xyz==float3(0,0,0)) ? 1 : 0;
	
	float3	lightDir	=	-normalize( RelightParams.DirectLightDirection.xyz );
	float3	lightColor	=	RelightParams.DirectLightIntensity.rgb;
	
	float	shadow		=	ComputeShadow( worldPos + normal * 0.05f );
	
	float3	samplePos		=	worldPos + normal*0.25 + float3(1,1,1)/2;
	float3	aogridCoords	=	samplePos.xyz/float3(128,64,128);
	float4	aogridValue		=	OcclusionGrid.SampleLevel( LinearSampler, aogridCoords, 0 ).rgba;
			aogridValue.xyz	=	aogridValue.xyz * 2 - 1;
	float 	skyOcclusion	=	length( aogridValue.xyz ) * (normal.y+1)/2;
	
	float3	lighting	=	saturate(dot(normal, lightDir)) * color * lightColor * shadow;
			lighting	=	lighting + color * RelightParams.SkyAmbient * skyOcclusion;
	
	//return float4(frac(pos/5.0f), 0);
	
	return float4( lerp(lighting, sky.rgb, skyFactor), 1-skyFactor );
}

static const float2 offsets[4] = {
	float2( 0.25f, 0.25f ),
	float2(-0.25f, 0.25f ),
	float2(-0.25f,-0.25f ),
	float2( 0.25f,-0.25f ),
};

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 	location	=	dispatchThreadId.xyz;
	
	float4 	face[6] 	= 	{ 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0 };

	for (int i=0; i<4; i++) {
		float u	=	2 * (location.x+0.25f + offsets[i].x) / (float)LightProbeSize - 1;
		float v	=	2 * (location.y+0.25f + offsets[i].y) / (float)LightProbeSize - 1;

		face[0]	+=	ComputeLight( float3( -1, -v, -u ) );
		face[1]	+=	ComputeLight( float3(  1, -v,  u ) );
		face[2]	+=	ComputeLight( float3( -u,  1,  v ) );
		face[3]	+=	ComputeLight( float3( -u, -1, -v ) );
		face[4]	+=	ComputeLight( float3( -u, -v,  1 ) );
		face[5]	+=	ComputeLight( float3(  u, -v, -1 ) );
	}
	
	TargetCube[int3(location.xy,0)]	=	face[0] / 4.0f;
	TargetCube[int3(location.xy,1)]	=	face[1] / 4.0f;
	TargetCube[int3(location.xy,2)]	=	face[2] / 4.0f;
	TargetCube[int3(location.xy,3)]	=	face[3] / 4.0f;
	TargetCube[int3(location.xy,4)]	=	face[4] / 4.0f;
	TargetCube[int3(location.xy,5)]	=	face[5] / 4.0f;
	
	// if (location.x==21 & location.y==21) {
		// TargetCube[int3(location.xy,0)]	=	float4(1000,1000,1000,1);
		// TargetCube[int3(location.xy,1)]	=	float4(1000,1000,1000,1);
		// TargetCube[int3(location.xy,2)]	=	float4(1000,1000,1000,1);
		// TargetCube[int3(location.xy,3)]	=	float4(1000,1000,1000,1);
		// TargetCube[int3(location.xy,4)]	=	float4(1000,1000,1000,1);
		// TargetCube[int3(location.xy,5)]	=	float4(1000,1000,1000,1);
	// }
}
#endif


/*-----------------------------------------------------------------------------
	Light probe prefiltering :
	http://graphicrants.blogspot.ru/2013/08/specular-brdf-reference.html
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "hammersley.fxi"
#include "brdf.fxi"

float3 ImportanceSampleGGX( float2 E, float roughness, float3 N )
{
	float m = roughness * roughness;

	float Phi = 2 * 3.1415 * E.x;
	float CosTheta = sqrt( (1 - E.y) / ( 1 + (m*m - 1) * E.y ) );
	float SinTheta = sqrt( 1 - CosTheta * CosTheta );

	float3 H;
	H.x = SinTheta * cos( Phi );
	H.y = SinTheta * sin( Phi );
	H.z = CosTheta;

	float3 UpVector = abs(N.z) < 0.999 ? float3(0,0,1) : float3(1,0,0);
	float3 TangentX = normalize( cross( UpVector, N ) );
	float3 TangentY = cross( N, TangentX );
	// tangent to world space
	return TangentX * H.x + TangentY * H.y + N * H.z;
}

float Lambert( float3 N, float3 H )
{
	return max(0, dot(N,H));
}

float3 PoissonBeckmann ( float x, float y, float size, float roughness )
{	
	float3 dir = normalize(float3(x*size,y*size,1));
	return float3( dir.x, dir.y, NDF( roughness, float3(0,0,1), dir ) );
	//return float3( dir.x, dir.y, 1 );
}

//#define USE_GGX

#ifdef ROUGHNESS_025
	static const uint 	COUNT		=	91;
	static const float	ROUGHNESS 	= 	0.22f;
	static const float	KERNEL_SIZE	=	0.1f;
#endif
#ifdef ROUGHNESS_050
	static const uint 	COUNT		=	91;
	static const float	ROUGHNESS 	= 	0.45f;
	static const float	KERNEL_SIZE	=	0.24f;
#endif
#ifdef ROUGHNESS_075
	static const uint 	COUNT		=	91;
	static const float	ROUGHNESS 	= 	0.71f;
	static const float	KERNEL_SIZE	=	0.50f;
#endif
#ifdef ROUGHNESS_100
	static const uint 	COUNT		=	91;
	static const float	ROUGHNESS 	= 	1.00f;
	static const float	KERNEL_SIZE	=	1.2f;
#endif

#ifdef SPECULAR
#include "relight.sampling.hlsl"
#endif

float4	PrefilterFace ( float3 dir, int3 location )
{
	float weight 	= 0;
	float3 result 	= 0;

	float3 dirN		= normalize(dir);
	float3 upVector = abs(dirN.z) < 0.71 ? float3(0,0,1) : float3(1,0,0);
	float3 tangentX = normalize( cross( upVector, dirN ) );
	float3 tangentY = cross( dirN, tangentX );

	float dxy	=	1 / RelightParams.TargetSize;
	
	//return LightProbe.SampleLevel(LinearSampler, dir, 0).rgba;// * saturate(dot(N,H));

#ifdef SPECULAR
	//	11 steps is perfect number of steps to pick every texel 
	//	of cubemap with initial size 256x256 and get all important 
	//	samples of Beckmann distrubution.
#ifdef USE_GGX
	int count = COUNT;
	weight = count;
	int rand = location.x * 17846 + location.y * 14734;
	
	for (int i=0; i<count; i++) {
		float2 E = hammersley2d(i+rand, count);
		float3 N = dir;
		float3 H = ImportanceSampleGGX( E, ROUGHNESS, N );

		result.rgb += LightProbe.SampleLevel(LinearSampler, float4(H, location.z), 0).rgb;// * saturate(dot(N,H));
	}
#else
	for (int i=0; i<sampleCount; i++) {
		float	x	=	poissonBeckmann[i].x;
		float	y	=	poissonBeckmann[i].y;
		float	d	=	poissonBeckmann[i].z;
		float3 	H 	= 	normalize(dirN + tangentX * x + tangentY * y);
		weight 		+= 	d;
		result.rgb 	+= 	LightProbe.SampleLevel(LinearSampler, float4(H, location.z), 0).rgb * d;
	}
#endif
	
#endif	

#ifdef DIFFUSE
	for (int i=0; i<31; i++) {
		float3 H 	= 	hammersley_sphere_uniform( i, 31 );
		float d 	= 	Lambert( H, dirN );
		weight 		+= 	d;
		float4 val	=	LightProbe.SampleLevel(LinearSampler, float4(H, location.z), 0).rgba;
		result.rgb 	+= 	val.rgb * d * val.a;
	}
#endif
	
	return float4(result/weight, 0);
}


[numthreads(PrefilterBlockSizeX,PrefilterBlockSizeX,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location	=	dispatchThreadId.xyz;
	uint width;
	uint height;
	uint count;
	
	TargetCube.GetDimensions(width, height, count);
	
	if (location.x>=width) return;
	if (location.y>=height) return;
	
	float	u	=	2 * (location.x+0.5f) / (float)width  - 1;
	float	v	=	2 * (location.y+0.5f) / (float)height - 1;
	// float	u	=	2 * (location.x) / RelightParams.TargetSize - 1;
	// float	v	=	2 * (location.y) / RelightParams.TargetSize - 1;
	
	TargetCube[int3(location.xy, location.z*6+0)]	=	PrefilterFace( float3(  1, -v, -u ), location.xyz );
	TargetCube[int3(location.xy, location.z*6+1)]	=	PrefilterFace( float3( -1, -v,  u ), location.xyz );
	TargetCube[int3(location.xy, location.z*6+2)]	=	PrefilterFace( float3(  u,  1,  v ), location.xyz );
	TargetCube[int3(location.xy, location.z*6+3)]	=	PrefilterFace( float3(  u, -1, -v ), location.xyz );
	TargetCube[int3(location.xy, location.z*6+4)]	=	PrefilterFace( float3(  u, -v,  1 ), location.xyz );
	TargetCube[int3(location.xy, location.z*6+5)]	=	PrefilterFace( float3( -u, -v, -1 ), location.xyz );
}
#endif