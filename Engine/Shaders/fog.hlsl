#if 0
$ubershader COMPUTE
$ubershader INTEGRATE +SHOW_SLICE
#endif

#include "auto/fog.fxi"
#include "fog_lighting.hlsl"
#include "ls_fog.fxi"

#define GAME_UNIT 0.32f


/*-----------------------------------------------------------------------------
	Stuff
-----------------------------------------------------------------------------*/

//
//	Compute length of the unit cell along view ray
//
float GetCellLength( uint3 location )
{
	float	slice			=	location.z;
	float	invDepthSlices	=	Fog.FogSizeInv.z;
	float	frontDistance	=	log( 1 - (slice+0.0000f) * invDepthSlices ) / Fog.FogGridExpK;
	float	backDistance	=	log( 1 - (slice+0.9999f) * invDepthSlices ) / Fog.FogGridExpK;
	
	//	FogScale affects only atmospheric lighting and should not be affected by local lights.
	//	Since atmospheric light is computed in Sky2 module, simply skip it here.
	float	cellHeight		=	abs(backDistance - frontDistance) /* Fog.FogScale*/ * GAME_UNIT;

	float2	normLocation	=	location.xy * Fog.FogSizeInv.xy;
	
	float	tangentX	=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY	=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float4	vsRay		=	float4( tangentX, tangentY, -1, 0 );
	
	return	length( vsRay ) * cellHeight;
}


float3 GetWorldPosition( float3 gridLocation )
{
	float3	normLocation	=	( gridLocation.xyz + float3(0.5,0.5,0.5) ) * Fog.FogSizeInv.xyz;
			normLocation.z	=	clamp( normLocation.z, Fog.FogSizeInv.z * 0.5f, 1 - Fog.FogSizeInv.z * 0.5f);
	
	float	tangentX		=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY		=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float	vsDistance		=	log(1-normLocation.z)/Fog.FogGridExpK;

	float4	vsPosition		=	float4( vsDistance * tangentX, vsDistance * tangentY, -vsDistance, 1 );
	float3	wsPosition		=	mul( vsPosition, Camera.ViewInverted ).xyz;
	
	return wsPosition;
}

float GetAtmosphericFogDensity ( float3 worldPos )
{
	return Fog.FogDensity * min( 1, exp(-(worldPos.y * GAME_UNIT)/Fog.FogHeight ) );
}


float GetAPBlendFactor( uint slice )
{
	return smoothstep( 64,128, slice );
}

/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

static const float HISTORY_FACTOR_SHADOW	=	0.97f;
static const float HISTORY_FACTOR_FOG		=	0.99f;

static const float2 aa8[8] = 
{
	float2(  1, -3 ) / 8.0f,
	float2( -1,  3 ) / 8.0f,
	float2(  5,  1 ) / 8.0f,
	float2( -3, -5 ) / 8.0f,
	float2( -5,  5 ) / 8.0f,
	float2( -7, -1 ) / 8.0f,
	float2(  3,  7 ) / 8.0f,
	float2(  7, -7 ) / 8.0f,
};

static const float2 aa4[4] = 
{
	float2( -2, -6 ) / 8.0f,
	float2(  6, -2 ) / 8.0f,
	float2( -6,  2 ) / 8.0f,
	float2(  2,  6 ) / 8.0f,
};

static const float2 aa5[5] = 
{
	float2(  0,  0 ) / 8.0f,
	float2( -2, -6 ) / 8.0f,
	float2(  6, -2 ) / 8.0f,
	float2( -6,  2 ) / 8.0f,
	float2(  2,  6 ) / 8.0f,
};

static const uint bayer[4][4] = 
{
	 0,  8,  2, 10,	12,  4, 14, 16,  6,  3,  1,  9, 15,  7, 13, 5,
};


float ClipHistory( float4 ppPosition, float4 ppPosition2 )
{
	float3	deviceCoords	=	ppPosition.xyz  / ppPosition.w;
	float3	deviceCoords2	=	ppPosition2.xyz / ppPosition2.w;
	
	float 	falloff			=	1 - 0.5*saturate(3*distance(deviceCoords, deviceCoords2));
	
	float	maxX			=	1.0f - 0.5f * Fog.FogSizeInv.x;
	float	maxY			=	1.0f - 0.5f * Fog.FogSizeInv.y;
	float	minZ			=	1.0f * Fog.FogSizeInv.z;
	//float	maxZ			=	1.0f - 0.5f * Fog.FogSizeInv.z;
	float	maxZ			=	0.9999f;
	
	if (abs(deviceCoords.x)>maxX || abs(deviceCoords.y)>maxY || deviceCoords.z>maxZ || deviceCoords.z<=0.1f )
	{
		return 0;
	}
	else
	{
		return falloff;
	}
}


float4 GetFogHistory( float3 wsPosition, out float factor )
{
	float4 	ppPosition	=	mul( float4(wsPosition,1), Camera.ReprojectionMatrix );
	float4 	ppPosition2	=	mul( float4(wsPosition,1), Camera.ViewProjection );
	float4	fogData		=	SampleVolumetricFog( Fog, ppPosition, LinearClamp, FogHistory );
	
	factor = ClipHistory( ppPosition, ppPosition2 ) * HISTORY_FACTOR_FOG;
	
	return fogData;
}


float2 GetShadowHistory( float3 wsPosition, out float factor )
{
	float4 	ppPosition	=	mul( float4(wsPosition,1), Camera.ReprojectionMatrix );
	float4 	ppPosition2	=	mul( float4(wsPosition,1), Camera.ViewProjection );
	float4	fogData		=	SampleVolumetricFog( Fog, ppPosition, LinearClamp, FogShadowHistory );
	
	factor = ClipHistory( ppPosition, ppPosition2 ) * HISTORY_FACTOR_SHADOW;
	
	return saturate(fogData.xy);
}


[numthreads(16,16,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint3 	location		=	dispatchThreadId.xyz;

	float	historyFactorFog	=	0;
	float	historyFactorShadow	=	0;
	float3	wsPositionNJ	=	GetWorldPosition( location.xyz );
	float4	fogHistory		=	GetFogHistory( wsPositionNJ, historyFactorFog );
	float2	shadowHistory	=	GetShadowHistory( wsPositionNJ, historyFactorShadow );
	
	//float3	offset			=	float3(0,0,0.75f);
	//float3	offset			=	aaPattern[ Fog.FrameCount % 8 ] * 1 * float3(0.5f,0.5f,0.5f);	
	float	offsetZ			=	( (bayer[location.x&3][location.y&3] + Fog.FrameCount*11) & 0xF ) / 16.0f;
	float2	offsetXY		=	1.0f*aa5[Fog.FrameCount % 5];
	float3	offset			=	float3(offsetXY, offsetZ);
	float3 	wsPosition		=	GetWorldPosition( location.xyz + offset );
	
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	//	Compute fog density :
	float	fadeout			=	pow( saturate( 1 - location.z * Fog.FogSizeInv.z ), 4 );
	float	density			=	GetAtmosphericFogDensity( wsPositionNJ );
	
	//	Compute phase function of incoming light :
	float	apWeight	=	GetAPBlendFactor( location.z );
	float3	localLight	=	ComputeClusteredLighting( wsPosition ).rgb * density;
	float3	phaseLight	=	localLight;
	
	//	Compute length of the cell :
	float	stepLength		=	GetCellLength( location.xyz );

	//	Compute integral segment :
	float	extinction		=	density * stepLength;
	float	extinctionClamp	=	clamp( extinction, 0.000001, 1 );
	float	transmittance	=	exp( -extinction );
	float3	scattering		=	phaseLight * stepLength;
	float3	integScatt		=	( scattering - scattering * transmittance ) / extinctionClamp;
	
	float4	fogST			=	float4( integScatt, 1 /* transmittance w/o atmospheric fog */ );
	
	//	to prevent NaN-history :
	float4 factor			=	float4( historyFactorFog, historyFactorFog, historyFactorFog, 0 );
	FogTarget[ location.xyz ] = historyFactorFog==0 ? fogST : lerp( fogST, fogHistory, factor );
	
	//	Compute sky shadow and ambient occlusion :
#if 0
	float2 skyShadow = 0;
	for (int i=0; i<4; i++)
	{
		wsPosition = GetWorldPosition( location.xyz + offset + float3(0,0,0.25f*i) );
		skyShadow += 0.25f*ComputeSkyShadow( wsPosition );
	}
#else
	float2 	skyShadow	=	ComputeSkyShadow( wsPosition );
#endif
			
	skyShadow	=	historyFactorShadow==0 ? skyShadow : lerp( skyShadow, shadowHistory, historyFactorShadow );
			
	FogShadowTarget[ location.xyz ] = float4( skyShadow, 0, 0 );
}

#endif

/*-----------------------------------------------------------------------------
	Pre-intagrate flux and fog :
-----------------------------------------------------------------------------*/

#ifdef INTEGRATE

[numthreads(8,8,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint2 	location		=	dispatchThreadId.xy;
	float	invDepthSlices	=	Fog.FogSizeInv.z;
	
	float3	accumFogScattering		=	float3(0,0,0);
	float	accumFogTransmittance	=	1;
	float3	accumSkyScattering		=	float3(0,0,0);
	float	accumSkyTransmittance	=	1;
	
	bool 	applyFog = location.x > 64;
	float3	apST0prev = 0;
	float3	apST1prev = 0;

	//	Zero slice is always transparent :
	FogTarget[ int3( location.xy, 0 ) ]	=	float4(0,0,0,1);
	
	//	Integrate all other slices :
	for ( uint slice=1; slice<Fog.FogSizeZ; slice++ ) 
	{
		//	Compute texture coords :
		uint3	loadXYZ			=	uint3( location.xy, slice );
		uint3	storeXYZ		=	uint3( location.xy, slice );
		float3	loadUVW			=	(loadXYZ + float3(0.5,0.5,0.5)) * Fog.FogSizeInv.xyz;
		//	...apply texel distribution (see sky2.hlsl):
		loadUVW.z = pow(abs(loadUVW.z), 1.0f / 1.5f);

		//	Sample AP LUT :
		float2	skyShadow		=	FogShadowSource[ loadXYZ ].rg;
		
		float	apWeight		=	GetAPBlendFactor( slice );
		float	fogWeight		=	1 - apWeight;
		float4	apST0			=	LutAP0.SampleLevel( LinearClamp, loadUVW, 0 );
		float3	spST0delta		=	apST0.rgb - apST0prev;
				apST0prev		=	apST0.rgb;
		float4	apST1			=	LutAP1.SampleLevel( LinearClamp, loadUVW, 0 );
		float3	spST1delta		=	apST1.rgb - apST1prev;
				apST1prev		=	apST1.rgb;
				
		float3	spSTdelta		=	spST0delta * skyShadow.r + spST1delta * skyShadow.g;
				
		//	Sample FOG grid :
		float4 	fogST			=	FogSource[ loadXYZ ];
		
		//	Integrate FOG with AP :
		accumFogScattering		+=	spSTdelta + fogST.rgb * accumFogTransmittance;
		accumFogTransmittance	*=	fogST.a;
		
		accumSkyScattering		+=	fogST.rgb * accumFogTransmittance;
		accumSkyTransmittance	*=	fogST.a;
		
		//	Store fog in LUT :
		FogTarget[ storeXYZ ]	=	float4( accumFogScattering, accumFogTransmittance * apST0.a );
		
		#ifdef SHOW_SLICE
			float4 	areaFog	=	float4(4,4,0, slice % 2);
			float4 	areaAP	=	float4(0,0,8, slice % 2);
			FogTarget[ int3( location.xy, slice ) ]	=	lerp( areaFog, areaAP, apWeight );
			if (slice>=Fog.FogSizeZ-6) FogTarget[ int3( location.xy, slice ) ]	=	float4(8,0,0,1);
		#endif
	}
	
	// 	integrated FOG for sky only, 
	//	because sky already has aerial perspective itself :
	float4	apST			=	LutAP0.SampleLevel( LinearClamp, float3( location.xy * Fog.FogSizeInv.xy, 1), 0 );
	
	SkyFogLut[ location.xy ] 	=	float4( accumSkyScattering, accumSkyTransmittance );
}

#endif

