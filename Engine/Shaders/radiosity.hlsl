
#if 0
$ubershader 	LIGHTING
$ubershader 	COLLAPSE
$ubershader 	INTEGRATE2
$ubershader 	INTEGRATE3
#endif

#include "auto/radiosity.fxi"

#define NO_DECALS
#define NO_CUBEMAPS

#include "ls_core.fxi"

#include "collision.fxi"

/*------------------------------------------------------------------------------
	Compute direct lighting :
------------------------------------------------------------------------------*/

#ifdef LIGHTING

// small addition to tell lit and unlit areas
static const float3 LightEpsilon = float3( 0.001f, 0.001f, 0.001f );

[numthreads(TileSize,TileSize,1)] 
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
	
	float3	indirect		=	Radiance[ loadXY ].rgb * Radiosity.SecondBounce * albedo.rgb;
	
	float3 	shadow			=	ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, shadowRc, false );
	
	FLUX	flux			=	ComputeDirectLightFlux( DirectLight );
	float3 	lighting		=	ComputeLighting( flux, geometry, albedo.rgb );
	
			lighting		=	(lighting * shadow + indirect + LightEpsilon) * albedo.a;
	
	RadianceUav[ storeXY.xy ]	=	float4(lighting, albedo.a );
}

#endif

/*------------------------------------------------------------------------------
	Dilate ligting results :
------------------------------------------------------------------------------*/

#ifdef DILATE

[numthreads(TileSize,TileSize,1)] 
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

/*------------------------------------------------------------------------------
	Collapse lighting buffer to patches :
------------------------------------------------------------------------------*/

#ifdef COLLAPSE

[numthreads(TileSize,TileSize,1)] 
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
	
	//	we use at least non-zero lighting value to collapse patches
	float	 factor		=	all(float4(lighting00.r, lighting01.r, lighting10.r, lighting11.r));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif


/*------------------------------------------------------------------------------
	Some utils :
------------------------------------------------------------------------------*/

float3 DecodeDirection (uint dir)
{
	uint	ux	=	( dir >> 3 ) & 0x7;
	uint	uy	=	( dir >> 0 ) & 0x7;

	float	fx	=	ux / 8.0f;
	float	fy	=	uy / 8.0f;
	
    float4 	nn 	=	float4(fx,fy,0,0) * float4(2,2,0,0) + float4(-1,-1,1,-1);
    float l 	=	- dot(nn.xyz,nn.xyw);
    nn.z 		= 	l * 2 - 1;
    nn.xy 		*= 	sqrt(abs(l)) * 2;
    return normalize(nn.xyz);
}

uint2 pack_color( float3 color )
{
	uint2 result;
	result.x	=	f32tof16( color.r ) | (f32tof16( color.g ) << 16 );
	result.y	=	f32tof16( color.b ) | (0xFFFFF << 16);
	return result;
}

float3 unpack_color( uint2 color )
{
	float3 result;
	result.r	=	f16tof32( color.x );
	result.g	=	f16tof32( color.x >> 16 );
	result.b	=	f16tof32( color.y );
	
	return result;
}


uint uintDivUp( uint a, uint b ) { return (a % b != 0) ? (a / b + 1) : (a / b); }

/*------------------------------------------------------------------------------
	Integrate 2D-lightmap :
------------------------------------------------------------------------------*/

#ifdef INTEGRATE2

void StoreLightmap( int2 xy, float4 shR, float4 shG, float4 shB )
{
	IrradianceL0[ xy ]	=	float4( shR.x		, shG.x			, shB.x			, 0 );
	IrradianceL1[ xy ]	=	float4( shR.y/shR.x , shG.y/shG.x	, shB.y/shB.x	, 0 ) * 0.5f + 0.5f;
	IrradianceL2[ xy ]	=	float4( shR.z/shR.x , shG.z/shG.x	, shB.z/shB.x	, 0 ) * 0.5f + 0.5f;
	IrradianceL3[ xy ]	=	float4( shR.w/shR.x , shG.w/shG.x	, shB.w/shB.x	, 0 ) * 0.5f + 0.5f;
}

groupshared uint2 radiance_cache[ PatchCacheSize ];


groupshared bool skip_tile_processing = false;

[numthreads(TileSize,TileSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	//	culling :
	float3 bboxMin		=	BBoxMin[ groupId.xy ].xyz;
	float3 bboxMax		=	BBoxMax[ groupId.xy ].xyz;

	if (groupIndex<6)
	{
		if (IsAABBOutsidePlane( FrustumPlanes[groupIndex], bboxMin, bboxMax )) skip_tile_processing = true;
	}
	
	GroupMemoryBarrierWithGroupSync();

	//	upload cache
	uint 	cacheIndex	=	Tiles[ groupId.xy ].x;
	uint 	cacheCount	=	Tiles[ groupId.xy ].y;
	uint	stride		=	TileSize * TileSize;
	
	GroupMemoryBarrierWithGroupSync();

	for (uint base=0; base<PatchCacheSize; base+=stride)
	{
		uint offset = groupThreadId.x + groupThreadId.y * TileSize;
		uint addr   = cacheIndex + base + offset;
		
		if (base+offset < cacheCount)
		{
			uint 	lmAddr		=	Cache[ addr ];
			uint 	lmX			=	(lmAddr >> 20) & 0xFFF;
			uint 	lmY			=	(lmAddr >>  8) & 0xFFF;
			uint 	lmMip		=	(lmAddr >>  5) & 0x007;
			int3	loadUVm		=	int3( lmX, lmY, lmMip );
			float3 	radiance	=	Radiance.Load( loadUVm ).rgb;
			radiance_cache[ base+offset ] = pack_color(radiance);
		}
		else
		{
			radiance_cache[ base+offset ] = pack_color(float3(10,0,5));
		}
	}//*/
	
	GroupMemoryBarrierWithGroupSync();
	
	int2	loadXY		=	dispatchThreadId.xy;
	int2	storeXY		=	dispatchThreadId.xy;

	uint 	offsetCount	=	IndexMap[ loadXY ];
	uint 	offset		=	offsetCount >> 8;
	uint 	count		=	offsetCount & 0xFF;
	
	uint	begin		=	offset;
	uint	end			=	offset + count;
	
	float4	irradianceR	=	float4( 0, 0, 0, 0 );
	float4	irradianceG	=	float4( 0, 0, 0, 0 );
	float4	irradianceB	=	float4( 0, 0, 0, 0 );
	
	float3	skyDir		=	Sky[ loadXY ].xyz * 2 - 1;
	float	skyFactor	=	length( skyDir ) * Radiosity.SkyFactor;
	float3	skyColor	=	SkyBox.SampleLevel( LinearSampler, skyDir.xyz, 0 ).rgb * skyFactor * skyFactor;
	
	irradianceR			+=	SHL1EvaluateDiffuse( skyColor.r, normalize(skyDir.xyz) );
	irradianceG			+=	SHL1EvaluateDiffuse( skyColor.g, normalize(skyDir.xyz) );
	irradianceB			+=	SHL1EvaluateDiffuse( skyColor.b, normalize(skyDir.xyz) );
	
	if (!skip_tile_processing)
	{
		for (uint index=begin; index<end; index++)
		{
			uint 	lmAddr		=	Indices[ index ];
			uint 	cacheIndex	=	(lmAddr >> 12) & 0xFFF;
			uint 	direction	=	(lmAddr >>  6) & 0x03F;
			uint 	hitCount	=	(lmAddr >>  0) & 0x03F;
			
			float3 	radiance	=	unpack_color( radiance_cache[ cacheIndex ] );
			float3 	lightDirN	=	DecodeDirection( direction );

			float3	light		=	radiance.rgb * hitCount * Radiosity.IndirectFactor;	
			
			irradianceR			+=	SHL1EvaluateDiffuse( light.r, lightDirN );
			irradianceG			+=	SHL1EvaluateDiffuse( light.g, lightDirN );
			irradianceB			+=	SHL1EvaluateDiffuse( light.b, lightDirN );
		}
	
		StoreLightmap( storeXY.xy, irradianceR, irradianceG, irradianceB );
	}
}

#endif

/*------------------------------------------------------------------------------
	Integrate 2D-lightmap :
------------------------------------------------------------------------------*/

#ifdef INTEGRATE3

void StoreLightVolume( int3 xyz, float4 shR, float4 shG, float4 shB )
{
	LightVolumeL0[ xyz ]	=	float4( shR.x		, shG.x			, shB.x			, 0 );
	LightVolumeL1[ xyz ]	=	float4( shR.y/shR.x , shG.y/shG.x	, shB.y/shB.x	, 0 ) * 0.5f + 0.5f;
	LightVolumeL2[ xyz ]	=	float4( shR.z/shR.x , shG.z/shG.x	, shB.z/shB.x	, 0 ) * 0.5f + 0.5f;
	LightVolumeL3[ xyz ]	=	float4( shR.w/shR.x , shG.w/shG.x	, shB.w/shB.x	, 0 ) * 0.5f + 0.5f;
}

groupshared uint2 radiance_cache[PatchCacheSize];

[numthreads(ClusterSize,ClusterSize,ClusterSize)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	//	upload cache
	uint 	cacheIndex	=	Clusters[ groupId.xyz ].x;
	uint 	cacheCount	=	Clusters[ groupId.xyz ].y;
	uint	stride		=	ClusterSize * ClusterSize * ClusterSize;
	
	GroupMemoryBarrierWithGroupSync();

	for (uint base=0; base<PatchCacheSize; base+=stride)
	{
		uint offset = 	groupThreadId.x + 
						groupThreadId.y * ClusterSize + 
						groupThreadId.z * ClusterSize * ClusterSize;
		uint addr   = 	cacheIndex + base + offset;
		
		if (base+offset < cacheCount)
		{
			uint 	lmAddr		=	Cache[ addr ];
			uint 	lmX			=	(lmAddr >> 20) & 0xFFF;
			uint 	lmY			=	(lmAddr >>  8) & 0xFFF;
			uint 	lmMip		=	(lmAddr >>  5) & 0x007;
			int3	loadUVm		=	int3( lmX, lmY, lmMip );
			float3 	radiance	=	Radiance.Load( loadUVm ).rgb;
			radiance_cache[ base+offset ] = pack_color(radiance);
		}
		else
		{
			radiance_cache[ base+offset ] = pack_color(float3(10,0,5));
		}
	}//*/
	
	GroupMemoryBarrierWithGroupSync();
	
	int3	loadXYZ		=	dispatchThreadId.xyz;
	int3	storeXYZ	=	dispatchThreadId.xyz;

	uint 	offsetCount	=	IndexVolume[ loadXYZ ];
	uint 	offset		=	offsetCount >> 8;
	uint 	count		=	offsetCount & 0xFF;
	uint	begin		=	offset;
	uint	end			=	offset + count;
	
	float4	irradianceR	=	float4( 0, 0, 0, 0 );
	float4	irradianceG	=	float4( 0, 0, 0, 0 );
	float4	irradianceB	=	float4( 0, 0, 0, 0 );
	
	float3	skyDir		=	SkyVolume[ loadXYZ ].xyz * 2 - 1;
	float	skyFactor	=	length( skyDir ) * Radiosity.SkyFactor;
	float3	skyColor	=	SkyBox.SampleLevel( LinearSampler, skyDir.xyz, 0 ).rgb * skyFactor * skyFactor;
	
	irradianceR			+=	SHL1EvaluateDiffuse( skyColor.r, normalize(skyDir.xyz) );
	irradianceG			+=	SHL1EvaluateDiffuse( skyColor.g, normalize(skyDir.xyz) );
	irradianceB			+=	SHL1EvaluateDiffuse( skyColor.b, normalize(skyDir.xyz) );
	
	//if (!skip_tile_processing)
	for (uint index=begin; index<end; index++)
	{
		uint 	lmAddr		=	Indices[ index ];
		uint 	cacheIndex	=	(lmAddr >> 12) & 0xFFF;
		uint 	direction	=	(lmAddr >>  6) & 0x03F;
		uint 	hitCount	=	(lmAddr >>  0) & 0x03F;
		
		float3 	radiance	=	unpack_color( radiance_cache[ cacheIndex ] );
		float3 	lightDirN	=	DecodeDirection( direction );

		float3	light		=	radiance.rgb * hitCount * Radiosity.IndirectFactor;	
		
		irradianceR			+=	SHL1EvaluateDiffuse( light.r, lightDirN );
		irradianceG			+=	SHL1EvaluateDiffuse( light.g, lightDirN );
		irradianceB			+=	SHL1EvaluateDiffuse( light.b, lightDirN );
	}//*/

	StoreLightVolume( storeXYZ, irradianceR, irradianceG, irradianceB );
}

#endif















