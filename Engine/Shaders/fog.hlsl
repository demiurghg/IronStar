#if 0
$ubershader 	COMPUTE|INTEGRATE
#endif

#include "auto/fog.fxi"
#include "fog_lighting.hlsl"
#include "ls_fog.fxi"


/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

static const float3 aaPattern[16] = 
{
	float3( 0.75f,  0.25f, -3.5f / 8.0f ),
	float3(-0.75f, -0.25f,  2.5f / 8.0f ),
	float3( 0.25f, -0.75f, -1.5f / 8.0f ),
	float3(-0.25f,  0.75f,  0.5f / 8.0f ),
	float3( 0.75f,  0.25f, -0.5f / 8.0f ),
	float3(-0.75f, -0.25f,  1.5f / 8.0f ),
	float3( 0.25f, -0.75f, -2.5f / 8.0f ),
	float3(-0.25f,  0.75f,  3.5f / 8.0f ),
	
	float3( 0.75f,  0.25f, -3.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3(-0.75f, -0.25f,  2.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3( 0.25f, -0.75f, -1.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3(-0.25f,  0.75f,  0.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3( 0.75f,  0.25f, -0.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3(-0.75f, -0.25f,  1.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3( 0.25f, -0.75f, -2.5f / 8.0f ) * float3(0.7,0.7,-1),
	float3(-0.25f,  0.75f,  3.5f / 8.0f ) * float3(0.7,0.7,-1),
};


float3 GetWorldPosition( float3 gridLocation )
{
	float3	normLocation	=	gridLocation.xyz / float3(FogSizeX, FogSizeY, FogSizeZ);
	
	float	tangentX		=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY		=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float	vsDistance		=	-log(1-normLocation.z)/FogGridExpScale;

	float4	vsPosition		=	float4( vsDistance * tangentX, vsDistance * tangentY, -vsDistance, 1 );
	float3	wsPosition		=	mul( vsPosition, Camera.ViewInverted ).xyz;
	
	return wsPosition;
}


float ClipHistory( float4 ppPosition )
{
	float3	deviceCoords	=	ppPosition.xyz / ppPosition.w;
	
	float	maxX			=	1.0f - 0.5f / FogSizeX;
	float	maxY			=	1.0f - 0.5f / FogSizeY;
	float	minZ			=	1.0f / FogSizeZ;
	
	if (abs(deviceCoords.x)>maxX || abs(deviceCoords.y)>maxY || deviceCoords.z>1 || deviceCoords.z<=minZ)
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
	float4	fogData		=	SampleVolumetricFog( ppPosition, LinearClamp, FogHistory );
	
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
	float3	wsPositionPrev	=	GetWorldPosition( location.xyz + float3(0.5,0.5,0.5) );
	float4	fogHistory		=	GetFogHistory( wsPositionPrev, historyFactor );
	
	float3	offset			=	aaPattern[ Fog.FrameCount % 8 ] * float3(0.5f,0.5f,1.0f) + float3(0.5,0.5,0.5);
	float3 	wsPosition		=	GetWorldPosition( location.xyz + offset );
	
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	float	density			=	Fog.FogDensity * min(1, exp(-(wsPosition.y)/Fog.FogHeight/3));
	
	emission				+=	ComputeClusteredLighting( wsPosition, density );
	
	//	to prevent Nan-history :
	FogTarget[ location.xyz ] = historyFactor==0 ? emission : lerp( emission, fogHistory, historyFactor );
}

#endif

/*-----------------------------------------------------------------------------
	Pre-intagrate flux and fog :
-----------------------------------------------------------------------------*/

#ifdef INTEGRATE

[numthreads(BlockSizeX,BlockSizeY,1)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int2 	location		=	dispatchThreadId.xy;
	float	invDepthSlices	=	1.0f / FogGridDepth;
	
	float3	accumScattering		=	float3(0,0,0);
	float	accumTransmittance	=	1;
	
	for ( int slice=0; slice<FogGridDepth; slice++ ) {
		
		float	frontDistance			=	- log( 1 - (slice+0.0000f) * invDepthSlices ) / FogGridExpScale;
		float	backDistance			=	- log( 1 - (slice+0.9999f) * invDepthSlices ) / FogGridExpScale;
		float	stepLength				=	abs(backDistance - frontDistance);
		
		float4 	scatteringExtinction	=	FogSource[ int3( location.xy, slice ) ];
		
		float3	extinction				=	scatteringExtinction.a * stepLength;
		float3	extinctionClamp			=	clamp( extinction, 0.000001, 1 );
		float	transmittance			=	exp( -extinction );
		
		float3	scattering				=	scatteringExtinction.rgb * stepLength;
		
		float3	integScatt				=	( scattering - scattering * transmittance ) / extinctionClamp;
		accumScattering					+=	accumTransmittance * integScatt;
		accumTransmittance				*=	transmittance;
		
		float4	storedValue		=	float4( accumScattering.rgb, 1 - accumTransmittance );
		FogTarget[ int3( location.xy, slice ) ]	=	storedValue;
		
		//FogTarget[ int3( location.xy, slice ) ]	=	float4( scatteringExtinction.rgb * 1000, 1 );
	}
}

#endif


