#if 0
$ubershader 	COMPUTE|INTEGRATE
#endif

#include "auto/fog.fxi"


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
	// int3 location		=	dispatchThreadId.xyz;
	// int3 blockSize		=	int3(BlockSizeX,BlockSizeY,BlockSizeZ);
	
	// float value = (location.x + location.y + location.z)&1;
	
	// float3	normLocation	=	(location.xyz + float3(0.5f,0.5f,0.5f)) / float3(FogSizeX, FogSizeY, FogSizeZ);
	
	// float	tangentX		=	lerp( -Params.CameraTangentX,  Params.CameraTangentX, normLocation.x );
	// float	tangentY		=	lerp(  Params.CameraTangentY, -Params.CameraTangentY, normLocation.y );
	
	// float	viewDistance	=	-log(1-normLocation.z)/0.03;

	// float4	viewPos			=	float4( viewDistance * tangentX, viewDistance * tangentY, -viewDistance, 1 );
	
	// float3	worldPos		=	mul( viewPos, Params.CameraMatrix );
	
	// float	density			=	0.01 * min(1, exp(-0.2*worldPos.y));
	// float3	emission		=	ComputeClusteredLighting( worldPos ) * density;

	// //Target[ location.xyz ] = float4(frac(worldPos.yyy/5), 1);
	// //Target[ location.xyz ] = float4(normLocation, 1);
	
	// Target[ location.xyz ] = float4(emission, density);
	//Target[ location.xyz ] = frac(viewDistance/10);
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
	// int3 location		=	dispatchThreadId.xyz;
	
	// float4 value = Source[ int3( location.xy, 0 ) ];
	// Target[ int3( location.xy, 0 ) ] = value;
	
	// float	transparency	=	1;
	
	// float3	illumination	=	value.rgb;
	
	// for ( int i=1; i<FogSizeZ; i++ ) {
		
		// float4 	src		=	Source[ int3( location.xy, i ) ];
				// value	=	lerp( value, src, src.a );//*/
				
		// transparency	*=	exp(-1 * src.a);
		
		// illumination	+=	src.rgb;
				
		// float4	temp	=	float4( illumination, 1 - transparency );

		// Target[ int3( location.xy, i ) ]	=	temp;
	// }
}

#endif


