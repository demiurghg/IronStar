#if 0
$ubershader 	COMPUTE|INTEGRATE
#endif

#include "auto/fog.fxi"
#include "fog_lighting.hlsl"


/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

static const float3 aaPattern[8] = 
{
	float3( 0.75f,  0.25f, 0.000f ),
	float3(-0.75f, -0.25f, 0.125f ),
	float3( 0.25f, -0.75f, 0.250f ),
	float3(-0.25f,  0.75f, 0.375f ),
	float3( 0.75f,  0.25f, 0.500f ),
	float3(-0.75f, -0.25f, 0.625f ),
	float3( 0.25f, -0.75f, 0.750f ),
	float3(-0.25f,  0.75f, 0.875f ),
};

[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	float4 emission = 0;

	uint3 location		=	dispatchThreadId.xyz;
	int3 blockSize		=	int3(BlockSizeX,BlockSizeY,BlockSizeZ);
	
	float value = (location.x + location.y + location.z)&1;
	
	float3	offset			=	0.5f;
	uint 	patternIdx		=	location.z % 4;
	float3	normLocation	=	(location.xyz + offset) / float3(FogSizeX, FogSizeY, FogSizeZ);
	
	float	tangentX		=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY		=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float	vsDistance		=	-log(1-normLocation.z)/FogGridExpScale;

	float4	vsPosition		=	float4( vsDistance * tangentX, vsDistance * tangentY, -vsDistance, 1 );
	
	float3	wsPosition		=	mul( vsPosition, Camera.ViewInverted ).xyz;
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	float	density			=	Fog.FogDensity * min(1, exp(-(wsPosition.y)/Fog.FogHeight/3));
	
	emission				+=	ComputeClusteredLighting( wsPosition, density );
	
	FogTarget[ location.xyz ] = emission;
	
	//FogTarget[ location.xyz ] = float4( frac(2*normLocation.xy), 0, 1 );
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
		float3	extinctionClamp			=	clamp( extinction, 0.0000001, 1 );
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


