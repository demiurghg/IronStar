#if 0
$ubershader		PREFILTER
#endif

#include "auto/cubegen.fxi"
#include "rgbe.fxi"

SamplerState				LinearSampler	: 	register(s0);
TextureCube					Source			:	register(t0);
RWTexture2DArray<float4>  	Target			: 	register(u0); 


/*-----------------------------------------------------------------------------
	Light probe prefiltering :
	http://graphicrants.blogspot.ru/2013/08/specular-brdf-reference.html
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "brdf.fxi"

float Lambert( float3 N, float3 H )
{
	return max(0, dot(N,H));
}


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

	roughness	=	sqrt(saturate(roughness));
	roughness	=	clamp( roughness, 1.0f / 1024.0f, 1 );
	
	int range	=	9;

	if (roughness>0.01f) {
		for (int i=-range; i<=range; i++) {
			for (int j=-range; j<=range; j++) {
				float 	x	=	i * dxy.x * 0.5f;
				float 	y	=	j * dxy.y * 0.5f;
				float3 	H 	= 	normalize(dirN + tangentX * x + tangentY * y);
				float	d	=	NDF( roughness, H, dirN );
				weight 		+= 	d;
				result.rgba	+= 	Source.SampleLevel(LinearSampler, H, mip).rgba * d;
			}
		}
	} else {
		weight	=	1;
		result	=	Source.SampleLevel(LinearSampler, dir, mip).rgba;
	}
	
	return float4(result/weight);
}


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint3 location	=	dispatchThreadId.xyz;
	uint width;
	uint height;
	
	Source.GetDimensions(width, height);
	
	uint divider	=	exp2( (int)Params.MipLevel );
	
	width  /= divider;
	height /= divider;
	
	if (location.x>=width ) return;
	if (location.y>=height) return;
	
	float2 dxy 	=	float2( 1.0f/width, 1.0f/height );
	
	float	u	=	2 * (location.x+0.5f) / (float)width  - 1;
	float	v	=	2 * (location.y+0.5f) / (float)height - 1;
	// float	u	=	2 * (location.x) / RelightParams.TargetSize - 1;
	// float	v	=	2 * (location.y) / RelightParams.TargetSize - 1;
	
	Target[ uint3(location.xy,0) ]	=	PrefilterFace( float3(  1, -v, -u ), location.xy, dxy );
	Target[ uint3(location.xy,1) ]	=	PrefilterFace( float3( -1, -v,  u ), location.xy, dxy );
	Target[ uint3(location.xy,2) ]	=	PrefilterFace( float3(  u,  1,  v ), location.xy, dxy );
	Target[ uint3(location.xy,3) ]	=	PrefilterFace( float3(  u, -1, -v ), location.xy, dxy );
	Target[ uint3(location.xy,4) ]	=	PrefilterFace( float3(  u, -v,  1 ), location.xy, dxy );
	Target[ uint3(location.xy,5) ]	=	PrefilterFace( float3( -u, -v, -1 ), location.xy, dxy );
}
#endif