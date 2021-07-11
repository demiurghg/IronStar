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


float GetGroundFogDensity ( float3 worldPos )
{
	return Fog.GroundFogDensity * min( 1, exp(-((worldPos.y - Fog.GroundFogLevel) * GAME_UNIT)/Fog.GroundFogHeight ) );
}


float GetAPBlendFactor( uint slice )
{
	return smoothstep( Fog.FogSizeZ - 5, Fog.FogSizeZ - 1, slice );
}

/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

static const float HISTORY_FACTOR_FOG		= 0.99f;

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
	float3	deviceCoords		=	ppPosition.xyz  / ppPosition.w;
	float3	deviceCoords2		=	ppPosition2.xyz / ppPosition2.w;
	float3	falloffAxialScale	=	float3(1, 1, 1);
	
	float 	falloff			=	1 - saturate(1.0f*distance(deviceCoords * falloffAxialScale, deviceCoords2 * falloffAxialScale));
	
	float	maxX			=	1.0f - 0.5f * Fog.FogSizeInv.x;
	float	maxY			=	1.0f - 0.5f * Fog.FogSizeInv.y;
	float	minZ			=	1.0f * Fog.FogSizeInv.z;
	//float	maxZ			=	1.0f - 0.5f * Fog.FogSizeInv.z;
	float	maxZ			=	0.9999f;
	
	if (abs(deviceCoords.x)>maxX || abs(deviceCoords.y)>maxY || deviceCoords.z>maxZ || deviceCoords.z<=0.1f )
	{
		return falloff * 0.9;
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
	
	float4 fogData	=	float4(0,0,0,0);
	
	if (1)
	{
		fogData		=	SampleVolumetricFog( Fog, ppPosition, LinearClamp, FogHistory );
	}
	else
	{
		fogData	+=	0.25 * SampleVolumetricFog( Fog, lerp(ppPosition, ppPosition2, 0.25f), LinearClamp, FogHistory );
		fogData	+=	0.25 * SampleVolumetricFog( Fog, lerp(ppPosition, ppPosition2, 0.50f), LinearClamp, FogHistory );
		fogData	+=	0.25 * SampleVolumetricFog( Fog, lerp(ppPosition, ppPosition2, 0.75f), LinearClamp, FogHistory );
		fogData	+=	0.25 * SampleVolumetricFog( Fog, lerp(ppPosition, ppPosition2, 1.00f), LinearClamp, FogHistory );
	}
	
	factor = ClipHistory( ppPosition, ppPosition2 ) * HISTORY_FACTOR_FOG;
	
	return fogData;
}


[numthreads(4,4,4)] 
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
 	
	//float3	offset			=	float3(0,0,0.75f);
	//float3	offset			=	aaPattern[ Fog.FrameCount % 8 ] * 1 * float3(0.5f,0.5f,0.5f);	
	float	offsetZ			=	( (bayer[location.x&3][location.y&3] + Fog.FrameCount*1 + location.z*0) & 0xF ) / 16.0f;
	float2	offsetXY		=	aa5[Fog.FrameCount % 5];
	float3	offset			=	float3(offsetXY, offsetZ);
	float3 	wsPosition		=	GetWorldPosition( location.xyz + offset );
	
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	//	Compute fog density :
	float	fadeout			=	pow( saturate( 1 - location.z * (Fog.FogSizeInv.z) * 1.1f ), 0.5f );
	float	apFactor		=	GetAPBlendFactor( location.z );
	float	densityAP		=	GetAtmosphericFogDensity( wsPositionNJ ) * (1-apFactor);
	float	densityGF		=	GetGroundFogDensity( wsPositionNJ ) * (1-apFactor);
	float3	color			=	Fog.FogColor.rgb * densityAP + Fog.GroundFogColor.rgb * densityGF;
	float3 	glow			=	0;
	float	density			=	densityAP + densityGF;

	//	Ground fog :
	
	//	Compute phase function of incoming light :
	float3	localLight	=	ComputeClusteredLighting( wsPosition ).rgb * color;
	float3	phaseLight	=	localLight + glow * density;
	
	//	Compute length of the cell :
	float	stepLength		=	GetCellLength( location.xyz );

	//	Compute integral segment :
	float	extinction		=	density * stepLength;
	float	extinctionClamp	=	clamp( extinction, 0.000001, 1 );
	float	transmittance	=	exp( -extinction );
	float3	scattering		=	phaseLight * stepLength;
	float3	integScatt		=	( scattering - scattering * transmittance ) / extinctionClamp;
	
	float4	fogST			=	float4( integScatt, transmittance*1 /* transmittance w/o atmospheric fog */ );
	
	//	to prevent NaN-history :
	float4 factor			=	float4( historyFactorFog, historyFactorFog, historyFactorFog, 0 );
	FogTarget[ location.xyz ] = historyFactorFog==0 ? fogST : lerp( fogST, fogHistory, factor );
	//FogTarget[ location.xyz ] = float4(float3(1,1,1)/100.0f, 0.99f);
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
		float	apWeight		=	GetAPBlendFactor( slice );
		float	fogWeight		=	1 - apWeight;
		float4	apST			=	LutAP.SampleLevel( LinearClamp, loadUVW, 0 );
				apST.rgb		*=	apWeight;
				
		//	Sample FOG grid :
		float4 	fogST			=	FogSource[ loadXYZ ];
		
		//	Integrate FOG with AP :
		accumFogScattering		+=	fogST.rgb * accumFogTransmittance;
		accumFogTransmittance	*=	fogST.a;
		
		//	Store fog in LUT :
		FogTarget[ storeXYZ ]	=	float4( accumFogScattering + apST.rgb * accumFogTransmittance, accumFogTransmittance * apST.a );
		
		#ifdef SHOW_SLICE
			if ((slice%2)==1)
			{
				FogTarget[ int3( location.xy, slice ) ]	=	(slice > Fog.FogSizeZ/2) ? float4(1,1,0,1) : float4(1,0,0,1);
			}
			if (slice>=Fog.FogSizeZ-1) FogTarget[ int3( location.xy, slice ) ]	=	float4(8,0,0,1);
		#endif
	}

	// 	integrated FOG for sky only, 
	//	because sky already has aerial perspective itself :
	SkyFogLut[ location.xy ] 	=	float4( accumFogScattering, accumFogTransmittance );
}

#endif

