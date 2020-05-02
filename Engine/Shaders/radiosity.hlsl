
#if 0
$ubershader 	LIGHTING
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
	geometry.normal			=	Normal	[ loadXY ].xyz * 2 - 1;
	geometry.normal			=	normalize( geometry.normal );
	
	float4 	albedo			=	Albedo	[ loadXY ].rgba;
	
	float3 	shadow			=	ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, shadowRc, false );
	
	FLUX	flux			=	ComputeDirectLightFlux( DirectLight );
	float3 	lighting		=	ComputeLighting( flux, geometry, albedo.rgb );
	
	RadianceUav[ storeXY.xy ]	=	float4(shadow * lighting, albedo.a );
	
	//if (all(storeXY.xy==int2(2,3))) RadianceUav[ storeXY.xy ] = float4(0,0,1000,1);
}

#endif

/*------------------------------------------------------------------------------
	Dilate ligting results :
------------------------------------------------------------------------------*/

#ifdef DILATE

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy + int2( 0, 0);
	int2	loadXY_T	=	dispatchThreadId.xy + int2( 0, 1);
	int2	loadXY_B	=	dispatchThreadId.xy + int2( 0,-1);
	int2	loadXY_R	=	dispatchThreadId.xy + int2( 1, 0);
	int2	loadXY_L	=	dispatchThreadId.xy + int2(-1, 0);
	int2	storeXY		=	dispatchThreadId.xy;
	
	float4	albedo_T	=	Albedo[ loadXY_T ];
	float4	albedo_B	=	Albedo[ loadXY_B ];
	float4	albedo_R	=	Albedo[ loadXY_R ];
	float4	albedo_L	=	Albedo[ loadXY_L ];
	
	float4	irrad_T		=	Albedo[ loadXY_T ];
	float4	irrad_B		=	Albedo[ loadXY_B ];
	float4	irrad_R		=	Albedo[ loadXY_R ];
	float4	irrad_L		=	Albedo[ loadXY_L ];
	
	
	
	float4 lighting		=	0.25 * ( lighting00 + lighting01 + lighting10 + lighting11 );
	
	float	 factor		=	all(float4(lighting00.a, lighting01.a, lighting10.a, lighting11.a));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif

/*#ifdef DENOISE

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
	
	float4 lighting		=	0.25 * ( lighting00 + lighting01 + lighting10 + lighting11 );
	
	float	 factor		=	all(float4(lighting00.a, lighting01.a, lighting10.a, lighting11.a));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif*/

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
	
	float4 lighting		=	0.25 * ( lighting00 + lighting01 + lighting10 + lighting11 );
	
	float	 factor		=	all(float4(lighting00.a, lighting01.a, lighting10.a, lighting11.a));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif


/*------------------------------------------------------------------------------
	Gather light from all visible patches :
------------------------------------------------------------------------------*/

#ifdef INTEGRATE

void AddLightMap( inout float3 lmcolor, inout float3 lmdir, float3 color, float3 dir )
{
	float lum = dot(float3(0.3f,0.5f,0.2f), lmcolor)+0;
	lmdir	+=	normalize(dir) * lum;
	lmcolor += 	color;
}

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;

	uint 	offsetCount	=	IndexMap[ loadXY ];
	uint 	offset		=	offsetCount >> 8;
	uint 	count		=	offsetCount & 0xFF;
	uint	begin		=	offset;
	uint	end			=	offset + count;
	
	float3	targetPoint	=	Position[ loadXY ].xyz;

	float3	lightmapColor	=	float3(0,0,0);
	float3	lightmapDir		=	float3(0,0,0);
	
	float3	skyDir		=	Sky[ loadXY ].xyz * 2 - 1;
	float	skyFactor	=	length( skyDir ) * Radiosity.SkyFactor;
	float3	skyColor	=	SkyBox.SampleLevel( LinearSampler, skyDir.xyz, 0 ).rgb * skyFactor;
	
	AddLightMap( lightmapColor, lightmapDir, skyColor, skyDir );
	
	for (uint index=begin; index<end; index++)
	{
		uint 	lmAddr		=	Indices[ index ];
		uint 	lmX			=	(lmAddr >> 20) & 0xFFF;
		uint 	lmY			=	(lmAddr >>  8) & 0xFFF;
		uint 	lmMip		=	(lmAddr >>  5) & 0x007;
		uint 	hitCount	=	(lmAddr >>  0) & 0x01F;
		int3	loadUVm		=	int3( lmX, lmY, lmMip );
			
		float4 	radiance	=	Radiance.Load( loadUVm ).rgba;
		float3 	normal		=	Normal.Load( loadUVm ).xyz * 2 - 1;
				normal		=	normalize(normal);
		float3 	position	=	Position.Load( loadUVm ).xyz;
		float	area		=	Area.Load( loadUVm ).x;
				
		float3 	lightDir	=	targetPoint - position;
		float	lightDist	=	length( lightDir );
		float3 	lightDirN	=	normalize( lightDir );
		float	nDotL		=	max( 0, dot( normal, lightDirN ) );
		
		float	bias		=	pow(2, lmMip*2);
		float3	light		=	radiance.rgb * nDotL / 128.0f * hitCount * Radiosity.IndirectFactor;	
		
		AddLightMap( lightmapColor, lightmapDir, light, -lightDirN * float3(1,0.2f,1) );
	}//*/
	
	LightmapColor[ storeXY.xy ]	=	float4( lightmapColor, 1 );
	LightmapDir	 [ storeXY.xy ]	=	float4( normalize(lightmapDir)*0.5f + 0.5f, 1 );
}

#endif

