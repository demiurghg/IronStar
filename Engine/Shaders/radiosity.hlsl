
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

// small addition to tell lit and unlit areas
static const float3 LightEpsilon = float3( 0.001f, 0.001f, 0.001f );

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
	
	//	we use at least non-zero lighting value to collapse patches
	float	 factor		=	all(float4(lighting00.r, lighting01.r, lighting10.r, lighting11.r));

	RadianceUav[ storeXY.xy ]	=	float4(lighting) * factor;
}

#endif


/*------------------------------------------------------------------------------
	Gather light from all visible patches :
------------------------------------------------------------------------------*/

void StoreLightmap( int2 xy, float4 shR, float4 shG, float4 shB )
{
	IrradianceL0[ xy ]	=	float4( shR.x		, shG.x			, shB.x			, 0 );
	IrradianceL1[ xy ]	=	float4( shR.y/shR.x , shG.y/shG.x	, shB.y/shB.x	, 0 ) * 0.5f + 0.5f;
	IrradianceL2[ xy ]	=	float4( shR.z/shR.x , shG.z/shG.x	, shB.z/shB.x	, 0 ) * 0.5f + 0.5f;
	IrradianceL3[ xy ]	=	float4( shR.w/shR.x , shG.w/shG.x	, shB.w/shB.x	, 0 ) * 0.5f + 0.5f;
}

static const float3 dir_lut[64] = {
	float3(  0.00f,  1.00f,  0.00f ),	float3( -0.25f,  0.97f,  0.00f ),
	float3(  0.00f,  0.94f,  0.35f ),	float3(  0.00f,  0.91f, -0.42f ),
	float3(  0.34f,  0.88f,  0.34f ),	float3( -0.38f,  0.84f, -0.38f ),
	float3( -0.41f,  0.81f,  0.41f ),	float3(  0.44f,  0.78f, -0.44f ),
	float3(  0.61f,  0.75f,  0.25f ),	float3( -0.64f,  0.72f, -0.27f ),
	float3( -0.28f,  0.69f,  0.67f ),	float3(  0.29f,  0.66f, -0.70f ),
	float3(  0.30f,  0.63f,  0.72f ),	float3( -0.31f,  0.59f, -0.74f ),
	float3( -0.76f,  0.56f,  0.32f ),	float3(  0.78f,  0.53f, -0.32f ),
	float3(  0.85f,  0.50f,  0.17f ),	float3( -0.87f,  0.47f, -0.17f ),
	float3( -0.18f,  0.44f,  0.88f ),	float3(  0.18f,  0.41f, -0.90f ),
	float3(  0.52f,  0.38f,  0.77f ),	float3( -0.52f,  0.34f, -0.78f ),
	float3( -0.79f,  0.31f,  0.53f ),	float3(  0.80f,  0.28f, -0.53f ),
	float3(  0.81f,  0.25f,  0.54f ),	float3( -0.81f,  0.22f, -0.54f ),
	float3( -0.55f,  0.19f,  0.82f ),	float3(  0.55f,  0.16f, -0.82f ),
	float3(  0.19f,  0.13f,  0.97f ),	float3( -0.19f,  0.09f, -0.98f ),
	float3( -0.98f,  0.06f,  0.19f ),	float3(  0.98f,  0.03f, -0.19f ),
	float3(  1.00f,  0.00f,  0.10f ),	float3( -0.99f, -0.03f, -0.10f ),
	float3( -0.10f, -0.06f,  0.99f ),	float3(  0.10f, -0.09f, -0.99f ),
	float3(  0.63f, -0.13f,  0.77f ),	float3( -0.63f, -0.16f, -0.76f ),
	float3( -0.76f, -0.19f,  0.62f ),	float3(  0.75f, -0.22f, -0.62f ),
	float3(  0.85f, -0.25f,  0.46f ),	float3( -0.85f, -0.28f, -0.45f ),
	float3( -0.45f, -0.31f,  0.84f ),	float3(  0.44f, -0.34f, -0.83f ),
	float3(  0.27f, -0.38f,  0.89f ),	float3( -0.27f, -0.41f, -0.87f ),
	float3( -0.86f, -0.44f,  0.26f ),	float3(  0.85f, -0.47f, -0.26f ),
	float3(  0.83f, -0.50f,  0.25f ),	float3( -0.81f, -0.53f, -0.25f ),
	float3( -0.24f, -0.56f,  0.79f ),	float3(  0.23f, -0.59f, -0.77f ),
	float3(  0.37f, -0.63f,  0.69f ),	float3( -0.36f, -0.66f, -0.67f ),
	float3( -0.64f, -0.69f,  0.34f ),	float3(  0.61f, -0.72f, -0.33f ),
	float3(  0.51f, -0.75f,  0.42f ),	float3( -0.48f, -0.78f, -0.40f ),
	float3( -0.37f, -0.81f,  0.45f ),	float3(  0.34f, -0.84f, -0.41f ),
	float3(  0.05f, -0.88f,  0.48f ),	float3( -0.04f, -0.91f, -0.42f ),
	float3( -0.35f, -0.94f,  0.03f ),	float3(  0.25f, -0.97f, -0.02f ),
};

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

#ifdef INTEGRATE

groupshared float3 radiance_cache[2048];

uint uintDivUp( uint a, uint b ) { return (a % b != 0) ? (a / b + 1) : (a / b); }

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	//	upload cache
	uint 	cacheIndex	=	Tiles[ groupId.xy ].x;
	uint 	cacheCount	=	Tiles[ groupId.xy ].y;
	uint	stride		=	BlockSizeX * BlockSizeY;
	
	GroupMemoryBarrierWithGroupSync();

	for (uint base=0; base<2048; base+=stride)
	{
		uint offset = groupThreadId.x + groupThreadId.y * BlockSizeX;
		uint addr   = cacheIndex + base + offset;
		
		if (base+offset < cacheCount)
		{
			uint 	lmAddr		=	Cache[ addr ];
			uint 	lmX			=	(lmAddr >> 20) & 0xFFF;
			uint 	lmY			=	(lmAddr >>  8) & 0xFFF;
			uint 	lmMip		=	(lmAddr >>  5) & 0x007;
			int3	loadUVm		=	int3( lmX, lmY, lmMip );
			float3 	radiance	=	Radiance.Load( loadUVm ).rgb;
			radiance_cache[ base+offset ] = radiance;
		}
		else
		{
			radiance_cache[ base+offset ] = float3(0,0,10000);
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
	
	for (uint index=begin; index<end; index++)
	{
		uint 	lmAddr		=	Indices[ index ];
		uint 	cacheIndex	=	(lmAddr >> 12) & 0xFFF;
		uint 	direction	=	(lmAddr >>  6) & 0x03F;
		uint 	hitCount	=	(lmAddr >>  0) & 0x03F;
		
		float3 	radiance	=	radiance_cache[ cacheIndex ];
		float3 	lightDirN	=	DecodeDirection( direction );

		float3	light		=	radiance.rgb / 256.0f * hitCount * Radiosity.IndirectFactor;	
		
		irradianceR			+=	SHL1EvaluateDiffuse( light.r, lightDirN );
		irradianceG			+=	SHL1EvaluateDiffuse( light.g, lightDirN );
		irradianceB			+=	SHL1EvaluateDiffuse( light.b, lightDirN );
	}//*/
	
	StoreLightmap( storeXY.xy, irradianceR, irradianceG, irradianceB );
}

#endif

