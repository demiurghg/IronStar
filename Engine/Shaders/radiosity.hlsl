
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
static const float3 LightEpsilon = float3( 0.001f, 0.001f, 0.001f );

#ifdef LIGHTING

void ReconstructBasis(float3 normal, out float3 tangentX, out float3 tangentY)
{
	tangentX = abs(normal.x>0.7) ? float3(0,0,1) : float3(1,0,0);
	tangentY = normalize( cross(normal, tangentX) );
	tangentX = normalize( cross(tangentY, normal) );
}


[numthreads(TileSize,TileSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2	loadXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	int2	storeXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	
	SHADOW_RESOURCES	shadowRc;
	shadowRc.ShadowSampler	=	ShadowSampler	; 
	shadowRc.LinearSampler	=	LinearSampler	;
	shadowRc.ShadowMap		=	ShadowMap		;
	shadowRc.ShadowMask		=	ShadowMask		;
	
	float4 	albedo			=	Albedo[ loadXY ].rgba;
	albedo.rgb				=	LinearToSRGB( albedo.rgb );
	
	if (albedo.a>0)
	{
		float3	indirect		=	Radiance[ loadXY ].rgb * Radiosity.SecondBounce * albedo.rgb;
		float3	position		=	Position[ loadXY ].xyz;
		float3	normal			=	Normal	[ loadXY ].xyz * 2 - 1;
				normal			=	normalize( normal );
		float	size			=	Radiosity.ShadowFilter;

		//	reconstruct basis ?
		float3  tangentX, tangentY;
		ReconstructBasis( normal, tangentX, tangentY );
		
		float3 		shadow = 0;
		GEOMETRY	geometry;
		geometry.position	=	position;
		geometry.normal		=	normal;

		float2 sample_pattern[] = {
			float2( 0.25f, 0.75f ),		float2(-0.75f, 0.25f ),
			float2(-0.25f,-0.75f ),		float2( 0.75f,-0.25f ),
			float2( 0.00f, 0.00f )
		};

		for (int i=0; i<5; i++)
		{
			float3 offset = tangentX * sample_pattern[i].x * size;
						  + tangentY * sample_pattern[i].y * size;
			geometry.position = position + offset;
			shadow += 0.2*ComputeCascadedShadows( geometry, float2(0,0), CascadeShadow, shadowRc, false );
		}
		
		FLUX	flux			=	ComputeDirectLightFlux( DirectLight );
		float3 	lighting		=	ComputeLighting( flux, geometry, albedo.rgb );
		
				lighting		=	(lighting * shadow + indirect + LightEpsilon) * albedo.a;
		
		RadianceUav[ storeXY.xy ]	=	float4(lighting, albedo.a );
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

/*------------------------------------------------------------------------------
	Raytrace test 
------------------------------------------------------------------------------*/

#ifdef RAYTRACE

uint wang_hash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

RAY CreateRay( uint2 xy )
{
	float 	x 	=	( xy.x )		/ 256.0 * 2 - 1;
	float 	y 	=	( 256-xy.y ) 	/ 256.0 * 2 - 1;
	float 	z	=	(wang_hash( 199*xy.x + 2999*xy.y ) & 0xF) / 64.0f + 1.0f;
	float3 	p 	=	Camera.CameraPosition.xyz;
	float3  d 	=	Camera.CameraForward.xyz * z + Camera.CameraRight.xyz * x + Camera.CameraUp.xyz * y;
	return ConstructRay( p, normalize(d) );
}



#define STACKSIZE			64
#define STACKPUSH(index) 	stack[stackIndex++] = index
#define STACKPOP 			stack[--stackIndex]
#define STACKEMPTY			(stackIndex==0)
#define STACKGUARD			if (stackIndex>=STACKSIZE) return;

[numthreads(TileSize,TileSize,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint2 storeXY	=	dispatchThreadId.xy;
	RAY ray			=	CreateRay( dispatchThreadId.xy );
	
	float4 result	=	float4(0,0,0,9999999);
	
	uint stack[STACKSIZE];
	uint stackIndex = 0;
	uint maxIndex = 0;
	STACKPUSH(0);
	
	while (!STACKEMPTY)
	{
		STACKGUARD
		
		maxIndex = max(maxIndex, stackIndex);
		
		uint current = STACKPOP;
		BvhNode node = RtBvhTree[current];
		float tmin, tmax;
		
		if ( RayAABBIntersection( ray, node.MinBound.xyz, node.MaxBound.xyz, tmin, tmax ) )
		{
			if (tmax>0)
			{
				if (node.IsLeaf) 
				{
					Triangle tri = RtTriangles[ node.Index ];
					float t;
					float2 uv;
					if ( RayTriangleIntersection( ray, tri.Point0.xyz, tri.Point1.xyz, tri.Point2.xyz, tri.PlaneEq, t, uv ) )
					{
						if (result.w>t)
						{
							result.xyz 	= lerp(result.xyz, tri.PlaneEq.xyz*0.5+0.5, 1);
							result.w	= t;
						}
					}
				}
				else
				{
					STACKPUSH(node.Index);
					STACKPUSH(current+1);
				}
			}
		}
	}
	
	RaytraceImage[ storeXY ] = float4(result.rgb,1);
}

#endif













