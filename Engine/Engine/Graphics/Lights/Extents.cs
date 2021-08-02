using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Lights 
{
	public static class Extents 
	{
		class Line 
		{
			public Line ( Vector3 a, Vector3 b ) 
			{ 
				A = new Vector4( a, 1 ); 
				B = new Vector4( b, 1 ); 
			}
			
			public Line ( Vector4 a, Vector4 b ) 
			{ 
				A = a; 
				B = b; 
			}
			public Vector4 A;
			public Vector4 B;
			
			/// <summary>
			/// Returns true if line is visible
			/// </summary>
			/// <param name="znear"></param>
			/// <returns></returns>
			public bool Clip ( float znear ) 
			{
				if ( A.Z <= znear && B.Z <= znear ) 
				{
					return true;
				}
				if ( A.Z >= znear && B.Z >= znear ) 
				{
					return false;
				}

				var factor	=	( znear - A.Z ) / ( B.Z - A.Z );
				var point	=	Vector4.Lerp( A, B, factor );
				
				if ( A.Z > znear ) A = point;
				if ( B.Z > znear ) B = point;

				return true;
			}
		}


		static Vector3 MakePoint( Vector4 p, bool projectZ )
		{
			if (projectZ)
			{
				return new Vector3( p.X/p.W, p.Y/p.W, p.Z/p.W );
			}
			else
			{
				return new Vector3( p.X/p.W, p.Y/p.W, Math.Abs(p.W) );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="projection"></param>
		/// <param name="frustum"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static bool GetFrustumExtent ( Matrix view, Matrix projection, Rectangle viewport, BoundingFrustum frustum, bool projectZ, out Vector3 min, out Vector3 max )
		{
			min = max	=	Vector3.Zero;

			var znear	=	projection.M34 * projection.M43 / projection.M33;

			var viewPoints = frustum.GetCorners();
			
			for (int i=0; i<viewPoints.Length; i++)
			{
				viewPoints[i] = Vector3.TransformCoordinate( viewPoints[i], view );
			}

			var lines = new[]
			{
				new Line( viewPoints[0], viewPoints[1] ),
				new Line( viewPoints[1], viewPoints[2] ),
				new Line( viewPoints[2], viewPoints[3] ),
				new Line( viewPoints[3], viewPoints[0] ),
														
				new Line( viewPoints[4], viewPoints[5] ),
				new Line( viewPoints[5], viewPoints[6] ),
				new Line( viewPoints[6], viewPoints[7] ),
				new Line( viewPoints[7], viewPoints[4] ),
													
				new Line( viewPoints[0], viewPoints[4] ),
				new Line( viewPoints[1], viewPoints[5] ),
				new Line( viewPoints[2], viewPoints[6] ),
				new Line( viewPoints[3], viewPoints[7] ),
			};

			lines = lines.Where( line => line.Clip(znear) ).ToArray();

			if (!lines.Any()) 
			{
				return false;
			}

			var projPoints = new List<Vector4>(24);
			
			foreach ( var line in lines ) 
			{
				projPoints.Add( Vector4.Transform( line.A, projection ) );
				projPoints.Add( Vector4.Transform( line.B, projection ) );
			}

			min		=	new Vector3( 99999,-99999, 99999);
			max		=	new Vector3(-99999, 99999,-99999);

			for (int i=0; i<projPoints.Count; i++)
			{
				var p	=	projPoints[i];
				min.X	=	Math.Min( min.X, p.X / p.W );
				min.Y	=	Math.Max( min.Y, p.Y / p.W );
				min.Z	=	Math.Min( min.Z, p.W );

				max.X	=	Math.Max( max.X, p.X / p.W );
				max.Y	=	Math.Min( max.Y, p.Y / p.W );
				max.Z	=	Math.Max( max.Z, p.W );
			}


			min.X	=	( min.X *  0.5f + 0.5f ) * viewport.Width;
			min.Y	=	( min.Y * -0.5f + 0.5f ) * viewport.Height;

			max.X	=	( max.X *  0.5f + 0.5f ) * viewport.Width;
			max.Y	=	( max.Y * -0.5f + 0.5f ) * viewport.Height;

			return true;
		} 



		static Vector3 ProjectPoint ( ref Vector3 point, ref Matrix proj, bool skipZ )
		{
			var pp = Vector3.TransformCoordinate( point, proj );

			if (skipZ)
			{
				return new Vector3( pp.X, pp.Y, -point.Z );
			}
			else
			{
				return pp;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="projection"></param>
		/// <param name="frustum"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static bool GetBasisExtent ( Matrix view, Matrix projection, Rectangle viewport, Matrix basis, out Vector3 min, out Vector3 max )
		{
			min = max	=	Vector3.Zero;

			var znear	=	projection.M34 * projection.M43 / projection.M33;
			
			var viewPoints = new Vector4[8];

			viewPoints[0]	=	new Vector4( basis.TranslationVector + basis.Right + basis.Up + basis.Backward, 1 );
			viewPoints[1]	=	new Vector4( basis.TranslationVector - basis.Right + basis.Up + basis.Backward, 1 );
			viewPoints[2]	=	new Vector4( basis.TranslationVector - basis.Right - basis.Up + basis.Backward, 1 );
			viewPoints[3]	=	new Vector4( basis.TranslationVector + basis.Right - basis.Up + basis.Backward, 1 );

			viewPoints[4]	=	new Vector4( basis.TranslationVector + basis.Right + basis.Up - basis.Backward, 1 );
			viewPoints[5]	=	new Vector4( basis.TranslationVector - basis.Right + basis.Up - basis.Backward, 1 );
			viewPoints[6]	=	new Vector4( basis.TranslationVector - basis.Right - basis.Up - basis.Backward, 1 );
			viewPoints[7]	=	new Vector4( basis.TranslationVector + basis.Right - basis.Up - basis.Backward, 1 );

			for (int i=0; i<viewPoints.Length; i++)
			{
				viewPoints[i] = Vector4.Transform( viewPoints[i], view );
			}

			var lines = new[]
			{
				new Line( viewPoints[0], viewPoints[1] ),
				new Line( viewPoints[1], viewPoints[2] ),
				new Line( viewPoints[2], viewPoints[3] ),
				new Line( viewPoints[3], viewPoints[0] ),
														
				new Line( viewPoints[4], viewPoints[5] ),
				new Line( viewPoints[5], viewPoints[6] ),
				new Line( viewPoints[6], viewPoints[7] ),
				new Line( viewPoints[7], viewPoints[4] ),
													
				new Line( viewPoints[0], viewPoints[4] ),
				new Line( viewPoints[1], viewPoints[5] ),
				new Line( viewPoints[2], viewPoints[6] ),
				new Line( viewPoints[3], viewPoints[7] ),
			};

			var projPoints = new List<Vector4>(24);
			
			for ( int i=0; i<lines.Length; i++ ) 
			{
				if (lines[i].Clip(znear))
				{
					projPoints.Add( Vector4.Transform( lines[i].A, projection ) );
					projPoints.Add( Vector4.Transform( lines[i].B, projection ) );
				}
			}

			if (projPoints.Count==0)
			{
				return false;
			}

			min		=	 Vector3.One;
			max		=	-Vector3.One;

			for (int i=0; i<projPoints.Count; i++)
			{
				var p	=	projPoints[i];
				min.X	=	Math.Min( min.X, p.X / p.W );
				min.Y	=	Math.Min( min.Y, p.Y / p.W );
				min.Z	=	Math.Min( min.Z, p.W );

				max.X	=	Math.Max( max.X, p.X / p.W );
				max.Y	=	Math.Max( max.Y, p.Y / p.W );
				max.Z	=	Math.Max( max.Z, p.W );
			}

			min.X	=	( min.X *  0.5f + 0.5f ) * viewport.Width;
			min.Y	=	( min.Y * -0.5f + 0.5f ) * viewport.Height;

			max.X	=	( max.X *  0.5f + 0.5f ) * viewport.Width;
			max.Y	=	( max.Y * -0.5f + 0.5f ) * viewport.Height;

			Misc.Swap( ref max.Y, ref min.Y );

			return true;
		} 


		/// <summary>
		/// 
		/// </summary>
		/// <param name="projection"></param>
		/// <param name="viewPos"></param>
		/// <param name="radius"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		public static bool GetSphereExtent ( Matrix view, Matrix projection, Vector3 position, Rectangle vp, float radius, bool projectZ, out Vector3 min, out Vector3 max )
		{
			min = max	=	Vector3.Zero;

			var znear	=	projection.M34 * projection.M43 / projection.M33;
			var nearW	=	projection.M11;
			var nearH	=	projection.M22;
			var viewPos	=	Vector3.TransformCoordinate( position, view );

			Vector3 min3, max3;
			

			var r0		=	GetSphereExtentAxis( znear, viewPos.X, viewPos.Z, radius, out min3.X, out max3.X );
			var r1		=	GetSphereExtentAxis( znear, viewPos.Y, viewPos.Z, radius, out min3.Y, out max3.Y );

			max3.Z		=	min3.Z	=	znear;
			var maxP	=	Vector3.TransformCoordinate( max3, projection );
			var minP	=	Vector3.TransformCoordinate( min3, projection );

			min.X		=	( minP.X * 0.5f + 0.5f ) * vp.Width;
			max.X		=	( maxP.X * 0.5f + 0.5f ) * vp.Width;

			max.Y		=	( minP.Y * -0.5f + 0.5f ) * vp.Height;
			min.Y		=	( maxP.Y * -0.5f + 0.5f ) * vp.Height;

			if (projectZ)
			{
				min.Z	=	Vector3.TransformCoordinate( new Vector3(0,0, Math.Min( viewPos.Z + radius, znear )), projection ).Z;
				max.Z	=	Vector3.TransformCoordinate( new Vector3(0,0, Math.Min( viewPos.Z - radius, znear )), projection ).Z;
			}
			else
			{
				min.Z	=	Math.Max( Math.Abs(viewPos.Z) - radius, znear );
				max.Z	=	Math.Max( Math.Abs(viewPos.Z) + radius, znear );
			}

			if (!r0)
			{
				return false;
			}

			return true;
		}


		static float sqrt( float x ) { return (float)Math.Sqrt(x); }
		static float square( float x ) { return x*x; }
		static float exp( float x ) { return (float)Math.Exp(x); }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="znear"></param>
		/// <param name="a"></param>
		/// <param name="z"></param>
		/// <param name="r"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		static bool GetSphereExtentAxis ( float znear, float a, float z, float r, out float min, out float max )
		{
			min = max = 0;

			if (z>r-znear)
			{
				return false;
			}

			var c		=	new Vector2( a, z );
			var t		=	sqrt( c.LengthSquared() - r * r );
			var cLen	=	c.Length();
	 		var cosT	=	t / cLen;
			var sinT	=	r / cLen;

			c.X /= cLen;
			c.Y /= cLen;

			var T		=	new Vector2( cosT * c.X - sinT * c.Y, +sinT * c.X + cosT * c.Y ) * t; 
			var B		=	new Vector2( cosT * c.X + sinT * c.Y, -sinT * c.X + cosT * c.Y ) * t; 

			var tau		=	new Vector2( a + sqrt( r*r - square(znear-z) ), znear );
			var beta	=	new Vector2( a - sqrt( r*r - square(znear-z) ), znear );

			var U		=	T.Y < znear ? T : tau;
			var L		=	B.Y < znear ? B : beta;

			max			=	U.X / U.Y * znear;
			min			=	L.X / L.Y * znear;

			return true;
		}
	}
}
