	
#if 0
$ubershader SHOW_LIGHTPROBES CUBES|SPHERES
$ubershader SHOW_LIGHTVOLUME
#endif

#include "gamma.fxi"

struct VSInput {
	float3 Position : POSITION;
	float3 Tangent 	: TANGENT;
	float3 Binormal	: BINORMAL;
	float3 Normal 	: NORMAL;
	float4 Color 	: COLOR;
	float2 TexCoord : TEXCOORD0;
	float2 LMCoord  : TEXCOORD1;
};


struct PSInput {
	float4 	Position 	: SV_POSITION;
	float4 	Color 		: COLOR;
	float3	Normal 		: TEXCOORD0;
	float3 	WorldPos	: TEXCOORD1;
	float	ImageIndex	: TEXCOORD2;
	
	nointerpolation 
	uint3	VoxelIndex	: TEXCOORD3;
};

#include "auto/lightmapDebug.fxi"

static const uint LightTypeOmni = 1;
static const uint LightTypeOmniShadow = 2;
static const uint LightTypeSpotShadow = 3;
static const uint LightTypeAmbient = 4;
static const uint LightSpotShapeSquare = 65536;
static const uint LightSpotShapeRound = 131072;
static const uint LightProbeSize = 128;
static const uint LightProbeMaxSpecularMip = 5;
static const uint LightProbeDiffuseMip = 6;

#include "ls_core.fxi"

PSInput VSMain( VSInput input, uint instanceId : SV_InstanceID )
{
	PSInput output;
	
	float4x4	projection	=	Camera.Projection;
	float4x4 	view		=	Camera.View;
	float4x4	world		=	float4x4( 1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 );
	uint3		voxelIndex	=	uint3(0,0,0);

	//float3	lpbPos		=	float3( 0, 0, instanceId );
#ifdef SHOW_LIGHTPROBES
	float   size		=	Params.LightProbeSize;
	float3	lpbPos		=	LightProbeData[ instanceId ].Position.xyz;
			size		=	size * LightProbeData[ instanceId ].Position.w;
#endif	

#ifdef SHOW_LIGHTVOLUME
	float	size		=	Params.VolumeStride / 8.0f;
	uint	width		=	Params.VolumeWidth;
	uint	height		=	Params.VolumeHeight;
	uint	depth		=	Params.VolumeDepth;
	uint	index_x		=	( instanceId % width		  );
	uint	index_y		=	( instanceId / width % height );
	uint	index_z		=	( instanceId / width / height );
			voxelIndex	=	uint3(index_x,index_y,index_z);
			
	float3	lpbPos		=	mad( float3(index_x,index_y,index_z), Params.VoxelToWorldScale.xyz, Params.VoxelToWorldOffset.xyz );
#endif	
	
	float4 	pos			=	float4( input.Position * size + lpbPos, 1 	);
	float4	wPos		=	mul( pos,  world 	);
	float4	vPos		=	mul( wPos, view 		);
	float4	pPos		=	mul( vPos, projection 	);
	float4	normal		=	mul( float4(input.Normal, 0 ), world );
	
	output.Position 	= 	pPos;
	output.Color 		= 	input.Color;
	output.Normal		= 	normal.xyz;
	output.WorldPos		=	lpbPos;
	output.VoxelIndex	=	voxelIndex;
	
	output.ImageIndex	=	LightProbeData[ instanceId ].ImageIndex;
	
	return output;
}


float GetLuminance(float3 color)
{
    return dot(color, float3(0.2127f, 0.7152f, 0.0722f));
}


float3 ColorSaturation( float3 rgbVal, float3 sat )
{
	float3 grey = GetLuminance(rgbVal);
	return grey + sat * (rgbVal-grey);
}


float4 PSMain( PSInput input ) : SV_Target0
{	
	float3 	cameraPos		=	Camera.CameraPosition.xyz;
	float3  surfacePos		=	input.WorldPos;
	float3	surfaceNormal	=	normalize(input.Normal);
	float	imageIndex		=	input.ImageIndex;
	float3	tint			=	float3(1,1,1);
	
	if (imageIndex>=MaxLightProbes)
	{
		tint = float3(1,0,0);
	}
	
	SURFACE 	surf	=	CreateDiffuseSurface( SRGBToLinear(float3(0.5,0.5,0.5)), surfaceNormal );
	GEOMETRY	geom	=	CreateGeometry( surfacePos, surfaceNormal );

	float3	viewDir		=	cameraPos - surfacePos;
	
#ifdef SHOW_LIGHTPROBES
	#ifdef SPHERES
	float3	reflectDir	=	reflect( -viewDir, surfaceNormal ) * float3(-1,1,1);
	#endif
	#ifdef CUBES
	float3	reflectDir	=	surfaceNormal * float3(-1,1,1);
	#endif
	
	float4	lightProbe	=	LightProbes.SampleLevel( Sampler, float4(reflectDir, imageIndex), Params.LightProbeMipLevel ).rgba;

	return float4(lightProbe.rgb * tint,1);
#endif

#ifdef SHOW_LIGHTVOLUME
	LIGHTMAP_RESOURCES rc;
	rc.IrradianceVolumeL0	=	LightVolumeL0;
	rc.IrradianceVolumeL1	=	LightVolumeL1;
	rc.IrradianceVolumeL2	=	LightVolumeL2;
	rc.IrradianceVolumeL3	=	LightVolumeL3;
	rc.Sampler				=	Sampler;
	
	float3 lighting		=	EvaluateLightVolume( rc, geom, surf, Camera, input.VoxelIndex ); // * 4 * 3.14; 
	return float4(lighting,1);
#endif
}









