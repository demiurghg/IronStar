
#if 0
$ubershader 	LIGHTING
$ubershader		DILATE
$ubershader 	COLLAPSE
$ubershader 	INTEGRATE
#endif

#include "auto/radiosity.fxi"

#define NO_DECALS
#define NO_CUBEMAPS

#include "ls_core.fxi"

/*------------------------------------------------------------------------------
	Compute direct lighting :
------------------------------------------------------------------------------*/

#ifdef LIGHTING

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;
	
	
	SHADOW_RESOURCES	shadowRc;
	shadowRc.ShadowSampler	=	ShadowSampler	; 
	shadowRc.LinearSampler	=	LinearSampler	;
	shadowRc.ShadowMap		=	ShadowMap		;
	shadowRc.ShadowMask		=	ShadowMask		;
	
	GEOMETRY	geometry;
	geometry.position		=	Position[ loadXY ].xyz;
	geometry.normal			=	Normal	[ loadXY ].xyz;
	
	float4 	albedo			=	Albedo	[ loadXY ].rgba;
	
	float3 	shadow			=	ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, shadowRc, false );
	
	FLUX	flux			=	ComputeDirectLightFlux( DirectLight );
	float3 	lighting		=	ComputeLighting( flux, geometry, albedo.rgb );
	
	RadianceUav[ storeXY.xy ]	=	float4(shadow * lighting, albedo.a );
}

#endif

/*------------------------------------------------------------------------------
	Dilate ligting results :
------------------------------------------------------------------------------*/

/*------------------------------------------------------------------------------
	Collapse lighting buffer to patches :
------------------------------------------------------------------------------*/

#ifdef COLLAPSE

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY00	=	dispatchThreadId.xy * 2 + int2(0,0);
	int2	loadXY01	=	dispatchThreadId.xy * 2 + int2(0,1);
	int2	loadXY10	=	dispatchThreadId.xy * 2 + int2(1,0);
	int2	loadXY11	=	dispatchThreadId.xy * 2 + int2(1,1);
	int2	storeXY		=	dispatchThreadId.xy;
	
	float4 lighting00	=	Radiance	[ loadXY00 ];
	float4 lighting01	=	Radiance	[ loadXY01 ];
	float4 lighting10	=	Radiance	[ loadXY10 ];
	float4 lighting11	=	Radiance	[ loadXY11 ];
	
	float4 lighting		=	0.25f * ( lighting00 + lighting01 + lighting10 + lighting11 );
	
	float4 factor		=	all(float4(lighting00.a, lighting01.a, lighting10.a, lighting11.a));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif


/*------------------------------------------------------------------------------
	Gather light from all visible patches :
------------------------------------------------------------------------------*/

