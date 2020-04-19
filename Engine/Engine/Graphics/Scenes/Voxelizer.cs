using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Scenes 
{
	public static class Voxelizer 
	{

		public static void RasterizeTriangle ( Vector3 p0, Vector3 p1, Vector3 p2, float step, Action<Vector3> draw )
		{
			var v01	=	p1 - p0;
			var v02	=	p2 - p0;

			var n	=	Vector3.Cross( v01, v02 );

			var nn	=	Vector3.Normalize( n );

			var absX	=	Math.Abs( nn.X );
			var absY	=	Math.Abs( nn.Y );
			var absZ	=	Math.Abs( nn.Z );

			if ( absX >= absY && absX >= absZ ) {
				RasterizeXZ( p0, p1, p2, step, v=>v.Y, v=>v.Z, draw );
			} else
			if ( absY >= absX && absY >= absZ ) {
				RasterizeXZ( p0, p1, p2, step, v=>v.X, v=>v.Z, draw );
			} else
			if ( absZ >= absX && absZ >= absY ) {
				RasterizeXZ( p0, p1, p2, step, v=>v.X, v=>v.Y, draw );
			}
		}


		static void RasterizeXZ ( Vector3 p0, Vector3 p1, Vector3 p2, float step, Func<Vector3,float> axisA, Func<Vector3,float> axisB, Action<Vector3> draw )
		{
			var v01		=	p1 - p0;
			var v02		=	p2 - p0;

			var vp01	=	new Vector2( axisA( v01 ), axisB( v01 ) );
			var vp02	=	new Vector2( axisA( v02 ), axisB( v02 ) );

			float x0	=	(float)Math.Floor( MathUtil.Min3( axisA(p0), axisA(p1), axisA(p2) ) ) - step;
			float x1	=	(float)Math.Floor( MathUtil.Max3( axisA(p0), axisA(p1), axisA(p2) ) ) + step;
			float y0	=	(float)Math.Floor( MathUtil.Min3( axisB(p0), axisB(p1), axisB(p2) ) ) - step;
			float y1	=	(float)Math.Floor( MathUtil.Max3( axisB(p0), axisB(p1), axisB(p2) ) ) + step;

			for ( float x = x0; x <= x1; x += step ) {
				for ( float y = y0; y <= y1; y += step ) {

					float dx = - axisA(p0);
					float dy = - axisB(p0);

					float a, b;
					bool sln = SolveEq2( vp01.X, vp02.X, x+dx,  vp01.Y, vp02.Y, y+dy,  out a, out b  );

					if (!sln) {
						continue;
					}

					//Console.Write("{0} {0}|", a, b );
					Debug.Assert( !float.IsNaN( a ) && !float.IsInfinity( a ) );
					Debug.Assert( !float.IsNaN( b ) && !float.IsInfinity( b ) );

					float e = 0.01f;//-0.05f;
					if (a<0-e || b<0-e || a+b>1+e ) {
						continue;
					}

					Vector3 p = p0 + (v01 * a) + (v02 * b);

					draw( p );
				}
			}

		}



		/// <summary>
		/// ax + by = c
		/// dx + ey = f
		/// </summary>
		static bool SolveEq2 ( float a, float b, float c, float d, float e, float f, out float x, out float y )
		{
			x = y = float.NaN;
			float div = ( a * e - b * d );
			if ( Math.Abs(div) < float.Epsilon ) {
				return false;
			}
			y = ( a * f - c * d ) / div;
			x = ( c * e - b * f ) / div;
			return true;
		}

		
	}
}
