#if 0
$ubershader 	BAKE|COPY
#endif

#include "obscurance.auto.hlsl"


/*-----------------------------------------------------------------------------
	Light probe relighting :
-----------------------------------------------------------------------------*/

#ifdef BAKE

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


[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
}

#endif


