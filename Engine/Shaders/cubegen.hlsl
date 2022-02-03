#if 0
$ubershader 	DOWNSAMPLE
$ubershader		PREFILTER +REFERENCE..DIFFERENCE
$ubershader		DIFFUSE
#endif

#include "auto/cubegen.fxi"
#include "rgbe.fxi"
#include "hammersley.fxi"

/*------------------------------------------------------------------------------
// Copyright 2016 Activision Publishing, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software 
// is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
------------------------------------------------------------------------------*/

/*------------------------------------------------------------------------------
https://research.activision.com/publications/archives/fast-filtering-of-reflection-probes
------------------------------------------------------------------------------*/

#ifdef DOWNSAMPLE

#define tex_hi_res	Source
#define tex_lo_res	Target
#define bilinear	LinearSampler
// TextureCube tex_hi_res : register( t0 );
// RWTexture2DArray<float4> tex_lo_res : register( u0 );
// SamplerState bilinear : register( s0 );


void get_dir_0( out float3 dir, in float u, in float v )
{
	dir[0] = 1;
	dir[1] = v;
	dir[2] = -u;
}
void get_dir_1( out float3 dir, in float u, in float v )
{
	dir[0] = -1;
	dir[1] = v;
	dir[2] = u;
}
void get_dir_2( out float3 dir, in float u, in float v )
{
	dir[0] = u;
	dir[1] = 1;
	dir[2] = -v;
}
void get_dir_3( out float3 dir, in float u, in float v )
{
	dir[0] = u;
	dir[1] = -1;
	dir[2] = v;
}
void get_dir_4( out float3 dir, in float u, in float v )
{
	dir[0] = u;
	dir[1] = v;
	dir[2] = 1;
}
void get_dir_5( out float3 dir, in float u, in float v )
{
	dir[0] = -u;
	dir[1] = v;
	dir[2] = -1;
}

void get_dir( out float3 dir, in float u, in float v, in int face )
{
	switch ( face )
	{
		case 0:		get_dir_0( dir, u, v );		break;
		case 1:		get_dir_1( dir, u, v );		break;
		case 2:		get_dir_2( dir, u, v );		break;
		case 3:		get_dir_3( dir, u, v );		break;
		case 4:		get_dir_4( dir, u, v );		break;
		default:	get_dir_5( dir, u, v );		break;
	}
}

float calcWeight( float u, float v )
{
	float val = u*u + v*v + 1;
	return val*sqrt( val );
}

[numthreads( 8, 8, 1 )]
void CSMain( uint3 id : SV_DispatchThreadID )
{
	uint res_lo;
	{
		uint h, e;
		tex_lo_res.GetDimensions( res_lo, h, e );
	}


	if ( id.x < res_lo && id.y < res_lo )
	{
		float inv_res_lo = rcp( (float)res_lo );

		float u0 = ( (float)id.x * 2.0f + 1.0f - .75f ) * inv_res_lo - 1.0f;
		float u1 = ( (float)id.x * 2.0f + 1.0f + .75f ) * inv_res_lo - 1.0f;

		float v0 = ( (float)id.y * 2.0f + 1.0f - .75f ) * -inv_res_lo + 1.0f;
		float v1 = ( (float)id.y * 2.0f + 1.0f + .75f ) * -inv_res_lo + 1.0f;

		float weights[4];
		weights[0] = calcWeight( u0, v0 );
		weights[1] = calcWeight( u1, v0 );
		weights[2] = calcWeight( u0, v1 );
		weights[3] = calcWeight( u1, v1 );

		const float wsum = 0.5f / ( weights[0] + weights[1] + weights[2] + weights[3] );
		[unroll]
		for ( int i = 0; i < 4; i++ )
			weights[i] = weights[i] * wsum + .125f;

#if 1
		float3 dir;
		float4 color;
		switch ( id.z )
		{
		case 0:
			get_dir_0( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_0( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_0( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_0( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		case 1:
			get_dir_1( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_1( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_1( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_1( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		case 2:
			get_dir_2( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_2( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_2( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_2( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		case 3:
			get_dir_3( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_3( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_3( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_3( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		case 4:
			get_dir_4( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_4( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_4( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_4( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		default:
			get_dir_5( dir, u0, v0 );	color =  tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];
			get_dir_5( dir, u1, v0 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];
			get_dir_5( dir, u0, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];
			get_dir_5( dir, u1, v1 );	color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
			break;
		}
#else
		float3 dir;
		get_dir( dir, u0, v0, id.z );
		float4 color = tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[0];

		get_dir( dir, u1, v0, id.z );
		color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[1];

		get_dir( dir, u0, v1, id.z );
		color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[2];

		get_dir( dir, u1, v1, id.z );
		color += tex_hi_res.SampleLevel( bilinear, dir, 0 ) * weights[3];
#endif

		tex_lo_res[id] = color;
	}
}

#endif

/*-----------------------------------------------------------------------------
https://www.iue.tuwien.ac.at/phd/ertl/node100.html
-----------------------------------------------------------------------------*/

#ifdef DIFFUSE

#include "hammersley.fxi"

#define NUM_DIFFUSE_SAMPLES 32

[numthreads( 8, 8, 1 )]
void CSMain( uint3 id : SV_DispatchThreadID )
{
	uint2 	location	=	id.xy;
	uint	faceId		=	id.z;
	uint	width, height, numFaces;
	
	Target.GetDimensions( width, height, numFaces );
	
	float4 	face[6] 	= 	{ 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0 };

	float u	=	mad( (location.x) / (float)(width -1), 2, - 1);
	float v	=	mad( (location.y) / (float)(height-1), 2, - 1);
	
	float3	dir = float3(0,0,0);

	switch (faceId)
	{
		case 0 : dir = normalize(float3(  1, -v, -u )); break;
		case 1 : dir = normalize(float3( -1, -v,  u )); break;
		case 2 : dir = normalize(float3(  u,  1,  v )); break;
		case 3 : dir = normalize(float3(  u, -1, -v )); break;
		case 4 : dir = normalize(float3(  u, -v,  1 )); break;
		case 5 : dir = normalize(float3( -u, -v, -1 )); break;
	}
	
	float4 accumColor = 0;
	
	for (int i=0; i<NUM_DIFFUSE_SAMPLES; i++)
	{
		float3	sample_dir	=	normalize(hammersley_sphere_uniform( i, NUM_DIFFUSE_SAMPLES ));
		float	nDorL		=	saturate( dot( sample_dir, dir ) );
		accumColor			+=	nDorL * Source.SampleLevel( LinearSampler, sample_dir, 0 );
	}
	
	accumColor /= NUM_DIFFUSE_SAMPLES;
	
	Target[int3(location.xy,faceId)]	=	accumColor;
}

#endif

/*-----------------------------------------------------------------------------
https://research.activision.com/publications/archives/fast-filtering-of-reflection-probes
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "hammersley.fxi"
#include "ls_brdf.fxi"

#define trilinear 	LinearSampler
#define tex_in		Source
#define tex_out0	Target0
#define tex_out1    Target1
#define tex_out2    Target2
#define tex_out3    Target3
#define tex_out4    Target4
#define tex_out5    Target5
#define tex_out6    Target6

// TextureCube tex_in : register( t0 );
// RWTexture2DArray<float4> tex_out0 : register( u0 );
// RWTexture2DArray<float4> tex_out1 : register( u1 );
// RWTexture2DArray<float4> tex_out2 : register( u2 );
// RWTexture2DArray<float4> tex_out3 : register( u3 );
// RWTexture2DArray<float4> tex_out4 : register( u4 );
// RWTexture2DArray<float4> tex_out5 : register( u5 );
// RWTexture2DArray<float4> tex_out6 : register( u6 );

#define NUM_TAPS 8
#define BASE_RESOLUTION 128

bool GetFaceLocalAddress( uint id, out uint level, out uint2 xy, out float2 uv )
{
	xy = uint2(0,0);
	uv = float2(0,0);
	
	if ( id.x < ( 128 * 128 ) )
	{
		level = 0;
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 ) )
	{
		level = 1;
		id.x -= ( 128 * 128 );
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 + 32 * 32 ) )
	{
		level = 2;
		id.x -= ( 128 * 128 + 64 * 64 );
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 ) )
	{
		level = 3;
		id.x -= ( 128 * 128 + 64 * 64 + 32 * 32 );
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 + 8 * 8 ) )
	{
		level = 4;
		id.x -= ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 );
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 + 8 * 8 + 4 * 4 ) )
	{
		level = 5;
		id.x -= ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 + 8 * 8 );
	}
	else if ( id.x < ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 + 8 * 8 + 4 * 4 + 2 * 2 ) )
	{
		level = 6;
		id.x -= ( 128 * 128 + 64 * 64 + 32 * 32 + 16 * 16 + 8 * 8 + 4 * 4 );
	}
	else
	{
		return false;
	}
	
	uint size 	= 	BASE_RESOLUTION >> level;
	xy.x		=	id % size;
	xy.y		=	id / size;
	
	uv.x 		= 	 ( (float)xy.x * 2.0f + 1.0f ) / (float)size - 1.0f;
	uv.y 		= 	-( (float)xy.y * 2.0f + 1.0f ) / (float)size + 1.0f;	
	
	return true;
}

float3 GetFaceDirection( in float2 uv, in uint face )
{
	switch ( face )
	{
		case 0:	 return float3(     1, 	uv.y,	-uv.x );	
		case 1:	 return float3(    -1, 	uv.y,	 uv.x );	
		case 2:	 return float3(  uv.x, 	   1,	-uv.y );	
		case 3:	 return float3(  uv.x, 	  -1,	 uv.y );	
		case 4:	 return float3(  uv.x, 	uv.y,	    1 );		
		default: return float3( -uv.x, 	uv.y,	   -1 );		
	}
}

float3 GetUpVector( in uint face )
{
	switch ( face )
	{
		case 0:	 return float3(  0,  1,  0 );	
		case 1:	 return float3(  0,  1,  0 );	
		case 2:	 return float3(  0,  0, -1 );	
		case 3:	 return float3(  0,  0,  1 );	
		case 4:	 return float3(  0,  1,  0 );		
		default: return float3(  0,  1,  0 );		
	}
}


float NDF(float perceptualRoughness, float3 R, float3 L)
{
	float3	N	=	R;
	float3	V	=	R;
			L	=	normalize(L);
			V	=	normalize(V);
	float3	H	=	normalize(V+L);
	
    float 	NoH	= 	saturate(dot(N, H));
	
    float  roughness = perceptualRoughness * perceptualRoughness;

    float 	D	=	D_GGX(NoH, roughness);
			
	return D;
}

#define GROUP_SIZE 64
[numthreads( GROUP_SIZE, 1, 1 )]
void CSMain( uint3 id : SV_DispatchThreadID )
{
	float2 uv;
	uint2 xy;
	uint level;
	uint face = id.y;
	
	GetFaceLocalAddress( id.x, level, xy, uv );
	
	uint3 storeXYf		=	uint3( xy.xy, face );
	float4 color		=	0;//tex_in.SampleLevel( LinearSampler, GetFaceDirection( uv, face ), level ); 
	float roughness		=	lerp(0.05f, 0.99f, level/6.0f);
	
	float3	direction	=	normalize( GetFaceDirection( uv, face ) );
	float3	upVector	=	GetUpVector( face );
	float3  tangentX	=	normalize( cross( direction, upVector ) );
	float3 	tangentY	=	normalize( cross( direction, tangentX ) );
	
	if (false/*level==0*/)
	{
		color	=	tex_in.SampleLevel( LinearSampler, direction, 0 ).rgba * 1
				+	tex_in.SampleLevel( LinearSampler, direction, 1 ).rgba * 0.006
				+	tex_in.SampleLevel( LinearSampler, direction, 2 ).rgba * 0.002
				;
	}
	else
	{
		uint count = level==0 ? 7 : 19;
		for (uint i=0; i<count; i++)
		{
			float4	filter		=	FilterData[20 * level + i];
			float3  localDir	=	normalize( direction + ( filter.x * tangentX + filter.y * tangentY ) );
			color.rgb			+=	tex_in.SampleLevel( LinearSampler, localDir, level + filter.z ).rgb * filter.w;
			color.a				+=	filter.w;
		}

		color /= color.w;
	}

#ifdef REFERENCE
	int range = 8;
	float4 refColor;
	float dxy = rcp( (float)(BASE_RESOLUTION >> level) ) * 1.5f;
	
	if (level<=0)
	{
		for (int x=-range; x<=range; x++)
		for (int y=-range; y<=range; y++)
		{
			float3  localDir	=	normalize( direction + ( x * tangentX + y * tangentY ) * dxy );
			float	nDotL		=	saturate( dot( direction, localDir ) );
			float	weight		=	NDF( roughness, direction, localDir );
			refColor.rgb		+=	tex_in.SampleLevel( LinearSampler, localDir, level ).rgb * weight * nDotL;
			refColor.a			+=	weight;
		}
	}
	else
	{
		for (int i=0; i<4096; i++)
		{
			float3	localDir	=	hammersley_sphere_uniform( i, 4096 );
			float	nDotL		=	saturate( dot( direction, localDir ) );
			float	weight		=	NDF( roughness, direction, localDir );
			refColor.rgb		+=	tex_in.SampleLevel( LinearSampler, localDir, level ).rgb * weight * nDotL;
			refColor.a			+=	weight;
		}
	}
	
	refColor /= refColor.w;
	
	#ifndef DIFFERENCE
		color = refColor;
	#else
		float lumFast = dot( color.rgb,    float3(0.3f,0.5f,0.2f));
		float lumRef  = dot( refColor.rgb, float3(0.3f,0.5f,0.2f));
		float diff	  = lumFast - lumRef;
		color	=	float4(5,0,0,1) * max(0,diff) + float4(0,0,15,1) * max(0,-diff);
	#endif
	
#endif
	
	switch ( level )
	{
		case 0:	 tex_out0[storeXYf] = color; break;
		case 1:	 tex_out1[storeXYf] = color; break;
		case 2:	 tex_out2[storeXYf] = color; break;
		case 3:	 tex_out3[storeXYf] = color; break;
		case 4:	 tex_out4[storeXYf] = color; break;
		case 5:	 tex_out5[storeXYf] = color; break;
		default: tex_out6[storeXYf] = color; break;
	}
}

#endif