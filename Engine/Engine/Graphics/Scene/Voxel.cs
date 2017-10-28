using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public class Voxel {

		public readonly int Width;
		public readonly int Height;
		public readonly int Depth;

		public BoundingBox BoundingBox;

		readonly byte[] data;




		public Voxel ( int width, int height, int depth )
		{
			if (width<0) throw new ArgumentOutOfRangeException("width");
			if (height<0) throw new ArgumentOutOfRangeException("height");
			if (depth<0) throw new ArgumentOutOfRangeException("depth");

			Width	=	width;
			Height	=	height;
			Depth	=	depth;

			data	=	new byte[ Width * Height * Depth ];
		}



		int Address ( int x, int y, int z )
		{
			return x + y * Width + z * Width * Height;
		}


		public byte Get( int x, int y, int z )
		{
			return data[ Address(x,y,z) ];
		}


		public void Set( int x, int y, int z, byte value )
		{
			if (x<0) return;
			if (y<0) return;
			if (z<0) return;
			if (x>=Width) return;
			if (y>=Height) return;
			if (z>=Depth) return;

			data[ Address(x,y,z) ] = value;
		}



		public void RasterizeTriangle ( Vector3 p0, Vector3 p1, Vector3 p2, byte value )
		{
			var v01	=	p1 - p0;
			var v02	=	p2 - p0;

			var n	=	Vector3.Cross( v01, v02 );

			var nn	=	Vector3.Normalize( n );

			var absX	=	Math.Abs( nn.X );
			var absY	=	Math.Abs( nn.Y );
			var absZ	=	Math.Abs( nn.Z );

			int axis	=	0;

			if ( absX > absY && absX > absZ ) {
				axis = 0;
				return;
			}
			if ( absY > absX && absY > absZ ) {
				axis = 1;
				return;
			}
			if ( absZ > absX && absZ > absY ) {
				axis = 2;
			}

			RasterizeY( p0, p1, p2, value, 1 );
		}



		public void RasterizeY ( Vector3 p0, Vector3 p1, Vector3 p2, byte value, float gridStep = 1 )
		{
			var v01		=	p1 - p0;
			var v02		=	p2 - p0;

			var vp01	=	new Vector3( v01.X, 0, v02.Z );
			var vp02	=	new Vector3( v02.X, 0, v02.Z );

			float x0	=	(float)Math.Floor( Min3( p0.X, p1.X, p2.X ) / gridStep ) * gridStep;
			float x1	=	(float)Math.Floor( Max3( p0.X, p1.X, p2.X ) / gridStep ) * gridStep;
			float z0	=	(float)Math.Floor( Min3( p0.Z, p1.Z, p2.Z ) / gridStep ) * gridStep;
			float z1	=	(float)Math.Floor( Max3( p0.Z, p1.Z, p2.Z ) / gridStep ) * gridStep;

			for ( float x = x0; x <= x1; x += gridStep ) {
				for ( float z = z0; z <= z1; z += gridStep ) {

					float dx = /*jitter * (float)(rand.NextDouble()*2-1)*/ - p0.X;
					float dz = /*jitter * (float)(rand.NextDouble()*2-1)*/ - p0.Z;

					float a, b;
					bool sln = SolveEq2( vp01.X, vp02.X, x+dx,  vp01.Z, vp02.Z, z+dz,  out a, out b  );

					if (!sln) {
						continue;
					}

					//Console.Write("{0} {0}|", a, b );
					Debug.Assert( !float.IsNaN( a ) && !float.IsInfinity( a ) );
					Debug.Assert( !float.IsNaN( b ) && !float.IsInfinity( b ) );

					float e = 0.01f;//-0.05f;
					if (a<0-e || b<0-e || a+b>1+e ) continue;

					Vector3 p = p0 + (v01 * a) + (v02 * b);

					Set( (int)p.X, (int)p.Y, (int)p.Z, value );
				}
			}

		}


		float Max3 ( float a, float b, float c ) { return Math.Max( a, Math.Max( b, c ) ); }
		float Min3 ( float a, float b, float c ) { return Math.Min( a, Math.Min( b, c ) ); }

		/// <summary>
		/// ax + by = c
		/// dx + ey = f
		/// </summary>
		bool SolveEq2 ( float a, float b, float c, float d, float e, float f, out float x, out float y )
		{
			x = y = float.NaN;
			float div = ( a * e - b * d );
			if ( Math.Abs(div) < float.Epsilon ) {
				return false;
			}
			y = ( a * f - c * d ) / ( a * e - b * d );
			x = ( c * e - b * f ) / ( a * e - b * d );
			return true;
		}

	}
}
