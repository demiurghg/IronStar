#if 0
$ubershader 	DOWNSAMPLE
$ubershader		PREFILTER
#endif

#include "auto/cubegen.fxi"
#include "rgbe.fxi"

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
https://research.activision.com/publications/archives/fast-filtering-of-reflection-probes
-----------------------------------------------------------------------------*/

#ifdef PREFILTER

#include "cubegen.coeffs.hlsl"

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

void get_dir( out float3 dir, in float2 uv, in int face )
{
	switch ( face )
	{
	case 0:
		dir[0] = 1;
		dir[1] = uv[1];
		dir[2] = -uv[0];
		break;
	case 1:
		dir[0] = -1;
		dir[1] = uv[1];
		dir[2] = uv[0];
		break;
	case 2:
		dir[0] = uv[0];
		dir[1] = 1;
		dir[2] = -uv[1];
		break;
	case 3:
		dir[0] = uv[0];
		dir[1] = -1;
		dir[2] = uv[1];
		break;
	case 4:
		dir[0] = uv[0];
		dir[1] = uv[1];
		dir[2] = 1;
		break;
	default:
		dir[0] = -uv[0];
		dir[1] = uv[1];
		dir[2] = -1;
		break;
	}
}

#define GROUP_SIZE 64
[numthreads( GROUP_SIZE, 1, 1 )]
void CSMain( uint3 id : SV_DispatchThreadID )
{
	// INPUT: 
	// id.x = the linear address of the texel (ignoring face)
	// id.y = the face
	// -> use to index output texture
	// id.x = texel x
	// id.y = texel y
	// id.z = face

	// determine which texel this is
	int level = 0;
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
		return;
	}

	//SetSGPR( level );

	// determine dir / pos for the texel
	float3 dir, adir, frameZ;
	{
		id.z = id.y;
		int res = BASE_RESOLUTION >> level;
		id.y = id.x / res;
		id.x -= id.y * res;

		float2 uv;
		uv.x = ( (float)id.x * 2.0f + 1.0f ) / (float)res - 1.0f;
		uv.y = -( (float)id.y * 2.0f + 1.0f ) / (float)res + 1.0f;

		get_dir( dir, uv, id.z );
		frameZ = normalize( dir );

		adir[0] = abs( dir[0] );
		adir[1] = abs( dir[1] );
		adir[2] = abs( dir[2] );
	}

	// GGX gather colors
	float4 color = 0;
	for ( int axis = 0; axis < 3; axis++ )
	{
		const int otherAxis0 = 1 - ( axis & 1 ) - ( axis >> 1 );
		const int otherAxis1 = 2 - ( axis >> 1 );

		float frameweight = ( max( adir[otherAxis0], adir[otherAxis1] ) - .75f ) / .25f;
		if ( frameweight > 0 )
		{
			// determine frame
#if 0
			float3 UpVector = 0;
			UpVector[axis] = 1;
#else
			float3 UpVector;
			switch ( axis )
			{
			case 0:
				UpVector = float3( 1, 0, 0 );
				break;
			case 1:
				UpVector = float3( 0, 1, 0 );
				break;
			default:
				UpVector = float3( 0, 0, 1 );
				break;
			}
#endif
			float3 frameX = normalize( cross( UpVector, frameZ ) );
			float3 frameY = cross( frameZ, frameX );

			// calculate parametrization for polynomial
			float Nx = dir[otherAxis0];
			float Ny = dir[otherAxis1];
			float Nz = adir[axis];

			float NmaxXY = max( abs( Ny ), abs( Nx ) );
			Nx /= NmaxXY;
			Ny /= NmaxXY;

			float theta;
			if ( Ny < Nx )
			{
				if ( Ny <= -.999 )
					theta = Nx;
				else
					theta = Ny;
			}
			else
			{
				if ( Ny >= .999 )
					theta = -Nx;
				else
					theta = -Ny;
			}

			float phi;
			if ( Nz <= -.999 )
				phi = -NmaxXY;
			else if ( Nz >= .999 )
				phi = NmaxXY;
			else
				phi = Nz;

			float theta2 = theta*theta;
			float phi2 = phi*phi;

			// sample
			for ( int iSuperTap = 0; iSuperTap < NUM_TAPS / 4; iSuperTap++ )
			{
				const int index = ( NUM_TAPS / 4 ) * axis + iSuperTap;
				float4 coeffsDir0[3];
				float4 coeffsDir1[3];
				float4 coeffsDir2[3];
				float4 coeffsLevel[3];
				float4 coeffsWeight[3];

				for ( int iCoeff = 0; iCoeff < 3; iCoeff++ )
				{
					coeffsDir0[iCoeff] = coeffs[level][0][iCoeff][index];
					coeffsDir1[iCoeff] = coeffs[level][1][iCoeff][index];
					coeffsDir2[iCoeff] = coeffs[level][2][iCoeff][index];
					coeffsLevel[iCoeff] = coeffs[level][3][iCoeff][index];
					coeffsWeight[iCoeff] = coeffs[level][4][iCoeff][index];
				}

				for ( int iSubTap = 0; iSubTap < 4; iSubTap++ )
				{
					// determine sample attributes (dir, weight, level)
					float3 sample_dir
						= frameX * ( coeffsDir0[0][iSubTap] + coeffsDir0[1][iSubTap] * theta2 + coeffsDir0[2][iSubTap] * phi2 )
						+ frameY * ( coeffsDir1[0][iSubTap] + coeffsDir1[1][iSubTap] * theta2 + coeffsDir1[2][iSubTap] * phi2 )
						+ frameZ * ( coeffsDir2[0][iSubTap] + coeffsDir2[1][iSubTap] * theta2 + coeffsDir2[2][iSubTap] * phi2 );

					float sample_level = coeffsLevel[0][iSubTap] + coeffsLevel[1][iSubTap] * theta2 + coeffsLevel[2][iSubTap] * phi2;

					float sample_weight = coeffsWeight[0][iSubTap] + coeffsWeight[1][iSubTap] * theta2 + coeffsWeight[2][iSubTap] * phi2;
					sample_weight *= frameweight;

					// adjust for jacobian
					sample_dir /= max( abs( sample_dir[0] ), max( abs( sample_dir[1] ), abs( sample_dir[2] ) ) );
					sample_level += 0.75f * log2( dot( sample_dir, sample_dir ) );

					// sample cubemap
					color.xyz += tex_in.SampleLevel( trilinear, sample_dir, sample_level ).xyz * sample_weight;
					color.w += sample_weight;
				}
			}
		}
	}
	color /= color.w;

	// write color
	color.x = max( 0, color.x );
	color.y = max( 0, color.y );
	color.z = max( 0, color.z );
	color.w = 1;

	switch ( level )
	{
	case 0:
		tex_out0[id] = color;
		break;
	case 1:
		tex_out1[id] = color;
		break;
	case 2:
		tex_out2[id] = color;
		break;
	case 3:
		tex_out3[id] = color;
		break;
	case 4:
		tex_out4[id] = color;
		break;
	case 5:
		tex_out5[id] = color;
		break;
	default:
		tex_out6[id] = color;
		break;
	}
}

#endif