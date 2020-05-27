
#if 0
$ubershader 	LIGHTING
$ubershader 	COLLAPSE
$ubershader 	INTEGRATE2
$ubershader 	INTEGRATE3
$ubershader		RAYTRACE
#endif

#include "auto/radiosity.fxi"

#define NO_DECALS
#define NO_CUBEMAPS

#define SKIP_MOST_DETAILED_CASCADES
#include "ls_core.fxi"
#include "gamma.fxi"

#include "collision.fxi"

/*------------------------------------------------------------------------------
	Compute direct lighting :
------------------------------------------------------------------------------*/

// small addition to tell lit and unlit areas
static const float3 LightEpsilon = float3( 0.0001f, 0.0001f, 0.0001f );

#ifdef LIGHTING

void ReconstructBasis(float3 normal, out float3 tangentX, out float3 tangentY)
{
	tangentX = abs(normal.x>0.7) ? float3(0,0,1) : float3(1,0,0);
	tangentY = normalize( cross(normal, tangentX) );
	tangentX = normalize( cross(tangentY, normal) );
}


groupshared uint light_indices[ TileSize * TileSize ];
groupshared uint light_count = 0;


[numthreads(TileSize,TileSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint2 	tileLoadXY	=	groupId.xy + Radiosity.RegionXY/8;
	float3 	bboxMin		=	BBoxMin[ tileLoadXY ].xyz;
	float3 	bboxMax		=	BBoxMax[ tileLoadXY ].xyz;

	int2	loadXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	int2	storeXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	
	SHADOW_RESOURCES	shadowRc;
	shadowRc.ShadowSampler	=	ShadowSampler	; 
	shadowRc.LinearSampler	=	LinearSampler	;
	shadowRc.ShadowMap		=	ShadowMap		;
	shadowRc.ShadowMask		=	ShadowMask		;
	
	float4 	albedo			=	Albedo[ loadXY ].rgba;
	
	//---------------------------------
	
	if (1)
	{
		uint 	light_index		=	groupIndex;
		LIGHT 	light			=	Lights[light_index];
		uint 	current_index;
		
		float4 planes[6], left, right, top, bottom, near, far;
		
		GetFrustumPlanesFromMatrix( light.ViewProjection, left, right, top, bottom, near, far );
		
		planes[0]	=	(-1) * left;
		planes[1] 	=	(-1) * right;
		planes[2]	=	(-1) * top;
		planes[3] 	=	(-1) * bottom;
		planes[4]	=	(-1) * near;
		planes[5] 	=	(-1) * far;

		if ( light.LightType!=LightTypeNone && FrustumAABBIntersect( planes, bboxMin, bboxMax )!=COLLISION_OUTSIDE )
		{
			InterlockedAdd( light_count, 1, current_index );
			light_indices[ current_index ] = light_index;
		}
	}
	
	GroupMemoryBarrierWithGroupSync();
	
	//---------------------------------
	
	if (albedo.a>0)
	{
		float3	indirect		=	Radiance[ loadXY ].rgb * Radiosity.SecondBounce;
		float3	position		=	Position[ loadXY ].xyz;
		float3	normal			=	Normal	[ loadXY ].xyz * 2 - 1;
				normal			=	normalize( normal );
		float	size			=	Radiosity.ShadowFilter;

		//	reconstruct basis ?
		float3  tangentX, tangentY;
		ReconstructBasis( normal, tangentX, tangentY );
		
		GEOMETRY	geometry;
		geometry.position	=	position;
		geometry.normal		=	normal;

		float3 totalLight 	=	0;
		float3 whiteAlbedo	=	float3(1,1,1);

		//-----------------------------
		// compute spot lights :
		//-----------------------------
		
		for (uint index=0; index<light_count; index++)
		{
			LIGHT light =	Lights[ light_indices[ index ] ];
			
			FLUX  flux 	=	ComputePointLightFlux( geometry, light, shadowRc );
			totalLight 	+= 	ComputeLighting( flux, geometry, whiteAlbedo );
		}

		//-----------------------------
		// compute direct light :
		//-----------------------------
		
		float2 sample_pattern[] = {
			float2( 0.25f, 0.75f ),		float2(-0.75f, 0.25f ),
			float2(-0.25f,-0.75f ),		float2( 0.75f,-0.25f ),
			float2( 0.00f, 0.00f )
		};
		float3 		shadow = 0;

		for (int i=0; i<5; i++)
		{
			float3 offset = tangentX * sample_pattern[i].x * size;
						  + tangentY * sample_pattern[i].y * size;
			geometry.position = position + offset;
			shadow += 0.2*ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, shadowRc, false );
		}
		
		FLUX	flux	=	ComputeDirectLightFlux( DirectLight );
		totalLight		+=	ComputeLighting( flux, geometry, whiteAlbedo ) * shadow;
		
		totalLight		+=	indirect;
		totalLight		+=	LightEpsilon;
		
		RadianceUav[ storeXY.xy ]	=	float4(totalLight * albedo.a, albedo.a );
	}
	else
	{
		RadianceUav[ storeXY.xy ]	=	float4(0,0,0,0);
	}
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
	int2	loadXY00	=	(dispatchThreadId.xy + Radiosity.RegionXY) * 2 + int2(0,0);
	int2	loadXY01	=	(dispatchThreadId.xy + Radiosity.RegionXY) * 2 + int2(0,1);
	int2	loadXY10	=	(dispatchThreadId.xy + Radiosity.RegionXY) * 2 + int2(1,0);
	int2	loadXY11	=	(dispatchThreadId.xy + Radiosity.RegionXY) * 2 + int2(1,1);
	int2	storeXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	
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
	uint	ux	=	( dir >> 6 ) & 0x3F;
	uint	uy	=	( dir >> 0 ) & 0x3F;

	float	fx	=	ux / 64.0f;
	float	fy	=	uy / 64.0f;
	
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
	shR.xyz += LightEpsilon;
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
	uint2 	tileLoadXY	=	groupId.xy + Radiosity.RegionXY/8;
	int2	loadXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	int2	storeXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	
	#if 0
	//	culling :
	float3 bboxMin		=	BBoxMin[ tileLoadXY ].xyz;
	float3 bboxMax		=	BBoxMax[ tileLoadXY ].xyz;

	if (groupIndex<6)
	{
		if (IsAABBOutsidePlane( FrustumPlanes[groupIndex], bboxMin, bboxMax )) skip_tile_processing = true;
	}
	
	GroupMemoryBarrierWithGroupSync();
	#endif

	//	upload cache
	uint 	cacheIndex	=	Tiles[ tileLoadXY ].x;
	uint 	cacheCount	=	Tiles[ tileLoadXY ].y;
	uint	cacheBase	=	Tiles[ tileLoadXY ].z;
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
			float3 	color		=	Albedo.Load( loadUVm ).rgb;
					//color		=	LinearToSRGB( color );
			radiance_cache[ base+offset ] = pack_color(radiance * color);
		}
		else
		{
			radiance_cache[ base+offset ] = pack_color(float3(10,0,5));
		}
	}//*/
	
	GroupMemoryBarrierWithGroupSync();

	uint 	offsetCount	=	IndexMap[ loadXY ];
	uint 	offset		=	(offsetCount >> 8) + cacheBase;
	uint 	count		=	(offsetCount & 0xFF);
	
	uint	begin		=	offset;
	uint	end			=	offset + count;
	
	float4	irradianceR	=	float4( 0, 0, 0, 0 );
	float4	irradianceG	=	float4( 0, 0, 0, 0 );
	float4	irradianceB	=	float4( 0, 0, 0, 0 );
	
	float3	skyDir		=	Sky[ loadXY ].xyz * 2 - 1;
	float	skyFactor	=	length( skyDir ) * Radiosity.SkyFactor;
	float3	skyColor	=	SkyBox.SampleLevel( LinearSampler, skyDir.xyz, 0 ).rgb * skyFactor * skyFactor;
	
	// irradianceR			+=	SHL1EvaluateDiffuse( Radiance[loadXY].r, float3(0,0,0) );
	// irradianceG			+=	SHL1EvaluateDiffuse( Radiance[loadXY].g, float3(0,0,0) );
	// irradianceB			+=	SHL1EvaluateDiffuse( Radiance[loadXY].b, float3(0,0,0) );

	irradianceR			+=	SHL1EvaluateDiffuse( skyColor.r, normalize(skyDir.xyz) );
	irradianceG			+=	SHL1EvaluateDiffuse( skyColor.g, normalize(skyDir.xyz) );
	irradianceB			+=	SHL1EvaluateDiffuse( skyColor.b, normalize(skyDir.xyz) );
	
	if (!skip_tile_processing)
	{
		for (uint index=begin; index<end; index++)
		{
			uint 	lmAddr		=	Indices[ index ];
			uint 	cacheIndex	=	(lmAddr >> 20) & 0xFFF;
			uint 	direction	=	(lmAddr >>  8) & 0xFFF;
			uint 	hitCount	=	(lmAddr >>  0) & 0x0FF;
			
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
	Integrate 3D-lightmap :
------------------------------------------------------------------------------*/

#ifdef INTEGRATE3

void StoreLightVolume( int3 xyz, float4 shR, float4 shG, float4 shB )
{
	shR.xyz += LightEpsilon;
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
	uint	cacheBase	=	Clusters[ groupId.xyz ].z;
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
	uint 	offset		=	(offsetCount >> 8) + cacheBase;
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
		uint 	cacheIndex	=	(lmAddr >> 20) & 0xFFF;
		uint 	direction	=	(lmAddr >>  8) & 0xFFF;
		uint 	hitCount	=	(lmAddr >>  0) & 0x0FF;
		
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














