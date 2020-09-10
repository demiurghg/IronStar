using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.Mathematics
{
	public static class Intersection
	{
		static float cos( float a ) { return (float)Math.Cos(a); }
		static float sin( float a ) { return (float)Math.Cos(a); }
		static float exp( float a ) { return (float)Math.Exp(a); }
		static float abs( float a ) { return Math.Abs(a); }
		static float max( float a, float b ) { return Math.Max(a,b); }
		static float min( float a, float b ) { return Math.Min(a,b); }
		static float length( Vector3 v ) { return v.Length(); }
		static float dot(Vector3 a, Vector3 b) { return Vector3.Dot( a, b ); }
		static float clamp(float a, float min, float max) { return MathUtil.Clamp( a, min, max ); }
		static float distance(Vector3 a, Vector3 b) { return Vector3.Distance(a, b); }
		static float pow(float a, float b) { return (float)Math.Pow( a, b ); }


		public static void DistancePointToLine( Vector3 a, Vector3 b, Vector3 c, out float d, out float t )
		{
			if (a==b) 
			{
				d	=	Vector3.Distance( a, c );
				t	=	0;
			}
			else
			{
				var ab		=	b - a;
				var ac		=	c - a;
				var area	=	Vector3.Cross( ab, ac ).Length();
				d			=	area / ab.Length();
				t			=	Vector3.Dot( ac, ab ) / ab.LengthSquared();
			}
		}


		public static void DistancePointToLineSegment( Vector3 a, Vector3 b, Vector3 c, out float d, out float t )
		{
			 DistancePointToLine( a, b, c, out d, out t );

			 if (t<0) 
			 {
				t = 0;
				d = Vector3.Distance( a, c );
			 }
			 else if (t>1) 
			 {
				t = 1;
				d = Vector3.Distance( b, c );
			 }
		}



		public static bool RaySphereIntersect(Vector3 origin, Vector3 dir, float radius, out float t0, out float t1 )
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
	}
}
