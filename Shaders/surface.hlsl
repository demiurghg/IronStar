


struct VSInput {
	float3 Position : POSITION;
	float3 Tangent 	: TANGENT;
	float3 Binormal	: BINORMAL;
	float3 Normal 	: NORMAL;
	float4 Color 	: COLOR;
	float2 TexCoord : TEXCOORD0;
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
	float4	TexShadow	: TEXCOORD6;
};

struct GBuffer {
	float4	hdr		 	: SV_Target0;
	float4	feedback	: SV_Target1;
};

#include "surface.auto.hlsl"

cbuffer 				CBStage 			: 	register(b0) { STAGE    	Stage     	: packoffset( c0 ); }	
cbuffer 				CBInstance 			: 	register(b1) { INSTANCE		Instance   	: packoffset( c0 ); }	
cbuffer 				CBSubset 			: 	register(b2) { SUBSET		Subset    	: packoffset( c0 ); }	
cbuffer 				CBBones				: 	register(b3) { float4x4 	Bones[128]	: packoffset( c0 ); }	
SamplerState			SamplerLinear		: 	register(s0);
SamplerState			SamplerPoint		: 	register(s1);
SamplerState			SamplerAnisotropic	: 	register(s2);
SamplerState			DecalSampler		: 	register(s3);
SamplerComparisonState	ShadowSampler		: 	register(s4);
SamplerState			ParticleSampler		: 	register(s5);
SamplerState			MipSampler			: 	register(s6);

Texture2D				Textures[4]			: 	register(t0);
Texture2D				MipIndex			: 	register(t4);
Texture3D<uint2>		ClusterTable		: 	register(t5);
Buffer<uint>			LightIndexTable		: 	register(t6);
StructuredBuffer<LIGHT>	LightDataTable		:	register(t7);
StructuredBuffer<DECAL>	DecalDataTable		:	register(t8);
Texture2D				DecalImages			:	register(t9);
Texture2D				ShadowMap			:	register(t10);
Texture2D				ShadowMapParticles	:	register(t11);
Texture2D				AmbientOcclusion	:	register(t12);
TextureCube				FogTable			: 	register(t13);

#ifdef _UBERSHADER
$ubershader FORWARD RIGID|SKINNED +ANISOTROPIC
$ubershader SHADOW RIGID|SKINNED
$ubershader ZPASS RIGID|SKINNED
#endif

#include "surface.lighting.hlsl"
#include "fog.fxi"

 
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

	#if RIGID
		float4 	pos			=	float4( input.Position, 1 );
		float4	wPos		=	mul( pos,  Instance.World 		);
		float4	vPos		=	mul( wPos, Stage.View 		);
		float4	pPos		=	mul( vPos, Stage.Projection );
		float4	normal		=	mul( float4(input.Normal,0),  Instance.World 		);
		float4	tangent		=	mul( float4(input.Tangent,0),  Instance.World 		);
		float4	binormal	=	mul( float4(input.Binormal,0),  Instance.World 	);
	#endif
	#if SKINNED
		float4 	sPos		=	TransformPosition	( input.BoneIndices, input.BoneWeights, input.Position	);
		float4  sNormal		=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Normal	);
		float4  sTangent	=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Tangent	);
		float4  sBinormal	=	TransformNormal		( input.BoneIndices, input.BoneWeights, input.Binormal	);
		
		float4	wPos		=	mul( sPos, Instance.World 		);
		float4	vPos		=	mul( wPos, Stage.View 		);
		float4	pPos		=	mul( vPos, Stage.Projection );
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
	output.TexShadow	=	float4(0,0,0,0);
	
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


#ifdef ZPASS
float4 PSMain( PSInput input ) : SV_TARGET0
{
	float3 normal = normalize(input.Normal.xyz) * 0.5f + 0.5f;
	return float4( normal, 1 );
}
#endif

#ifdef FORWARD
GBuffer PSMain( PSInput input )
{
	GBuffer output;

	float3x3 tbnToWorld	= float3x3(
			input.Tangent.x,	input.Tangent.y,	input.Tangent.z,	
			input.Binormal.x,	input.Binormal.y,	input.Binormal.z,	
			input.Normal.x,		input.Normal.y,		input.Normal.z		
		);
		
	float3	baseColor			=	0.5f;
	float	roughness			=	0.5f;
	float3	localNormal			=	float3(0,0,1);
	float	emission			=	0;
	float 	metallic			=	0;
	
	
	float2 	scaledCoords	=	input.TexCoord.xy * Subset.Rectangle.zw;
	
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
	
	float gradScale	=	Stage.GradientScaler * exp2(-mip);

	float2 uvddx	=	ddx( scaledCoords.xy ) * gradScale;
	float2 uvddy	=	ddy( scaledCoords.xy ) * gradScale;
	
	float scale		=	exp2(mip);
	float pageX		=	floor( saturate(input.TexCoord.x) * VTVirtualPageCount / scale );
	float pageY		=	floor( saturate(input.TexCoord.y) * VTVirtualPageCount / scale );
	float dummy		=	1;
	
	float4 feedback	=	 float4( pageX / 1024.0f, pageY / 1024.0f, mip / 1024.0f, dummy / 4.0f );

	#if 0
	if (input.Position.x>640) {
		output.hdr		=	frac(mipt);
		output.feedback	=	feedback;
		return output;
	}
	#endif

	//---------------------------------
	//	Virtual texturing stuff :
	//---------------------------------
	float2 vtexTC		=	saturate(input.TexCoord);
	float4 fallback		=	float4( 0.5f, 0.5, 0.5f, 1.0f );
	int2 indexXY 		=	(int2)floor(input.TexCoord * VTVirtualPageCount / scale );
	float4 physPageTC	=	Textures[0].Load( int3(indexXY, (int)(mip)) ).xyzw;
	
	float mipFrac		=	max(0, mipf - physPageTC.z);
	
	if (physPageTC.w>0) {
		float2 	withinPageTC	=	vtexTC * VTVirtualPageCount / exp2(physPageTC.z);
				withinPageTC	=	frac( withinPageTC );
				withinPageTC	=	withinPageTC * Stage.VTPageScaleRCP;

		float  halfTexel	=	0.5f / 4096.0f;
				
		float2	finalTC			=	physPageTC.xy + withinPageTC;// + float2(halfTexel, -halfTexel);
		
		#ifndef ANISOTROPIC
		float4	channelC		=	Textures[1].SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		float4	channelN		=	Textures[2].SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		float4	channelS		=	Textures[3].SampleLevel( SamplerLinear, finalTC, mipFrac ).rgba;
		#else
		float4	channelC		=	Textures[1].SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		float4	channelN		=	Textures[2].SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		float4	channelS		=	Textures[3].SampleGrad( SamplerLinear, finalTC, uvddx, uvddy ).rgba;
		#endif
		
		baseColor	=	channelC.rgb;
		localNormal	=	channelN.rgb * 2 - 1;
		roughness	=	channelS.r;
		metallic	=	channelS.g;
		emission	=	channelS.b;
	}

	if ( Subset.Rectangle.z==Subset.Rectangle.w && Subset.Rectangle.z==0 ) {
		float3	checker	=	floor(input.WorldPos.xyz-0.5f)/2;
		baseColor	=	0.2*frac(checker.x + checker.y + checker.z)+0.3;
		localNormal	=	float3(0,0,1);
		roughness	=	0.5;
		metallic	=	0;
		emission	=	0;
	}
	
	// output.hdr			=	float4( baseColor, 1 );
	// output.feedback		=	feedback;
	// return output;

	//---------------------------------
	//	Prepare output values :
	//---------------------------------
	//	NB: Multiply normal length by local normal projection on surface normal.
	//	Shortened normal will be used as Fresnel decay (self occlusion) factor.
	float3 	worldNormal = 	normalize( mul( localNormal, tbnToWorld ).xyz );
		
	float3 	triNormal	=	cross( ddx(input.WorldPos.xyz), -ddy(input.WorldPos.xyz) );
			triNormal	=	normalize( triNormal );
	
	float3 	entityColor	=	input.Color.rgb;
	
	float3 	lighting	=	ComputeClusteredLighting( input, ClusterTable, Stage.ViewBounds.xy, baseColor, worldNormal, triNormal, roughness, metallic );
	
			lighting	=	emission * entityColor + lighting;
	
	//---------------------------------
	//	Apply fog :
	//---------------------------------
	float	fogDensity	=	Stage.FogDensityHeight.x;
	float3	final		=	ApplyFogColor( lighting, FogTable, SamplerLinear, fogDensity, Stage.ViewPos.xyz, input.WorldPos.xyz );
	
	output.hdr			=	float4( final, 1 );
	output.feedback		=	feedback;
	
	return output;
}
#endif



#ifdef SHADOW
float4 PSMain( PSInput input ) : SV_TARGET0
{
	float z		= input.ProjPos.z / Stage.BiasSlopeFar.z;

	float dzdx	 = ddx(z);
	float dzdy	 = ddy(z);
	float slope = abs(dzdx) + abs(dzdy);

	return z + Stage.BiasSlopeFar.x + slope * Stage.BiasSlopeFar.y;
}
#endif



