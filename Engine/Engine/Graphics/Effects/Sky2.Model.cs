using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core.Shell;
using Fusion.Engine.Imaging;

namespace Fusion.Engine.Graphics 
{
	public partial class Sky2
	{
		float cos( float a ) { return (float)Math.Cos(a); }
		float sin( float a ) { return (float)Math.Cos(a); }
		float exp( float a ) { return (float)Math.Exp(a); }
		float abs( float a ) { return Math.Abs(a); }
		float max( float a, float b ) { return Math.Max(a,b); }
		float min( float a, float b ) { return Math.Min(a,b); }
		Color4 exp( Color4 a ) { return new Color4( exp(a.Red), exp(a.Green), exp(a.Blue), exp(a.Alpha) ); }
		float length( Vector3 v ) { return v.Length(); }
		float dot(Vector3 a, Vector3 b) { return Vector3.Dot( a, b ); }
		float clamp(float a, float min, float max) { return MathUtil.Clamp( a, min, max ); }
		float distance(Vector3 a, Vector3 b) { return Vector3.Distance(a, b); }
		float pow(float a, float b) { return (float)Math.Pow( a, b ); }
		Vector3 lerp(Vector3 a, Vector3 b, float t) { return Vector3.Lerp( a, b, t ); }
		Color4 clamp(Color4 c, float min, float max) { return Color4.Clamp( c, new Color4(min), new Color4(max) ); }

		float AtmosphereRadius { get { return PlanetRadius * 1000 + AtmosphereHeight * 1000; } }

		struct SkyST 
		{
			public Color4 Scattering;
			public Color4 Transmittance;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Vector3 GetSunDirection()
		{
			float	cosAlt	=	(float)Math.Cos( MathUtil.DegreesToRadians( SunAltitude ) );
			float	sinAlt	=	(float)Math.Sin( MathUtil.DegreesToRadians( SunAltitude ) );

			float	cosAz	=	(float)Math.Cos( MathUtil.DegreesToRadians( SunAzimuth ) );
			float	sinAz	=	(float)Math.Sin( MathUtil.DegreesToRadians( SunAzimuth ) );

			float	x		=	 sinAz * cosAlt;
			float	y		=	 sinAlt;
			float	z		=	-cosAz * cosAlt;

			return new Vector3( x, y, z ).Normalized();
		}


		public Vector4 GetSunDirection4()
		{
			return new Vector4( GetSunDirection(), 0 );
		}


		public Vector3 GetViewOrigin()
		{
			return Vector3.Up * (PlanetRadius * 1000 + Math.Max( 2, ViewElevation ) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color4 GetSunIntensity( bool horizonDarken = false )
		{
			float	scale	=	MathUtil.Exp2( SunIntensityEv );
			Color4	color	=	Temperature.GetColor( (int)SunTemperature );
			color *= scale;

			if (horizonDarken)
			{
				var origin	=	Vector3.Up * ( PlanetRadius * 1000 + ViewElevation );
				var dir		=	GetSunDirection();
				color		*=	ComputeAtmosphereAbsorption( origin, dir );
			}

			return 	color;
		}



		Color4 ComputeZenithColor()
		{
			var st = ComputeSkyColor( GetViewOrigin() + Vector3.Up * (MieHeight*0.5f), Vector3.Up);
			return st.Scattering;
		}


		Color4 ComputeLightExtincation( Vector3 p0, Vector3 p1 )
		{
			int numSamples = 3; // is enough for half numerical solution
								// fully numerical solution requires about 64 samples
	
			//	computes amount of attenuated sun light 
			//	coming at given point p0.
			float 	opticalLength	=	Vector3.Distance( p0, p1 ) / numSamples;
			float 	opticalDepthR	=	0;
			float 	opticalDepthM	=	0;
			var	betaR				=	BetaRayleigh * MathUtil.Exp2( RayleighScale );
			var	betaM				=	BetaMie		 * MathUtil.Exp2( MieScale ) /** MieColor*/;
	
			//	integrate transmittance numerically using
			//	analytically calculated integral for each segment
			for (int i=0; i<numSamples; i++)
			{
				Vector3 pos0	= 	Vector3.Lerp( p0, p1, (i+0)/(float)numSamples );
				Vector3 pos1	= 	Vector3.Lerp( p0, p1, (i+1)/(float)numSamples );
				float  	h0		=	max( 0, length( pos0 ) - PlanetRadius * 1000 );
				float  	h1		=	max( 0, length( pos1 ) - PlanetRadius * 1000 );
				float	k		=	( h1 - h0 ) / opticalLength;
				float	hR0		=	exp(-h0 / RayleighHeight);
				float	hM0		=	exp(-h0 / MieHeight);
				float	hR1		=	exp(-h1 / RayleighHeight);
				float	hM1		=	exp(-h1 / MieHeight);
		
				if (abs(k)>0.001f)
				{
					//	integral exp(ax+b) = 1/a * exp(ax+b)
					//	a = k / H
					//	k = dh / dS
					opticalDepthR	+=	(-1) * (hR1 - hR0) * RayleighHeight / k;
					opticalDepthM	+=	(-1) * (hM1 - hM0) * MieHeight	    / k;
				}
				else
				{
					//	prevent division by zero, when height difference is small :
					opticalDepthR	+=	hR0 * opticalLength;
					opticalDepthM	+=	hM0 * opticalLength;
				}
			}
	
			Color4 tau	=	betaR * (opticalDepthR) + betaM * 1.1f * (opticalDepthM); 
	
			return exp( -tau );
		}



		Color4 ComputeIndcidentSunLight( Vector3 samplePosition, Vector3 sunDirection )
		{
			float t0, t1;
			//	no intersection, sun light comes without attenuation :
			if (!RaySphereIntersect(samplePosition, sunDirection, AtmosphereRadius, out t0, out t1))
			{
				return GetSunIntensity();
			}
	
			Vector3	p0	=	samplePosition + sunDirection * t0;
			Vector3	p1	=	samplePosition + sunDirection * t1;

			return ComputeLightExtincation( p0, p1 ) * GetSunIntensity();
		}



		const int numIntegrationSamlpes = 48;
		float t(float i) { return pow((i+0.5f) / numIntegrationSamlpes, 2); }
		float q(float i) { return t(i+0.5f) - t(i-0.5f); }


		SkyST ComputeSkyColor( Vector3 origin, Vector3 dir )
		{
			float	M_PI	=	3.141592f;
			float 	g 		= 	MieExcentricity; 
			var	betaR		=	BetaRayleigh * MathUtil.Exp2( RayleighScale );
			var	betaM		=	BetaMie		 * MathUtil.Exp2( MieScale ) /** MieColor*/;
			var		sunDir	=	GetSunDirection();

			SkyST st = new SkyST();
			st.Scattering		=	Color4.Zero;
			st.Transmittance	=	new Color4(1,1,1,1);
	
			float t0, t1; 
			if (!RaySphereIntersect(origin, dir, AtmosphereRadius, out t0, out t1)) return st; 
	
			// keep clamp for future use :
			//t0	=	clamp( t0, tmin, tmax );
			//t1	=	clamp( t1, tmin, tmax );
	
			Vector3 p0			=	origin + dir * t0;
			Vector3 p1			=	origin + dir * t1;
	 
			float 	segmentLength 	= distance(p0, p1); 
			float 	mu 				= dot(dir, sunDir); // cosine of the angle between the sun direction and the ray direction 
			float 	phaseR 			= 3f / (16f * M_PI) * (1 + mu * mu); 
			float 	phaseM 			= 3f / (8f * M_PI) * ((1f - g * g) * (1f + mu * mu)) / ((2f + g * g) * pow(abs(1f + g * g - 2f * g * mu), 1.5f)); 
	
			for (uint i = 0; i < numIntegrationSamlpes; ++i) 
			{ 
				Vector3 pos 	=	lerp( p0, p1, t(i) ); 
				float 	height 	=	length(pos) - PlanetRadius * 1000; 

				float 	hr 		= 	exp(-height / RayleighHeight) * segmentLength * q(i); 
				float 	hm 		= 	exp(-height / MieHeight		) * segmentLength * q(i); 
		
				var	extinction		=	hr * betaR + hm * betaM * 1.1f;
				var	extinctionClamp	=	clamp( extinction, 0.0000001f, 1 );
				var	transmittance	=	exp( - extinction );
		
				var	luminance		=	ComputeIndcidentSunLight( pos, sunDir );
				var	scattering		=	luminance * (hr * phaseR * betaR + hm * phaseM * betaM);
		
				var	integScatt		=	( scattering - scattering * transmittance ) / extinctionClamp;
				st.Scattering		+=	st.Transmittance * integScatt;
				st.Transmittance	*=	transmittance;
			} 
 
			return st; 
		}



		Color4 ComputeAtmosphereAbsorption( Vector3 origin, Vector3 direction )
		{
			var	betaR		=	BetaRayleigh * MathUtil.Exp2( RayleighScale );
			var	betaM		=	BetaMie		 * MathUtil.Exp2( MieScale ) /** MieColor*/;
			var distance	=	0.0f;

			if ( RayAtmosphereIntersection( origin, direction, out distance) )
			{
				var hitPoint	=	origin + direction * distance;

				return ComputeLightExtincation( origin, hitPoint );
			}
			else
			{
				return new Color4(1,1,1,1);
			}
		}


		bool RayAtmosphereIntersection( Vector3 origin, Vector3 dir, out float dist )
		{
			float t0, t1;

			if (!RaySphereIntersect( origin, dir, (PlanetRadius + AtmosphereHeight) * 1000, out t0, out t1 ) && t1<0)
			{
				dist = 0;
				return false;
			}
			else
			{
				dist = t1;
				return true;
			}
		}


		bool RaySphereIntersect(Vector3 origin, Vector3 dir, float radius, out float t0, out float t1 )
		{
			t0 = t1 = 0;
	
			var	r0	=	origin;			// - r0: ray origin
			var	rd	=	dir;			// - rd: normalized ray direction
			var	s0	=	Vector3.Zero;	// - s0: sphere center
			var	sr	=	radius;			// - sr: sphere radius

			float 	a 		= Vector3.Dot(rd, rd);
			Vector3	s0_r0 	= r0 - s0;
			float 	b 		= 2.0f * Vector3.Dot(rd, s0_r0);
			float 	c 		= Vector3.Dot(s0_r0, s0_r0) - (sr * sr);
	
			float	D		=	b*b - 4.0f*a*c;
	
			if (D<0)
			{
				return false;
			}
	
			t0	=	(-b - (float)Math.Sqrt(D))/(2.0f*a);
			t1	=	(-b + (float)Math.Sqrt(D))/(2.0f*a);

			if (t0<0 && t1<0) return false;

			t0	=	max(0, t0);

			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color4 GetSunBrightness()
		{
			float	scale	=	MathUtil.Exp2( SunBrightnessEv );
			Color4	color	=	Temperature.GetColor( (int)SunTemperature );
			color *= scale;

			color.Alpha		=	MathUtil.DegreesToRadians( SunAngularSize );

			return 	color;
		}
	}
}
