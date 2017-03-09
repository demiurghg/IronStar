﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Lights {

	public static class Extents {

		class Line {
			public Line ( Vector3 a, Vector3 b ) { A = a; B = b; }
			public Vector3 A;
			public Vector3 B;
			
			/// <summary>
			/// Returns true if line is visible
			/// </summary>
			/// <param name="znear"></param>
			/// <returns></returns>
			public bool Clip ( float znear ) 
			{
				if ( A.Z <= znear && B.Z <= znear ) {
					return true;
				}
				if ( A.Z >= znear && B.Z >= znear ) {
					return false;
				}

				var factor	=	( znear - A.Z ) / ( B.Z - A.Z );
				var point	=	Vector3.Lerp( A, B, factor );
				
				if ( A.Z > znear ) A = point;
				if ( B.Z > znear ) B = point;

				return true;
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
		public static bool GetFrustumExtent ( Matrix view, Matrix projection, Rectangle viewport, BoundingFrustum frustum, out Vector4 min, out Vector4 max )
		{
			min = max	=	Vector4.Zero;

			var znear	=	projection.M34 * projection.M43 / projection.M33;
			
			var viewPoints = frustum.GetCorners()
					.Select( p0 => Vector3.TransformCoordinate( p0, view ) )
					.ToArray();

			var lines = new[]{
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

			if (!lines.Any()) {
				return false;
			}

			var projPoints = new List<Vector3>();
			
			foreach ( var line in lines ) {
				projPoints.Add( Vector3.TransformCoordinate( line.A, projection ) );
				projPoints.Add( Vector3.TransformCoordinate( line.B, projection ) );
			}

			min.X	=	projPoints.Min( p => p.X );
			min.Y	=	projPoints.Max( p => p.Y );
			min.Z	=	projPoints.Min( p => p.Z );

			max.X	=	projPoints.Max( p => p.X );
			max.Y	=	projPoints.Min( p => p.Y );
			max.Z	=	projPoints.Max( p => p.Z );

			min.X	=	( min.X *  0.5f + 0.5f ) * viewport.Width;
			min.Y	=	( min.Y * -0.5f + 0.5f ) * viewport.Height;

			max.X	=	( max.X *  0.5f + 0.5f ) * viewport.Width;
			max.Y	=	( max.Y * -0.5f + 0.5f ) * viewport.Height;

			return true;
		} 



		static Vector3 ProjectPoint ( ref Vector3 point, ref Matrix proj, bool skipZ )
		{
			var pp = Vector3.TransformCoordinate( point, proj );

			if (skipZ) {
				return new Vector3( pp.X, pp.Y, point.Z );
			} else {
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
		public static bool GetBasisExtent ( Matrix view, Matrix projection, Rectangle viewport, Matrix basis, bool projectZ, out Vector4 min, out Vector4 max )
		{
			min = max	=	Vector4.Zero;

			var znear	=	projection.M34 * projection.M43 / projection.M33;
			
			var viewPoints = new Vector3[8];

			viewPoints[0]	=	basis.TranslationVector + basis.Right + basis.Up + basis.Backward;
			viewPoints[1]	=	basis.TranslationVector - basis.Right + basis.Up + basis.Backward;
			viewPoints[2]	=	basis.TranslationVector - basis.Right - basis.Up + basis.Backward;
			viewPoints[3]	=	basis.TranslationVector + basis.Right - basis.Up + basis.Backward;

			viewPoints[4]	=	basis.TranslationVector + basis.Right + basis.Up - basis.Backward;
			viewPoints[5]	=	basis.TranslationVector - basis.Right + basis.Up - basis.Backward;
			viewPoints[6]	=	basis.TranslationVector - basis.Right - basis.Up - basis.Backward;
			viewPoints[7]	=	basis.TranslationVector + basis.Right - basis.Up - basis.Backward;

			for (int i=0; i<viewPoints.Length; i++) {
				viewPoints[i] = Vector3.TransformCoordinate( viewPoints[i], view );
			}

			var lines = new[]{
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

			if (!lines.Any()) {
				return false;
			}

			var projPoints = new List<Vector3>();
			
			foreach ( var line in lines ) {
				projPoints.Add( ProjectPoint( ref line.A, ref projection, !projectZ ) );
				projPoints.Add( ProjectPoint( ref line.B, ref projection, !projectZ ) );
			}

			min.X	=	projPoints.Min( p => p.X );
			min.Y	=	projPoints.Max( p => p.Y );
			min.Z	=	projPoints.Min( p => p.Z );

			max.X	=	projPoints.Max( p => p.X );
			max.Y	=	projPoints.Min( p => p.Y );
			max.Z	=	projPoints.Max( p => p.Z );

			min.X	=	( min.X *  0.5f + 0.5f ) * viewport.Width;
			min.Y	=	( min.Y * -0.5f + 0.5f ) * viewport.Height;

			max.X	=	( max.X *  0.5f + 0.5f ) * viewport.Width;
			max.Y	=	( max.Y * -0.5f + 0.5f ) * viewport.Height;

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
		public static bool GetSphereExtent ( Matrix view, Matrix projection, Vector3 position, Rectangle vp, float radius, bool projectZ, out Vector4 min, out Vector4 max )
		{
			min = max	=	Vector4.Zero;

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

			if (projectZ) {
				min.Z	=	Vector3.TransformCoordinate( new Vector3(0,0, Math.Min( viewPos.Z + radius, znear )), projection ).Z;
				max.Z	=	Vector3.TransformCoordinate( new Vector3(0,0, Math.Min( viewPos.Z - radius, znear )), projection ).Z;
			} else {
				min.Z	=	Math.Min( viewPos.Z + radius, znear );
				max.Z	=	Math.Min( viewPos.Z - radius, znear );
			}

			if (!r0) {
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

			if (z>r-znear) {
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
