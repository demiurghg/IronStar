#if 0
$ubershader 	COMPUTE|INTEGRATE
#endif

#include "auto/fog.fxi"
#include "fog_lighting.hlsl"


/*-----------------------------------------------------------------------------
	Compute flux through the fog :
-----------------------------------------------------------------------------*/

#ifdef COMPUTE

[numthreads(BlockSizeX,BlockSizeY,BlockSizeZ)] 
void CSMain( 
	uint3 groupId : SV_GroupID, 
	uint3 groupThreadId : SV_GroupThreadID, 
	uint  groupIndex: SV_GroupIndex, 
	uint3 dispatchThreadId : SV_DispatchThreadID) 
{
	int3 location		=	dispatchThreadId.xyz;
	int3 blockSize		=	int3(BlockSizeX,BlockSizeY,BlockSizeZ);
	
	float value = (location.x + location.y + location.z)&1;
	
	float3	normLocation	=	(location.xyz + float3(0.5f,0.5f,0.5f)) / float3(FogSizeX, FogSizeY, FogSizeZ);
	
	float	tangentX		=	lerp( -Camera.CameraTangentX,  Camera.CameraTangentX, normLocation.x );
	float	tangentY		=	lerp(  Camera.CameraTangentY, -Camera.CameraTangentY, normLocation.y );
	
	float	vsDistance		=	-log(1-normLocation.z)/FogGridExpScale;

	float4	vsPosition		=	float4( vsDistance * tangentX, vsDistance * tangentY, -vsDistance, 1 );
	
	float3	wsPosition		=	mul( vsPosition, Camera.ViewInverted ).xyz;
	float3	cameraPos		=	Camera.CameraPosition.xyz;
	
	float	density			=	0.005 * min(1, 1*exp(-0.04*(wsPosition.y-15))) + 0.0001;
	float3	normal			=	normalize( wsPosition - cameraPos );
	float3	emission		=	ComputeClusteredLighting( wsPosition, normal, float3(1,1,1)*1, 1, 1 );

	
	FogTarget[ location.xyz ] = float4(emission * density, density);
	
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
	
	float4	accumScatteringTransmittance	=	float4(0,0,0,1);
	
	for ( int slice=0; slice<FogGridDepth; slice++ ) {
		
		float	frontDistance			=	- log( 1 - (slice+0.0000f) * invDepthSlices ) / FogGridExpScale;
		float	backDistance			=	- log( 1 - (slice+0.9999f) * invDepthSlices ) / FogGridExpScale;
		float	stepLength				=	abs(backDistance - frontDistance);
		
		float4 	scatteringExtinction	=	FogSource[ int3( location.xy, slice ) ];
		float	transmittance			=	exp( -scatteringExtinction.a * stepLength );
		
		accumScatteringTransmittance.rgb	+=	scatteringExtinction.rgb * accumScatteringTransmittance.a;
		accumScatteringTransmittance.a		*=	transmittance;

		float4	storedValue		=	float4( accumScatteringTransmittance.rgb, 1 - accumScatteringTransmittance.a );
		FogTarget[ int3( location.xy, slice ) ]	=	storedValue;
		
		//FogTarget[ int3( location.xy, slice ) ]	=	float4( scatteringExtinction.rgb, 1 );
	}
}

#endif


