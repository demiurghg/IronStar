
#ifdef _UBERSHADER
$ubershader FORWARD RIGID|SKINNED ANISOTROPIC +TRANSPARENT IRRADIANCE_MAP|IRRADIANCE_VOLUME
$ubershader SHADOW RIGID|SKINNED +TRANSPARENT
$ubershader ZPASS RIGID|SKINNED
$ubershader GBUFFER RIGID|SKINNED
// $ubershader RADIANCE RIGID IRRADIANCE_MAP|IRRADIANCE_VOLUME

// $ubershader FORWARD RIGID +ANISOTROPIC +TRANSPARENT IRRADIANCE_MAP|IRRADIANCE_VOLUME
// $ubershader SHADOW RIGID +TRANSPARENT
// $ubershader ZPASS RIGID
// $ubershader GBUFFER RIGID
// $ubershader RADIANCE RIGID IRRADIANCE_MAP|IRRADIANCE_VOLUME
#endif


struct VSInput {
	float3 Position : POSITION;
	float3 Tangent 	: TANGENT;
	float3 Binormal	: BINORMAL;
	float3 Normal 	: NORMAL;
	float4 Color 	: COLOR;
	float2 TexCoord : TEXCOORD0;
#ifdef RIGID
	float2 LMCoord  : TEXCOORD1;
#endif	
#ifdef SKINNED
    int4   BoneIndices  : BLENDINDICES0;
    float4 BoneWeights  : BLENDWEIGHTS0;
#endif	
};

struct PSInput {
	float4 	Position 	: SV_POSITION;
	float4 	Color 		: COLOR;
	float2 	TexCoord 	: TEXCOORD0;
	float3	Tangent 	: TEXCOORD1;
	float3	Binormal	: TEXCOORD2;
	float3	Normal 		: TEXCOORD3;
	float4	ProjPos		: TEXCOORD4;
	float3 	WorldPos	: TEXCOORD5;
	float2	LMCoord		: TEXCOORD6;
};

struct GBuffer {
	float4	hdr		 	: SV_Target0;
	float4	feedback	: SV_Target1;
#ifdef TRANSPARENT
	float4	distort		: SV_Target2;
#endif	
};

#include "auto/surface.fxi"

#include "gamma.fxi"
#include "shl1.fxi"

#define SHADOW_FILTER
#define SHADOW_TRANSITION

#include "ls_core.fxi"
#include "ls_fog.fxi"


#ifdef RADIANCE
#define DIFFUSE_ONLY
#endif

#include "surface_lighting.hlsl"

 
/*-----------------------------------------------------------------------------
	Vertex shader :
	Note on prefixes:
		s - means skinned
		w - means world
		v - means view
		p - means projected
-----------------------------------------------------------------------------*/

float4x3 ToFloat4x3 ( float4x4 m )
{
	return float4x3( m._m00_m10_m20_m30, 
					 m._m01_m11_m21_m31, 
					 m._m02_m11_m22_m32 );
}

float4x4 AccumulateSkin( float4 boneWeights, int4 boneIndices )
{
	float4x4 result = boneWeights.x * Bones[boneIndices.x];
	result = result + boneWeights.y * Bones[boneIndices.y];
	result = result + boneWeights.z * Bones[boneIndices.z];
	result = result + boneWeights.w * Bones[boneIndices.w];
	// float4x3 result = boneWeights.x * ToFloat4x3( Bones[boneIndices.x] );
	// result = result + boneWeights.y * ToFloat4x3( Bones[boneIndices.y] );
	// result = result + boneWeights.z * ToFloat4x3( Bones[boneIndices.z] );
	// result = result + boneWeights.w * ToFloat4x3( Bones[boneIndices.w] );
	return result;
}

float4 TransformPosition( int4 boneIndices, float4 boneWeights, float3 inputPos )
{
	float4 position = 0; 
	
	float4x4 xform  = AccumulateSkin(boneWeights, boneIndices); 
	position = mul( float4(inputPos,1), xform );
	
	return position;
}


float4 TransformNormal( int4 boneIndices, float4 boneWeights, float3 inputNormal )
{
    float4 normal = 0;

	float4x4 xform  = AccumulateSkin(boneWeights, boneIndices); 
	normal = mul( float4(inputNormal,0), xform );
	
	return float4(normal.xyz,0);	// force w to zero
}



PSInput VSMain( VSInput input )
{
	PSInput output;
	
	float4x4	projection	=	Camera.Projection;
	
	#if RIGID
		float4 	pos			=	float4( input.Position, 1 	);
		float4	wPos		=	mul( pos,  Instance.World 	);
		float4	vPos		=	mul( wPos, Camera.View 		);
		float4	pPos		=	mul( vPos, projection 		);
		float4	normal		=	mul( float4(input.Normal,0	),  Instance.World 		);
		float4	tangent		=	mul( float4(input.Tangent,0	),  Instance.World 		);
		float4	binormal	=	mul( float4(input.Binormal,0),  Instance.World 	);
	#endif
	#if SKINNED
		float4 	sPos		=	TransformPosition	( input.BoneIndices, input.BoneWeights, input.Position	);
		float4  sNormal		=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Normal	);
		float4  sTangent	=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Tangent	);
		float4  sBinormal	=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Binormal	);
		
		float4	wPos		=	mul( sPos, Instance.World 	);
		float4	vPos		=	mul( wPos, Camera.View 		);
		float4	pPos		=	mul( vPos, projection		);
		float4	normal		=	mul( sNormal,  Instance.World 	);
		float4	tangent		=	mul( sTangent,  Instance.World 	);
		float4	binormal	=	mul( sBinormal,  Instance.World 	);
	#endif
	
	output.Position 	= 	pPos;
	output.ProjPos		=	pPos;
	output.Color 		= 	Instance.Color;
	output.TexCoord		= 	input.TexCoord;
	output.Normal		= 	normal.xyz;
	output.Tangent 		=  	tangent.xyz;
	output.Binormal		=  	binormal.xyz;
	output.WorldPos		=	wPos.xyz;
	
	#ifdef RIGID
	output.LMCoord		=	mad( input.LMCoord.xy, Instance.LMRegion.xy, Instance.LMRegion.zw );
	#else
	output.LMCoord		=	0;
	#endif
	
	return output;
}


 
/*-----------------------------------------------------------------------------
	Pixel shader :
-----------------------------------------------------------------------------*/

//	https://www.marmoset.co/toolbag/learn/pbr-theory	
//	This means that in theory conductors will not show any evidence of diffuse light. 
//	In practice however there are often oxides or other residues on the surface of a 
//	metal that will scatter some small amounts of light.

//	Blend mode refernce:
//	http://www.deepskycolors.com/archivo/2010/04/21/formulas-for-Photoshop-blending-modes.html	



float MipLevel( float2 uv );

//	https://www.opengl.org/discussion_boards/showthread.php/171485-Texture-LOD-calculation-(useful-for-atlasing)
//	http://developer.download.nvidia.com/opengl/specs/GL_EXT_texture_filter_anisotropic.txt
//	http://hugi.scene.org/online/coding/hugi%2014%20-%20comipmap.htm
//	http://www.mrelusive.com/publications/papers/Software-Virtual-Textures.pdf
float MipLevel( float2 uv )
{
	float2 dx = ddx( uv * VTPageSize*VTVirtualPageCount );
	float2 dy = ddy( uv * VTPageSize*VTVirtualPageCount );

#ifndef ANISOTROPIC
	float d = max( dot( dx, dx ), dot( dy, dy ) );
	return clamp( 0.5 * log2(d), 0, VTMaxMip-1 );
#else
	const float maxAniso 		= 4;
	const float maxAnisoLog2 	= log2( maxAniso );
	float 	px 			=	dot( dx, dx );
	float 	py 			=	dot( dy, dy );
	float 	maxLod		=	0.5 * log2( max( px, py ) ); 
	float 	minLod		=	0.5 * log2( min( px, py ) );
	float 	anisoLOD 	=	maxLod - min( maxLod - minLod, maxAnisoLog2 );
	
	return 	clamp( anisoLOD, 0, VTMaxMip-1 );
#endif	
}


/*-----------------------------------------------------------------------------
	ZPASS
-----------------------------------------------------------------------------*/

#ifdef ZPASS
//	ZPASS has no pixel shader
#endif

/*-----------------------------------------------------------------------------
	VIRTUAL TEXTURE
-----------------------------------------------------------------------------*/

SURFACE SampleVirtualTexture( PSInput input, out float4 feedback )
{
	SURFACE surf;
	surf.alpha			=	0.5f;
	surf.baseColor		=	pow(abs(Subset.Color.rgb), 2.2);
	surf.roughness		=	0.5f;
	surf.normal			=	float3(0,0,1);
	surf.emission		=	0;
	surf.metallic		=	0;
	surf.occlusion		=	1;
	
	float2 	scaledCoords	=	input.TexCoord.xy * Subset.Rectangle.zw;
	
	float2	checkerTC	=	input.TexCoord.xy;

	input.TexCoord.x	=	frac(input.TexCoord.x);
	input.TexCoord.y	=	frac(input.TexCoord.y);
	
	input.TexCoord.x	=	mad( input.TexCoord.x, Subset.Rectangle.z, Subset.Rectangle.x );
	input.TexCoord.y	=	mad( input.TexCoord.y, Subset.Rectangle.w, Subset.Rectangle.y );
	
	
	//---------------------------------
	//	Compute miplevel :
	//---------------------------------
	float2 mipuv	=	scaledCoords.xy * VTMipSelectorScale;
	float mipt		=	MipIndex.SampleGrad( MipSampler, mipuv, ddx(mipuv), ddy(mipuv) ).r;
	float mipf		=	clamp(mipt, 0, Subset.MaxMip); // MipLevel( scaledCoords );
	float mip		=	floor( mipf );
	
	float gradScale	=	Stage.VTGradientScaler * exp2(-mip);

	float2 uvddx	=	ddx( scaledCoords.xy ) * gradScale;
	float2 uvddy	=	ddy( scaledCoords.xy ) * gradScale;
	
	float scale		=	exp2(mip);
	float pageX		=	floor( saturate(input.TexCoord.x) * VTVirtualPageCount / scale );
	float pageY		=	floor( saturate(input.TexCoord.y) * VTVirtualPageCount / scale );
	float dummy		=	1;
	
	feedback		=	 float4( pageX / 1024.0f, pageY / 1024.0f, mip / 1024.0f, dummy / 4.0f );

	//---------------------------------
	//	Virtual texturing stuff :
	//---------------------------------
	float2 vtexTC		=	saturate(input.TexCoord);
	float4 fallback		=	float4( 0.5f, 0.5, 0.5f, 1.0f );
	int2 indexXY 		=	(int2)floor(input.TexCoord * VTVirtualPageCount / scale );
	float4 physPageTC	=	Texture0.Load( int3(indexXY, (int)(mip)) ).xyzw;
	
	float mipFrac		=	max(0, mipf - physPageTC.z);
	
	if (physPageTC.w>0) {
		float2 	withinPageTC	=	vtexTC * VTVirtualPageCount / exp2(physPageTC.z);
				withinPageTC	=	frac( withinPageTC );
				withinPageTC	=	withinPageTC * Stage.VTPageScaleRCP;

		float  halfTexel	=	0.5f / 4096.0f;
				
		float2	finalTC			=	physPageTC.xy + withinPageTC;// + float2(halfTexel, -halfTexel);
		
		#ifndef ANISOTROPIC
		float4	channelC		=	Texture1.SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		float4	channelN		=	Texture2.SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		float4	channelS		=	Texture3.SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		#else
		float4	channelC		=	Texture1.SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		float4	channelN		=	Texture2.SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		float4	channelS		=	Texture3.SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		#endif
		
		surf.baseColor		=	channelC.rgb;
		surf.alpha			=	channelC.a;
		surf.normal			=	channelN.rgb * 2 - 1;
		surf.roughness		=	channelS.r;
		surf.metallic		=	channelS.g;
		surf.emission		=	channelS.b * input.Color.rgb;
		surf.occlusion		=	channelS.a * channelS.a;
	}

	//---------------------------------
	//	Fallback for missing material :
	//---------------------------------
	if ( Subset.Rectangle.z==Subset.Rectangle.w && Subset.Rectangle.z==0 ) {
		float 	checkerX	=	frac(checkerTC.x*4) > 0.5 ? 1 : 0;
		float 	checkerY	=	frac(checkerTC.y*4) > 0.5 ? 1 : 0;
		float	checker		=	(checkerX+checkerY) % 2;
		surf.baseColor	=	pow(0.1*checker+0.5, 2);
		surf.normal		=	float3(0,0,1);
		surf.roughness	=	0.5;
		surf.metallic	=	0;
		surf.emission	=	0;
		surf.alpha		=	0.5f;
	}

	//---------------------------------
	//	Transform local-normal to world space
	//---------------------------------
	float3x3 tbnToWorld	= float3x3(
			input.Tangent.x,	input.Tangent.y,	input.Tangent.z,	
			input.Binormal.x,	input.Binormal.y,	input.Binormal.z,	
			input.Normal.x,		input.Normal.y,		input.Normal.z		
		);
		
	surf.normal		= 	normalize( mul( surf.normal, tbnToWorld ).xyz );
	
	return surf;
}


/*-----------------------------------------------------------------------------
	FORWARD
-----------------------------------------------------------------------------*/

#ifdef FORWARD
GBuffer PSMain( PSInput input )
{
	GBuffer output;

	float4	feedback;
	SURFACE surface		=	SampleVirtualTexture( input, feedback );

	float3 	triNormal	=	cross( ddx(input.WorldPos.xyz), -ddy(input.WorldPos.xyz) );
			triNormal	=	normalize( triNormal );
	
	float3 	lighting	=	ComputeClusteredLighting( input, Stage.ViewportSize.xy, surface, triNormal, input.LMCoord );
	
#ifdef TRANSPARENT
	float3	viewDir		=	normalize(Camera.CameraPosition.xyz - input.WorldPos.xyz);
	float	nDotV		=	max( 0, dot( surface.normal, viewDir ) );
	float	fresnelF	=	pow( 1-nDotV, 5 );
	surface.alpha		=	lerp( surface.alpha, 1, fresnelF );
	
	float4	viewNormal	=	normalize( mul( float4(surface.normal.xyz,0), Camera.View) );
	float4	distort		=	float4( viewNormal.xy * 0.5 + 0.5, surface.roughness, 1 );
	
	output.distort		=	distort;
#endif
	
	//	Fog :
	float	dist	=	distance( input.WorldPos.xyz, Camera.CameraPosition.xyz ); 
	float3	final	=	ApplyVolumetricFog( Fog, lighting, input.ProjPos, SamplerLinearClamp, FogVolume );
	
	output.hdr			=	float4( final, surface.alpha );
	output.feedback		=	feedback;
	
	return output;
}
#endif

/*-----------------------------------------------------------------------------
	RADIANCE
-----------------------------------------------------------------------------*/

#ifdef RADIANCE
float4 PSMain( PSInput input ) : SV_TARGET0
{
	float4	feedback;
	SURFACE surface		=	SampleVirtualTexture( input, feedback );

	float3 	triNormal	=	cross( ddx(input.WorldPos.xyz), -ddy(input.WorldPos.xyz) );
			triNormal	=	normalize( triNormal );
	
	float3 	lighting	=	ComputeClusteredLighting( input, Stage.ViewportSize.xy, surface, triNormal, input.LMCoord );
	
	//	Apply fog :
	float3	final	=	ApplyVolumetricFog( lighting, input.ProjPos, SamplerLinearClamp, FogVolume );
	
	return	float4( final, 1 );
}
#endif

/*-----------------------------------------------------------------------------
	SHADOW
-----------------------------------------------------------------------------*/

#ifdef SHADOW
float4 PSMain( PSInput input, float4 vpos : SV_POSITION ) : SV_TARGET0
{
	float z		= input.ProjPos.z / Camera.FarDistance;

	float dzdx	 = ddx(z);
	float dzdy	 = ddy(z);
	float slope = abs(dzdx) + abs(dzdy);
	
	#ifdef TRANSPARENT
		clip( 0.5 - dither2( vpos.x, vpos.y ) );
	#endif

	return z + Stage.DepthBias + slope * Stage.SlopeBias;
}
#endif

/*-----------------------------------------------------------------------------
	GBUFFER
-----------------------------------------------------------------------------*/

#ifdef GBUFFER

#include "rgbe.fxi"

struct LPGBuffer {
	float4	color 	: SV_Target0;
	float2	mapping	: SV_Target1;
};

LPGBuffer PSMain( PSInput input )
{	
	float4	feedback;
	SURFACE surface		=	SampleVirtualTexture( input, feedback );

	LPGBuffer	output;
	
	output.color	=	float4(surface.baseColor, 1);
	output.mapping	=	input.LMCoord.xy;

	return output;
}
#endif

