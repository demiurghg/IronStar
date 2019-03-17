using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public static class Rasterizer {

		

		public static void RasterizeTriangle ( Vector2 A, Vector2 B, Vector2 C, Action<Int2,float,float> interpolate )
		{
			int maxX = (int)( Math.Ceiling	(Math.Max(A.X, Math.Max(B.X, C.X))) );
			int minX = (int)( Math.Floor	(Math.Min(A.X, Math.Min(B.X, C.X))) );
			int maxY = (int)( Math.Ceiling	(Math.Max(A.Y, Math.Max(B.Y, C.Y))) );
			int minY = (int)( Math.Floor	(Math.Min(A.Y, Math.Min(B.Y, C.Y))) );

			Vector2 ab	=	new Vector2(B.X - A.X, B.Y - A.Y);
			Vector2 ac	=	new Vector2(C.X - A.X, C.Y - A.Y);

			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					Vector2 q = new Vector2(x - A.X + 0.5f, y - A.Y + 0.5f);

					float s = crossProduct(q, ac) / crossProduct(ab, ac);
					float t = crossProduct(ab, q) / crossProduct(ab, ac);

					if ( (s >= 0) && (t >= 0) && (s + t <= 1))
					{
						interpolate( new Int2(x,y), s, t );
					}
				}
			}
		}



		public static void RasterizeTriangleConservative ( Vector2 A, Vector2 B, Vector2 C, Action<Int2,float,float,bool> interpolate )
		{
			/*RasterizeLineDDA ( A, B, (p,f) => interpolate(p,   f, 0) );
			RasterizeLineDDA ( A, C, (p,f) => interpolate(p,   0, f) );
			RasterizeLineDDA ( B, C, (p,f) => interpolate(p, 1-f, f) );*/

			int maxX = (int)( Math.Ceiling	(Math.Max(A.X, Math.Max(B.X, C.X))) ) + 1;
			int minX = (int)( Math.Floor	(Math.Min(A.X, Math.Min(B.X, C.X))) ) - 1;
			int maxY = (int)( Math.Ceiling	(Math.Max(A.Y, Math.Max(B.Y, C.Y))) ) + 1;
			int minY = (int)( Math.Floor	(Math.Min(A.Y, Math.Min(B.Y, C.Y))) ) - 1;

			Vector2 ab	=	new Vector2(B.X - A.X, B.Y - A.Y);
			Vector2 ac	=	new Vector2(C.X - A.X, C.Y - A.Y);

			float eps = 1 / 4096.0f;

			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					var pixel	=	new RectangleF( x - eps, y - eps, 1f + 2*eps, 1f + 2*eps );

					var q		=	new Vector2(x - A.X + 0.5f, y - A.Y + 0.5f);

					var s		=	crossProduct(q, ac) / crossProduct(ab, ac);
					var t		=	crossProduct(ab, q) / crossProduct(ab, ac);

					if ( (s >= 0) && (t >= 0) && (s + t <= 1) )
					{
						interpolate( new Int2(x,y), s, t, true );
					}
					else if ( pixel.Contains( A ) )
					{
						interpolate( new Int2(x,y), s, t, false );
					}
					else if ( pixel.Contains( B ) ) 
					{
						interpolate( new Int2(x,y), s, t, false );
					}
					else if ( pixel.Contains( C ) ) 
					{
						interpolate( new Int2(x,y), s, t, false );
					}
					else
					{
						if (   LineRectangleIntersection( pixel, A, B ) 
							|| LineRectangleIntersection( pixel, B, C ) 
							|| LineRectangleIntersection( pixel, C, A ) )
						{
							interpolate( new Int2(x,y), s, t, false );
						}
					}
				}
			}
		}



		static bool LineRectangleIntersection ( RectangleF rect, Vector2 a, Vector2 b )
		{
			Vector2 r;

			var p0 = rect.BottomLeft;
			var p1 = rect.BottomRight;
			var p2 = rect.TopRight;
			var p3 = rect.TopLeft;

			return	LineIntersectsLine( a, b, p0, p1, out r )
				||	LineIntersectsLine( a, b, p1, p2, out r )
				||	LineIntersectsLine( a, b, p2, p3, out r )
				||	LineIntersectsLine( a, b, p3, p0, out r );
		}



		// a1 is line1 start, a2 is line1 end, b1 is line2 start, b2 is line2 end
		static bool LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection)
		{
			intersection = Vector2.Zero;

			Vector2 b = a2 - a1;
			Vector2 d = b2 - b1;
			float bDotDPerp = b.X * d.Y - b.Y * d.X;

			// if b dot d == 0, it means the lines are parallel so have infinite intersection points
			if (bDotDPerp == 0) {
				return false;
			}

			Vector2 c = b1 - a1;
			float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
			
			if (t < 0 || t > 1) {
				return false;
			}

			float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
			
			if (u < 0 || u > 1) {
				return false;
			}

			intersection = a1 + t * b;

			return true;
		}

		
		static float floor ( float x ) { return (float)Math.Floor(x); }
		static float ceil ( float x ) { return (float)Math.Floor(x); }
		static float fabs ( float x ) { return Math.Abs(x); }


		static Int2 roundPoint ( Vector2 p )
		{
			return new Int2( (int)(p.X), (int)(p.Y) );
		}


		static float crossProduct ( Vector2 a, Vector2 b )
		{
			return a.X * b.Y - a.Y * b.X;
		}

		
	}
}
