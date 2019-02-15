using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics {
	public static class Rasterizer {

		

		public static void RasterizeTriangle ( Vector2 vt1, Vector2 vt2, Vector2 vt3, Action<Int2,float,float> interpolate )
		{
			int maxX = (int)( Math.Ceiling	(Math.Max(vt1.X, Math.Max(vt2.X, vt3.X))) );
			int minX = (int)( Math.Floor	(Math.Min(vt1.X, Math.Min(vt2.X, vt3.X))) );
			int maxY = (int)( Math.Ceiling	(Math.Max(vt1.Y, Math.Max(vt2.Y, vt3.Y))) );
			int minY = (int)( Math.Floor	(Math.Min(vt1.Y, Math.Min(vt2.Y, vt3.Y))) );

			Vector2 vs1 = new Vector2(vt2.X - vt1.X, vt2.Y - vt1.Y);
			Vector2 vs2 = new Vector2(vt3.X - vt1.X, vt3.Y - vt1.Y);

			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					Vector2 q = new Vector2(x - vt1.X + 0.5f, y - vt1.Y + 0.5f);

					float s = crossProduct(q, vs2) / crossProduct(vs1, vs2);
					float t = crossProduct(vs1, q) / crossProduct(vs1, vs2);

					if ( (s >= 0) && (t >= 0) && (s + t <= 1))
					{
						interpolate( new Int2(x,y), s, t );
					}
				}
			}
		}


		static float crossProduct ( Vector2 a, Vector2 b )
		{
			return a.X * b.Y - a.Y * b.X;
		}

		
	}
}
