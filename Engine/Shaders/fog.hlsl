#if 0
$ubershader COMPUTE
$ubershader INTEGRATE +SHOW_SLICE
#endif

#include "auto/fog.fxi"
#include "fog_lighting.hlsl"
#include "ls_fog.fxi"

#define GAME_UNIT 0.32f

/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

static const float3 aaPattern[8] = 
{
	float3(  1, -3, -7 ) / 8.0f,
	float3( -1,  3,  5 ) / 8.0f,
	float3(  5,  1, -3 ) / 8.0f,
	float3( -3, -5,  1 ) / 8.0f,
	float3( -5,  5, -1 ) / 8.0f,
	float3( -7, -1,  3 ) / 8.0f,
	float3(  3,  7, -5 ) / 8.0f,
	float3(  7, -7,  7 ) / 8.0f,
};


float3 GetWorldPosition( float3 gridLocation )
{
	float3	normLocation	=	gridLocation.xyz * Fog.FogSizeInv.xyz;
	
	float	tangentX		=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY		=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float	vsDistance		=	log(1-normLocation.z)/Fog.FogGridExpK;

	float4	vsPosition		=	float4( vsDistance * tangentX, vsDistance * tangentY, -vsDistance, 1 );
	float3	wsPosition		=	mul( vsPosition, Camera.ViewInverted ).xyz;
	
	return wsPosition;
}


float ClipHistory( float4 ppPosition )
{
	float3	deviceCoords	=	ppPosition.xyz / ppPosition.w;
	
	float	maxX			=	1.0f - 0.5f * Fog.FogSizeInv.x;
	float	maxY			=	1.0f - 0.5f * Fog.FogSizeInv.y;
	float	minZ			=	1.0f * Fog.FogSizeInv.z;
	
	if (abs(deviceCoords.x)>maxX || abs(deviceCoords.y)>maxY || deviceCoords.z>1 || deviceCoords.z<=0.1f )
	{
		return 0;
	}
	else
	{
		return 1;
	}
}


float4 GetFogHistory( float3 wsPosition, out float factor )
{
	float4 	ppPosition	=	mul( float4(wsPosition,1), Camera.ReprojectionMatrix );
	float4	fogData		=	SampleVolumetricFog( Fog, ppPosition, LinearClamp, FogHistory );
	
	factor = ClipHistory( ppPosition ) * Fog.HistoryFactor;
	
	return fogData;
}


[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	float4 emission = 0;

	uint3 	location		=	dispatchThreadId.xyz;

	float	historyFactor	=	1;
	float3	wsPositionNJ	=	GetWorldPosition( location.xyz + float3(0.5,0.5,0.5) );
	float4	fogHistory		=	GetFogHistory( wsPositionNJ, historyFactor );
	
	float3	offset			=	aaPattern[ Fog.FrameCount % 8 ] * 1 * float3(0.25f,0.25f,0.5f) + float3(0.5,0.5,0.5);
	float3 	wsPosition		=	GetWorldPosition( location.xyz + offset );
	
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	float	fadeout			=	pow( saturate( 1 - location.z * Fog.FogSizeInv.z ), 4 );
	float	density			=	fadeout * Fog.FogDensity * min(1, exp(-(wsPositionNJ.y*GAME_UNIT)/Fog.FogHeight));
	
	emission				+=	ComputeClusteredLighting( wsPosition, density );
	
	//	to prevent Nan-history :
	FogTarget[ location.xyz ] = historyFactor==0 ? emission : lerp( emission, fogHistory, historyFactor );
}

#endif

/*-----------------------------------------------------------------------------
	Pre-intagrate flux and fog :
-----------------------------------------------------------------------------*/

#ifdef INTEGRATE

float GetCellLengthScale( uint2 location )
{
	float2	normLocation	=	location.xy * Fog.FogSizeInv.xy;
	
	float	tangentX	=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY	=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float4	vsRay		=	float4( tangentX, tangentY, -1, 0 );
	
	return	length( vsRay );
}


[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	uint2 	location		=	dispatchThreadId.xy;
	float	invDepthSlices	=	Fog.FogSizeInv.z;
	
	float3	accumScattering		=	float3(0,0,0);
	float	accumTransmittance	=	1;
	float3	skyFogScattering	=	float3(0,0,0);
	float	skyFogTransmittance	=	1;
	
	for ( uint slice=0; slice<Fog.FogSizeZ; slice++ ) 
	{
		//	Compute texture coords :
		uint3	loadXYZ					=	uint3( location.xy, slice );
		uint3	storeXYZ				=	uint3( location.xy, slice );
		float3	loadUVW					=	loadXYZ * Fog.FogSizeInv.xyz;

		//	Sample AP LUT :
		float	apWeight				=	saturate( ((float)slice - Fog.FogSizeZ/4) * 3 * invDepthSlices );
		float4	aerialPerspective		=	LutAP.SampleLevel( LinearClamp, loadUVW, 0 );
		
		//	Sample FOG grid :
		float	frontDistance			=	log( 1 - (slice+0.0000f) * invDepthSlices ) / Fog.FogGridExpK;
		float	backDistance			=	log( 1 - (slice+0.9999f) * invDepthSlices ) / Fog.FogGridExpK;
		float	stepLength				=	abs(backDistance - frontDistance) * GetCellLengthScale( location ) * Fog.FogScale * GAME_UNIT;
		
		float4 	scatteringExtinction	=	FogSource[ loadXYZ ];
		
		//	Compute integral segment :
		float	extinction				=	scatteringExtinction.a * stepLength;
		float	extinctionClamp			=	clamp( extinction, 0.000001, 1 );
		float	transmittance			=	exp( -extinction );
		float3	scattering				=	scatteringExtinction.rgb * stepLength * (1 - apWeight);
		float3	integScatt				=	( scattering - scattering * transmittance ) / extinctionClamp;
		
		//	Integrate FOG with AP :
		accumScattering					+=	accumTransmittance * integScatt * aerialPerspective.a;
		accumTransmittance				*=	transmittance;

		//	Integrate FOG without AP :
		skyFogScattering				+=	accumTransmittance * integScatt;
		skyFogTransmittance				*=	transmittance;
		
		//	Store scattering and inv transmittance :
		float4	integratedFog			=	float4( aerialPerspective.rgb * apWeight, 1 - apWeight * (1 - aerialPerspective.a) );
		
				//	Apply FOG over AP :
				integratedFog.rgb		*=	skyFogTransmittance;
				integratedFog.rgb		+=	skyFogScattering;
				
		FogTarget[ storeXYZ ]			=	integratedFog;
		
		#ifdef SHOW_SLICE
			float4 	areaFog	=	float4(4,4,0, slice % 2);
			float4 	areaAP	=	float4(0,0,8, slice % 2);
			FogTarget[ int3( location.xy, slice ) ]	=	lerp( areaFog, areaAP, apWeight );
			if (slice>=Fog.FogSizeZ-6) FogTarget[ int3( location.xy, slice ) ]	=	float4(8,0,0,1);
		#endif
	}
	
	// 	integrated FOG for sky only, because sky already has aerial perspective itself :
	SkyFogLut[ location.xy ] 	=	float4( skyFogScattering.rgb, skyFogTransmittance );
}

#endif

