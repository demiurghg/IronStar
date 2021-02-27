
#if 0
$ubershader 	ILLUMINATE
$ubershader 	COLLAPSE
$ubershader 	INTEGRATE2
$ubershader 	INTEGRATE3
#endif

#include "auto/radiosity.fxi"
#include "raytracer.hlsl"
#include "hammersley.fxi"

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
static const float NoLight = 0.00001f;
static const float3 LightEpsilon = float3( 0.0001f, 0.0001f, 0.0001f );
static const float3 WhiteColor = float3( 0.875f, 0.875f, 0.875f );

#ifdef ILLUMINATE

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
	uint2 	tileLoadXY	=	groupId.xy + Radiosity.RegionXY/8;
	int2	loadXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	int2	storeXY		=	dispatchThreadId.xy + Radiosity.RegionXY;
	
	SHADOW_RESOURCES	shadowRc;
	shadowRc.ShadowSampler	=	ShadowSampler	; 
	shadowRc.LinearSampler	=	LinearSampler	;
	shadowRc.ShadowMap		=	ShadowMap		;
	shadowRc.ShadowMask		=	ShadowMask		;
	
	float4 	albedo			=	Albedo[ loadXY ].rgba;
	
	GroupMemoryBarrierWithGroupSync();
	
	//---------------------------------
	
	if (albedo.a>0)
	{
		float3	position		=	Position[ loadXY ].xyz;
		float3	normal			=	Normal	[ loadXY ].xyz * 2 - 1;
				normal			=	normalize( normal );
		float	size			=	Radiosity.ShadowFilter;
		float3	indirect		=	Radiance[ loadXY ].rgb;

		//	reconstruct basis ?
		float3  tangentX, tangentY;
		ReconstructBasis( normal, tangentX, tangentY );
		
		GEOMETRY	geometry;
		geometry.position	=	position;
		geometry.normal		=	normal;

		LIGHTING totalLight	=	(LIGHTING)0;
		SURFACE  surface	=	CreateDiffuseSurface( float3(1,1,1), normal );

		//-----------------------------
		// compute spot lights :
		//-----------------------------
		
		/*for (uint index=0; index<light_count; index++)
		{
			LIGHT light 		=	Lights[ light_indices[ index ] ];
			LIGHTING lighting	=	ComputePointLight( light, Camera, geometry, surface, shadowRc );
			
			AccumulateLighting( totalLight, lighting, 1 );
		}*/

		//-----------------------------
		// compute direct light :
		//-----------------------------
		
		if (1) 
		{
			float3 	dir	=	-normalize(DirectLight.DirectLightDirection.xyz);
			RAY 	ray = 	ConstructRay( position + normal * 0.01f, dir );
			
			bool shadow = RayTrace( ray, RtTriangles, RtBvhTree );
			
			LIGHTING lighting 	= (LIGHTING)0;
			lighting.diffuse	= shadow ? 0 : max( 0, dot(normal, dir) * DirectLight.DirectLightIntensity.rgb );
			//LIGHTING lighting	=	ComputeDirectLight( DirectLight, Camera, geometry, surface, CascadeShadow, shadowRc, loadXY );
			
			AccumulateLighting( totalLight, lighting, 1 );
		}
		
		totalLight.diffuse	+=	indirect;
		totalLight.diffuse	+=	LightEpsilon;
		
		RadianceUav[ storeXY.xy ]	=	float4(totalLight.diffuse * albedo.a, albedo.a );
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

float2 lerp_barycentric_coords( float2 a, float2 b, float2 c, float2 uv )
{
	float u = uv.x;
	float v = uv.y;
	float w = 1 - u - v;
	return a * u + b * v + c * w;
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
	
	float4	irradianceR	=	float4( NoLight, 0, 0, 0 );
	float4	irradianceG	=	float4( NoLight, 0, 0, 0 );
	float4	irradianceB	=	float4( NoLight, 0, 0, 0 );
	
	float3	lmNormal	=	Normal	[ loadXY ].xyz * 2 - 1;
			lmNormal	=	normalize(lmNormal);
	float3	lmPosition	=	Position[ loadXY ].xyz;// + lmNormal * 0.01;
	uint  num_samples	=	Radiosity.NumRays;
	float k = 1.0f / num_samples;
	
	float3	random_vector	=	hammersley_sphere_uniform( groupIndex, TileSize * TileSize );
	
	[loop]
	for (uint i=0; i<num_samples; i++)
	{
		float3	rayDir		=	normalize(hammersley_sphere_uniform( i, num_samples ));
				rayDir		=	reflect( rayDir, random_vector );
		
		if (dot(rayDir, lmNormal)>0.01)
		{
			RAY 	ray		=	ConstructRay( lmPosition, rayDir );
			bool	hit		=	RayTrace( ray, RtTriangles, RtBvhTree );
			float3	light	=	float3(0,0,0);
			
			if (hit)
			{
				uint 	triIndex	=	ray.index;
				float3	hitNormal	=	normalize(ray.norm);
				float2 	lmCoord0	=	RtLmVerts[ triIndex*3+0 ].LMCoord;
				float2 	lmCoord1	=	RtLmVerts[ triIndex*3+1 ].LMCoord;
				float2 	lmCoord2	=	RtLmVerts[ triIndex*3+2 ].LMCoord;
				float2	lmCoord		=	lerp_barycentric_coords( lmCoord0, lmCoord1, lmCoord2, ray.uv );
				float	nDotL		=	max( 0, -dot( hitNormal, rayDir ) );
				float3	albedo		=	Albedo.SampleLevel( LinearSampler, lmCoord, 0 ).rgb;
						albedo		=	lerp( albedo, 0.9f, Radiosity.WhiteAlbedo );
				light				=	nDotL * albedo * Radiance.SampleLevel( LinearSampler, lmCoord, 0 ).rgb;//*/
			}
			else
			{
				light		=	SkyBox.SampleLevel( LinearSampler, rayDir.xyz * float3(-1,1,1), 0 ).rgb;
			}

			irradianceR		+=	SHL1EvaluateDiffuse( k * light.r, rayDir );
			irradianceG		+=	SHL1EvaluateDiffuse( k * light.g, rayDir );
			irradianceB		+=	SHL1EvaluateDiffuse( k * light.b, rayDir );
		}
	}
	
	StoreLightmap( storeXY.xy, irradianceR, irradianceG, irradianceB );
}

#endif

/*------------------------------------------------------------------------------
	Integrate 3D-lightmap :
------------------------------------------------------------------------------*/

#ifdef INTEGRATE3

void StoreLightVolume( int3 xyz, float4 shR, float4 shG, float4 shB, float skyFactor )
{
	shR.xyz += LightEpsilon;
	LightVolumeL0[ xyz ]	=	float4( float3( shR.x		, shG.x			, shB.x		 	)				, 0 );
	LightVolumeL1[ xyz ]	=	float4( float3( shR.y/shR.x , shG.y/shG.x	, shB.y/shB.x	) * 0.5f + 0.5f	, skyFactor );
	LightVolumeL2[ xyz ]	=	float4( float3( shR.z/shR.x , shG.z/shG.x	, shB.z/shB.x	) * 0.5f + 0.5f	, 0 );
	LightVolumeL3[ xyz ]	=	float4( float3( shR.w/shR.x , shG.w/shG.x	, shB.w/shB.x	) * 0.5f + 0.5f	, 0 );
}

[numthreads(ClusterSize,ClusterSize,ClusterSize)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3	loadXYZ		=	dispatchThreadId.xyz;
	int3	storeXYZ	=	dispatchThreadId.xyz;

	float4	irradianceR	=	float4( NoLight, 0, 0, 0 );
	float4	irradianceG	=	float4( NoLight, 0, 0, 0 );
	float4	irradianceB	=	float4( NoLight, 0, 0, 0 );
	
	float3	lmPosition	=	mul( float4(storeXYZ, 1.0f), Radiosity.VoxelToWorld ).xyz;

	uint  num_samples	=	Radiosity.NumRays;
	float k = 1.0f / num_samples;
	
	float3	random_vector	=	hammersley_sphere_uniform( groupIndex, TileSize * TileSize );
	
	for (uint i=0; i<num_samples; i++)
	{
		float3	rayDir		=	hammersley_sphere_uniform( i, num_samples );
		
		if (true)
		{
			RAY 	ray		=	ConstructRay( lmPosition + rayDir, rayDir );
			bool	hit		=	RayTrace( ray, RtTriangles, RtBvhTree );
			float3	light	=	float3(0,0,0);
			
			if (hit)
			{
				uint 	triIndex	=	ray.index;
				float3	hitNormal	=	normalize(ray.norm);
				float2 	lmCoord0	=	RtLmVerts[ triIndex*3+0 ].LMCoord;
				float2 	lmCoord1	=	RtLmVerts[ triIndex*3+1 ].LMCoord;
				float2 	lmCoord2	=	RtLmVerts[ triIndex*3+2 ].LMCoord;
				float2	lmCoord		=	lerp_barycentric_coords( lmCoord0, lmCoord1, lmCoord2, ray.uv );
				float	nDotL		=	max( 0, -dot( hitNormal, rayDir ) );
				float3	albedo		=	Albedo.SampleLevel( LinearSampler, lmCoord, 0 ).rgb;
				light				=	nDotL * albedo * Radiance.SampleLevel( LinearSampler, lmCoord, 0 ).rgb;
			}
			else
			{
				light		=	SkyBox.SampleLevel( LinearSampler, rayDir.xyz * float3(-1,1,1), 0 ).rgb;
			}

			irradianceR		+=	SHL1EvaluateDiffuse( k * light.r, rayDir );
			irradianceG		+=	SHL1EvaluateDiffuse( k * light.g, rayDir );
			irradianceB		+=	SHL1EvaluateDiffuse( k * light.b, rayDir );
		}
	}

	StoreLightVolume( storeXYZ, irradianceR, irradianceG, irradianceB, 0.1f );
}

#endif














