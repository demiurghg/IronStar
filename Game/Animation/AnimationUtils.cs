using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.Animation {

	public enum AnimationCurve
	{
		LinearStep			,
		SmoothStep			,
		SmootherStep		,
		QuadraticStep		,
		QuadraticStepInv	,
		SlowFollowThrough	,
		FastFollowThrough	,
	}


	static class AnimationUtils 
	{
		public static float Curve( AnimationCurve curve, float x )
		{
			switch (curve)
			{
				case AnimationCurve.LinearStep			: return LinearStep			( x );
				case AnimationCurve.SmoothStep			: return SmoothStep			( x );
				case AnimationCurve.SmootherStep		: return SmootherStep		( x );
				case AnimationCurve.QuadraticStep		: return QuadraticStep		( x );
				case AnimationCurve.QuadraticStepInv	: return QuadraticStepInv	( x );
				case AnimationCurve.SlowFollowThrough	: return SlowFollowThrough	( x );
				case AnimationCurve.FastFollowThrough	: return FastFollowThrough	( x );
			}
			return x;
		}

		public static float SmoothStep( float x )
		{
			return MathUtil.SmoothStep(x);
		}


		public static float SmootherStep( float x )
		{
			return MathUtil.SmootherStep(x);
		}


		public static float QuadraticStep( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;
			return x * x;
		}


		public static float QuadraticStepInv( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;
			return 1 - (1-x) * (1-x);
		}


		public static float LinearStep( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;
			return x;
		}


		public static float SlowFollowThrough( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;

			const float p = 0.750f;
			const float h = 0.125f;

			if (x<p) 
			{	
				return SmoothStep( x / p ) * ( 1 + h );
			}
			else
			{
				return 1 + h - h * SmoothStep( ( x - p ) / ( 1 - p ) );
			}
		}


		public static float FastFollowThrough( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;

			const float p = 0.660f;
			const float h = 0.125f;

			if (x<p) 
			{	
				return QuadraticStepInv( x / p ) * ( 1 + h );
			}
			else
			{
				return 1 + h - h * SmoothStep( ( x - p ) / ( 1 - p ) );
			}
		}


		public static float KickCurve ( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;

			const float p = 0.333f;

			if (x<p) 
			{	
				return QuadraticStepInv( x / p );
			}
			else
			{
				return 1 - SmoothStep( ( x - p ) / ( 1 - p ) );
			}
		}


		public static float BellCurve ( float x )
		{
			if (x<0) return 0;
			if (x>1) return 1;

			if (x<0.5f) 
			{	
				return SmoothStep(2*x);
			}
			else
			{
				return SmoothStep(2-2*x);
			}
		}


		public static Matrix Lerp ( Matrix x0, Matrix x1, float weight )
		{
			if (weight==0) 
			{
				return x0;
			}
			if (weight==1) 
			{
				return x1;
			}

			Quaternion q0, q1;
			Vector3 t0, t1;
			Vector3 s0, s1;

			x0.Decompose( out s0, out q0, out t0 );
			x1.Decompose( out s1, out q1, out t1 );

			var q	=	Quaternion.Slerp( q0, q1, weight );
			var t	=	Vector3.Lerp( t0, t1, weight );
			var s	=	Vector3.Lerp( s0, s1, weight );

			var x	=	Matrix.Scaling( s ) * Matrix.RotationQuaternion( q ) * Matrix.Translation( t );

			return x;
		}

		public static void Lerp ( Matrix[] frame1, Matrix[] frame2, float weight, Matrix[] destination )
		{
			if (frame1.Length!=frame2.Length) 
			{
				throw new ArgumentException("frame1.Length!=frame2.Length");
			}
			
			if (frame1.Length!=destination.Length) 
			{
				throw new ArgumentException("frame1.Length!=destination.Length");
			}

			int length = frame1.Length;


			for ( int i=0; i<length; i++ ) 
			{
				var x0	=	frame1[i];
				var x1	=	frame2[i];

				Quaternion q0, q1;
				Vector3 t0, t1;
				Vector3 s0, s1;

				x0.Decompose( out s0, out q0, out t0 );
				x1.Decompose( out s1, out q1, out t1 );

				var q	=	Quaternion.Slerp( q0, q1, weight );
				var t	=	Vector3.Lerp( t0, t1, weight );
				var s	=	Vector3.Lerp( s0, s1, weight );

				var x	=	Matrix.Scaling( s ) * Matrix.RotationQuaternion( q ) * Matrix.Translation( t );

				destination[i]	=	x;
			}
		}


		public static TimeSpan Max( TimeSpan a, TimeSpan b )
		{
			if (a >= b)	return a;
				else	return b;
		}

		public static TimeSpan Min( TimeSpan a, TimeSpan b )
		{
			if (a >= b)	return a;
				else	return b;
		}
	}
}
