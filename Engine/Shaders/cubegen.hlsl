#if 0
$ubershader		PREFILTER MIP0|MIP1|MIP2|MIP3|MIP4|MIP5
#endif

#include "auto/cubegen.fxi"
#include "rgbe.fxi"

/*-----------------------------------------------------------------------------
	Light probe prefiltering :
	http://graphicrants.blogspot.ru/2013/08/specular-brdf-reference.html
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "ls_brdf.fxi"

float3 DirNDFFunc ( float x, float y, float size, float roughness )
{	
	float3 dir = normalize(float3(x*size,y*size,1));
	return float3( dir.x, dir.y, NDF( roughness, float3(0,0,1), dir ) );
	//return float3( dir.x, dir.y, 1 );
}

#ifdef MIP0
#define ROUGHNESS	0.001f
#define KERNEL_SIZE	0.001f
#endif

#ifdef MIP1
#define ROUGHNESS	0.089f
#define KERNEL_SIZE	0.025f
#endif

#ifdef MIP2
#define ROUGHNESS	0.252f
#define KERNEL_SIZE	0.100f
#endif

#ifdef MIP3
#define ROUGHNESS	0.465f
#define KERNEL_SIZE	0.210f
#endif

#ifdef MIP4
#define ROUGHNESS	0.716f
#define KERNEL_SIZE	0.500f
#endif

#ifdef MIP5
#define ROUGHNESS	1.000f
#define KERNEL_SIZE	1.100f
#endif

static const uint sampleCount = 32;
static const float3 sampleDirWeights[32]= {
	DirNDFFunc(  0.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  1.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  1.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.25,   1.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,   1.25, KERNEL_SIZE, ROUGHNESS ),

	DirNDFFunc( -0.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -1.25,   0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -1.25,   0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.25,   1.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,   1.25, KERNEL_SIZE, ROUGHNESS ),

	DirNDFFunc( -0.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -1.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -1.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.24,  -1.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc( -0.75,  -1.25, KERNEL_SIZE, ROUGHNESS ),

	DirNDFFunc(  0.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  1.25,  -0.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  1.25,  -0.75, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.25,  -1.25, KERNEL_SIZE, ROUGHNESS ),
	DirNDFFunc(  0.75,  -1.25, KERNEL_SIZE, ROUGHNESS ),
};


float4	PrefilterFace ( float3 dir, uint2 location, float2 dxy )
{
	float weight 		= 0.0001;
	float4 result 		= 0;
	
	float3 dirN			= normalize(dir);
	float3 upVector 	= abs(dirN.z) < 0.71 ? float3(0,0,1) : float3(1,0,0);
	float3 tangentX 	= normalize( cross( upVector, dirN ) );
	float3 tangentY 	= cross( dirN, tangentX );
	float  mip			= Params.MipLevel;
	float  roughness	= Params.Roughness;
	float4 tint			= float4(1,1,1,1);
	
#ifdef MIP0
	return Source.SampleLevel(LinearSampler, dir, 0).rgba;
#else
	
	if (dir.z>0) {
	
	for (uint i=0; i<sampleCount; i++) {
		float	x	=	sampleDirWeights[i].x;
		float	y	=	sampleDirWeights[i].y;
		float	d	=	sampleDirWeights[i].z;
		float3 	H 	= 	normalize(dirN + tangentX * x + tangentY * y);
		weight 		+= 	d;
		result.rgba	+= 	Source.SampleLevel(LinearSampler, H, 0).rgba * d;
	}
	
	} else {
		
	//roughness	=	sqrt(saturate(roughness));
	roughness	=	ROUGHNESS;//saturate(roughness);
	roughness	=	clamp( roughness, 0.1f, 1 );
	
	int 	range	=	5;
	float	scale	=	1.0;

	for (int i=-range; i<=range; i++) {
		for (int j=-range; j<=range; j++) {
			float 	x	=	i * dxy.x * scale;
			float 	y	=	j * dxy.y * scale;
			float3 	H 	= 	normalize(dirN + tangentX * x + tangentY * y);
			float	d	=	NDF( roughness, H, dirN );
			weight 		+= 	d;
			result.rgba	+= 	Source.SampleLevel(LinearSampler, H, mip).rgba * d;
		}
	}
	}
	
	return float4(result/weight) * tint;
#endif
}


[numthreads(BlockSize,BlockSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	float3 location	=	dispatchThreadId.xyz;
	float width		=	Params.TargetSize;
	float height	=	Params.TargetSize;
	
	if (location.x>=width ) return;
	if (location.y>=height) return;
	
	float2 dxy 	=	float2( 1.0f/width, 1.0f/height );
	
	float	u	=	2 * (location.x+0.5f) / (float)width  - 1;
	float	v	=	2 * (location.y+0.5f) / (float)height - 1;
	
	Target[ uint3(location.xy,0) ]	=	PrefilterFace( float3(  1, -v, -u ), location.xy, dxy );
	Target[ uint3(location.xy,1) ]	=	PrefilterFace( float3( -1, -v,  u ), location.xy, dxy );
	Target[ uint3(location.xy,2) ]	=	PrefilterFace( float3(  u,  1,  v ), location.xy, dxy );
	Target[ uint3(location.xy,3) ]	=	PrefilterFace( float3(  u, -1, -v ), location.xy, dxy );
	Target[ uint3(location.xy,4) ]	=	PrefilterFace( float3(  u, -v,  1 ), location.xy, dxy );
	Target[ uint3(location.xy,5) ]	=	PrefilterFace( float3( -u, -v, -1 ), location.xy, dxy );
}
#endif