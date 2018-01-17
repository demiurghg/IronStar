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
TextureCube			LightProbe				:	register(t4);
Texture3D			OcclusionGrid			:	register(t5);

SamplerState			PointSampler		: 	register(s0);
SamplerState			LinearSampler		: 	register(s1);
SamplerComparisonState	ShadowSampler		: 	register(s2);


RWTexture2DArray<float4>  TargetCube : register(u0); 

cbuffer CBRelightParams :  register(b0) { RELIGHT_PARAMS RelightParams : packoffset( c0 ); }	


/*-----------------------------------------------------------------------------
	TODO:
	1.	[*] Write position in surface.hlsl and read position here.
	2.	[*] Use shadomap for direct light.
	3.  [*] Use sky occlusion map for more ambient light.
	4. 	[Optional] Get 3-5 closest spot-lights without shadows and inject light.
	5.	Retrive color data from megatexture.
	6.	[*] Move prefilter shader here.
	7.	[*] Prefilter sky.
	8.	[*] Apply specular and diffuse terms.
	9.	Implement better occlusion grid (offset, grid density, better local occlusion)
	10.	Store occlusion grid as separate content asset file.
	
	------------------------
	
	Perfromance:
	1. Check CPU timing 	(+/- OK, ResetDeviceState is quite expensive)
	2. See CoD relighting	(fast approach with prefiltering)
	3. CopyFromRenderTargetCube is too slooooow
	
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
	float3	worldPos	=	dir * dist + RelightParams.LightProbePosition.xyz;
	
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
	
	TargetCube[int3(location.xy,0)]	=	ComputeLight( float3( -1, -v, -u ) );
	TargetCube[int3(location.xy,1)]	=	ComputeLight( float3(  1, -v,  u ) );
	TargetCube[int3(location.xy,2)]	=	ComputeLight( float3( -u,  1,  v ) );
	TargetCube[int3(location.xy,3)]	=	ComputeLight( float3( -u, -1, -v ) );
	TargetCube[int3(location.xy,4)]	=	ComputeLight( float3( -u, -v,  1 ) );
	TargetCube[int3(location.xy,5)]	=	ComputeLight( float3(  u, -v, -1 ) );
}
#endif


/*-----------------------------------------------------------------------------
	Light probe prefiltering :
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "hammersley.fxi"

float Beckmann( float3 N, float3 H, float roughness)
{
	float 	m		=	roughness*roughness;
	float	cos_a	=	dot(N,H);
	float	sin_a	=	sqrt(abs(1 - cos_a * cos_a)); // 'abs' to avoid negative values
	return	exp( -(sin_a*sin_a) / (cos_a*cos_a) / (m*m) ) / (3.1415927 * m*m * cos_a * cos_a * cos_a * cos_a );
}

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
	float3 dir = float3(x*size,y*size,1);
	return float3( dir.x, dir.y, Beckmann( float3(0,0,1), dir, roughness ) );
}

#ifdef ROUGHNESS_025
	static const float	ROUGHNESS 	= 	0.25f;
	static const float	KERNEL_SIZE	=	0.07f;
#endif
#ifdef ROUGHNESS_050
	static const float	ROUGHNESS 	= 	0.50f;
	static const float	KERNEL_SIZE	=	0.25f;
#endif
#ifdef ROUGHNESS_075
	static const float	ROUGHNESS 	= 	0.75f;
	static const float	KERNEL_SIZE	=	0.50f;
#endif
#ifdef ROUGHNESS_100
	static const float	ROUGHNESS 	= 	1.00f;
	static const float	KERNEL_SIZE	=	0.75f;
#endif

#ifdef SPECULAR
static const float3 poissonBeckmann[16]= {
	PoissonBeckmann( -0.6828758f,   0.5264853f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.9846674f,   0.1491582f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.3335175f,   0.1175671f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.1510262f,   0.9201540f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.0776904f,   0.4907993f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.6843108f,  -0.1940148f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.0070783f,  -0.1083425f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.5304128f,  -0.6343142f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.4250475f,   0.2877348f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.4858001f,   0.7253821f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.8246021f,   0.1354496f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.2029337f,  -0.4910559f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann( -0.1761186f,  -0.9231045f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.5713789f,  -0.2682010f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.3672436f,  -0.8677061f,  KERNEL_SIZE,  ROUGHNESS ),
	PoissonBeckmann(  0.7550967f,  -0.6394721f,  KERNEL_SIZE,  ROUGHNESS ),
};
#endif

float4	PrefilterFace ( float3 dir, int2 location )
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
#if 0
	int count = 91;
	weight = count;
	int rand = 0;//location.x * 17846 + location.y * 14734;
	
	for (int i=0; i<count; i++) {
		float2 E = hammersley2d(i+rand, count);
		float3 N = dir;
		float3 H = ImportanceSampleGGX( E, ROUGHNESS, N );

		result.rgb += LightProbe.SampleLevel(LinearSampler, H, 0).rgb;// * saturate(dot(N,H));
	}
#else
	for (int i=0; i<16; i++) {
		float	x	=	poissonBeckmann[i].x;
		float	y	=	poissonBeckmann[i].y;
		float	d	=	poissonBeckmann[i].z;
		float3 	H 	= 	normalize(dirN + tangentX * x + tangentY * y);
		weight 		+= 	d;
		result.rgb 	+= 	LightProbe.SampleLevel(LinearSampler, H, 0).rgb * d;
	}
#endif
	
#endif	

#ifdef DIFFUSE
	for (int i=0; i<128; i++) {
		float3 H 	= 	hammersley_sphere_uniform( i, 128 );
		float d 	= 	Lambert( H, dirN );
		weight 		+= 	d;
		float4 val	=	LightProbe.SampleLevel(LinearSampler, H, 0).rgba;
		result.rgb 	+= 	val.rgb * d * val.a;
	}
#endif
	
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
	
	if (location.x>=RelightParams.TargetSize) return;
	if (location.y>=RelightParams.TargetSize) return;
	
	float	u	=	2 * (location.x+0.5f) / (float)RelightParams.TargetSize - 1;
	float	v	=	2 * (location.y+0.5f) / (float)RelightParams.TargetSize - 1;
	// float	u	=	2 * (location.x) / RelightParams.TargetSize - 1;
	// float	v	=	2 * (location.y) / RelightParams.TargetSize - 1;
	
	TargetCube[int3(location.xy,0)]	=	PrefilterFace( float3(  1, -v, -u ), location.xy );
	TargetCube[int3(location.xy,1)]	=	PrefilterFace( float3( -1, -v,  u ), location.xy );
	TargetCube[int3(location.xy,2)]	=	PrefilterFace( float3(  u,  1,  v ), location.xy );
	TargetCube[int3(location.xy,3)]	=	PrefilterFace( float3(  u, -1, -v ), location.xy );
	TargetCube[int3(location.xy,4)]	=	PrefilterFace( float3(  u, -v,  1 ), location.xy );
	TargetCube[int3(location.xy,5)]	=	PrefilterFace( float3( -u, -v, -1 ), location.xy );
}
#endif