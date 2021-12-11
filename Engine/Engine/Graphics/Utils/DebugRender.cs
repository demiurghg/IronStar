using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;

namespace Fusion.Engine.Graphics
{
	public abstract class DebugRender : DisposableBase
	{
		public abstract void Submit();
		public abstract void PushVertex( DebugVertex v );
		public abstract void AddModel( DebugModel model );
		public abstract void RemoveModel( DebugModel model );

		/*-----------------------------------------------------------------------------------------
		 *	Primitives :
		-----------------------------------------------------------------------------------------*/

		public void DrawLine(Vector3 p0, Vector3 p1, Color color0, Color color1, float width0, float width1)
		{
			var a = new DebugVertex() { Pos = new Vector4(p0,width0), Color = color0.ToVector4() };
			var b = new DebugVertex() { Pos = new Vector4(p1,width1), Color = color1.ToVector4() };

			PushVertex( a );
			PushVertex( b );
		}


		public void DrawLine(Vector3 p0, Vector3 p1, Color color)
		{
			DrawLine( p0, p1, color, color, 0, 0 );
		}


		public void DrawLine(Vector3 p0, Vector3 p1, Color color0, Color color1)
		{
			DrawLine( p0, p1, color0, color1, 0, 0 );
		}


		public void DrawGrid()
		{
			var color = new Color(64,64,64,255);

			int gridsz = 128;
			for (int x = -gridsz; x <= gridsz; x += 4)
			{
				float w = (x==0) ? 2 : 1;
				DrawLine(new Vector3(x, 0, gridsz), new Vector3(x, 0, -gridsz), color, color, w, w);
				DrawLine(new Vector3(gridsz, 0, x), new Vector3(-gridsz, 0, x), color, color, w, w);
			}
		}


		public void DrawBasis(Matrix basis, float scale, float width=1)
		{
			Vector3 pos = Vector3.TransformCoordinate(Vector3.Zero, basis);
			Vector3 xaxis = Vector3.TransformNormal(Vector3.UnitX * scale, basis);
			Vector3 yaxis = Vector3.TransformNormal(Vector3.UnitY * scale, basis);
			Vector3 zaxis = Vector3.TransformNormal(Vector3.UnitZ * scale, basis);
			DrawLine(pos, pos + xaxis, Color.Red , Color.Red , width, width);
			DrawLine(pos, pos + yaxis, Color.Lime, Color.Lime, width, width);
			DrawLine(pos, pos + zaxis, Color.Blue, Color.Blue, width, width);
		}


		public void DrawVector(Vector3 origin, Vector3 dir, Color color, float scale = 1.0f)
		{
			DrawLine(origin, origin + dir * scale, color/*, Matrix.Identity*/ );
		}


		public void DrawPoint(Vector3 p, float size, Color color, float width=1)
		{
			float h = size / 2;	// half size
			DrawLine(p + Vector3.UnitX * h, p - Vector3.UnitX * h, color, color, width, width);
			DrawLine(p + Vector3.UnitY * h, p - Vector3.UnitY * h, color, color, width, width);
			DrawLine(p + Vector3.UnitZ * h, p - Vector3.UnitZ * h, color, color, width, width);
		}


		public void DrawWaypoint(Vector3 p, float size, Color color, int width = 1 )
		{
			float h = size / 2;	// half size
			DrawLine( p + Vector3.UnitX * h, p - Vector3.UnitX * h, color, color, width, width );
			DrawLine( p + Vector3.UnitZ * h, p - Vector3.UnitZ * h, color, color, width, width );
		}


		public void DrawRing(Vector3 origin, float radius, Color color, int numSegments = 32, float angle = 0)
		{
			int N = numSegments;
			Vector3[] points = new Vector3[N + 1];

			for (int i = 0; i <= N; i++)
			{
				points[i].X = origin.X + radius * (float)Math.Cos(Math.PI * 2 * i / N + angle);
				points[i].Y = origin.Y;
				points[i].Z = origin.Z + radius * (float)Math.Sin(Math.PI * 2 * i / N + angle);
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color);
			}
		}


		public void DrawCylinder(Vector3 origin, float radius, float height, Color color, int numSegments = 32, float angle = 0)
		{
			int N = numSegments;
			Vector3[] pointsTop		= new Vector3[N + 1];
			Vector3[] pointsBottom	= new Vector3[N + 1];

			for (int i = 0; i <= N; i++)
			{
				pointsTop[i].X		= origin.X + radius * (float)Math.Cos(Math.PI * 2 * i / N + angle);
				pointsTop[i].Y		= origin.Y + height/2;
				pointsTop[i].Z		= origin.Z + radius * (float)Math.Sin(Math.PI * 2 * i / N + angle);

				pointsBottom[i].X	= origin.X + radius * (float)Math.Cos(Math.PI * 2 * i / N + angle);
				pointsBottom[i].Y	= origin.Y - height/2;
				pointsBottom[i].Z	= origin.Z + radius * (float)Math.Sin(Math.PI * 2 * i / N + angle);
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine( pointsTop[i]		, pointsTop		[i + 1]	, color);
				DrawLine( pointsBottom[i]	, pointsBottom	[i + 1]	, color);
				DrawLine( pointsTop[i]		, pointsBottom	[i]		, color);
			}
		}


		public void DrawAxialRing ( Vector3 origin, Vector3 axis, float radius, Color color )
		{
			axis	=	Vector3.Normalize( axis );
			var rt	=	Vector3.Cross( axis, Vector3.Up );	

			if (rt.LengthSquared()<0.001f) {
				rt	=	Vector3.Cross( axis, Vector3.Right );
			}
			rt.Normalize();

			var up	=	Vector3.Cross( rt, axis );
			up.Normalize();

			int N = 12;
			Vector3[] points = new Vector3[N + 1];

			for (int i = 0; i <= N; i++)
			{
				float c =  radius * (float)Math.Cos(Math.PI * 2 * i / N + 0);
				float s =  radius * (float)Math.Sin(Math.PI * 2 * i / N + 0);

				points[i] = origin + up * s + rt * c;
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color);
			}
		}


		public void DrawSphere(Vector3 origin, float radius, Color color, int numSegments = 32)
		{
			int N = numSegments;
			Vector3[] points = new Vector3[N + 1];

			for (int i = 0; i <= N; i++)
			{
				points[i].X = origin.X + radius * (float)Math.Cos(Math.PI * 2 * i / N);
				points[i].Y = origin.Y;
				points[i].Z = origin.Z + radius * (float)Math.Sin(Math.PI * 2 * i / N);
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color);
			}

			for (int i = 0; i <= N; i++)
			{
				points[i].X = origin.X + radius * (float)Math.Cos(Math.PI * 2 * i / N);
				points[i].Y = origin.Y + radius * (float)Math.Sin(Math.PI * 2 * i / N);
				points[i].Z = origin.Z;
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color);
			}

			for (int i = 0; i <= N; i++)
			{
				points[i].X = origin.X;
				points[i].Y = origin.Y + radius * (float)Math.Cos(Math.PI * 2 * i / N);
				points[i].Z = origin.Z + radius * (float)Math.Sin(Math.PI * 2 * i / N);
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color);
			}
		}


		public void DrawRing(Matrix basis, float radius, Color color, int numSegments = 32, float width=0, float stretch = 1)
		{
			int N = numSegments;
			Vector3[] points = new Vector3[N + 1];
			Vector3 origin = basis.TranslationVector;

			for (int i = 0; i <= N; i++)
			{
				points[i] = origin + radius * basis.Forward * (float)Math.Cos(Math.PI * 2 * i / N) * stretch
									+ radius * basis.Left * (float)Math.Sin(Math.PI * 2 * i / N);
			}

			for (int i = 0; i < N; i++)
			{
				DrawLine(points[i], points[i + 1], color, color, width, width);
			}
		}


		public void DrawFrustum ( BoundingFrustum frustum, Color color, float scale = 1.0f, float width = 1 )
		{
			var points = frustum.GetCorners();

			for (int i=0; i<4; i++)
			{
				points[i+4] = Vector3.Lerp( points[i], points[i+4], scale );
			}

			DrawLine( points[0], points[1], color, color, width, width );
			DrawLine( points[1], points[2], color, color, width, width );
			DrawLine( points[2], points[3], color, color, width, width );
			DrawLine( points[3], points[0], color, color, width, width );

			DrawLine( points[4], points[5], color, color, width, width );
			DrawLine( points[5], points[6], color, color, width, width );
			DrawLine( points[6], points[7], color, color, width, width );
			DrawLine( points[7], points[4], color, color, width, width );

			DrawLine( points[0], points[4], color, color, width, width );
			DrawLine( points[1], points[5], color, color, width, width );
			DrawLine( points[2], points[6], color, color, width, width );
			DrawLine( points[3], points[7], color, color, width, width );

			//	dr.DrawLine( points[0], points[1], dispColor );
			//	dr.DrawLine( points[1], points[2], dispColor );
			//	dr.DrawLine( points[2], points[3], dispColor );
			//	dr.DrawLine( points[3], points[0], dispColor );

			//	dr.DrawLine( points[4], points[5], dispColor );
			//	dr.DrawLine( points[5], points[6], dispColor );
			//	dr.DrawLine( points[6], points[7], dispColor );
			//	dr.DrawLine( points[7], points[4], dispColor );

			//	dr.DrawLine( points[0], points[4], dispColor );
			//	dr.DrawLine( points[1], points[5], dispColor );
			//	dr.DrawLine( points[2], points[6], dispColor );
			//	dr.DrawLine( points[3], points[7], dispColor );
		}


		public void DrawBox( Vector3 center, float w, float h, float d, Color color )
		{
			var hszv	=	new Vector3(w/2,h/2,d/2);
			DrawBox( new BoundingBox( center-hszv, center+hszv), color );
		}


		public void DrawBox( Vector3 center, float size, Color color )
		{
			DrawBox( center, size, size, size, color );
		}


		public void DrawBox(BoundingBox bbox, Color color)
		{
			var crnrs = bbox.GetCorners();

			var p = bbox.Maximum;
			var n = bbox.Minimum;

			DrawLine(new Vector3(p.X, p.Y, p.Z), new Vector3(n.X, p.Y, p.Z), color);
			DrawLine(new Vector3(n.X, p.Y, p.Z), new Vector3(n.X, p.Y, n.Z), color);
			DrawLine(new Vector3(n.X, p.Y, n.Z), new Vector3(p.X, p.Y, n.Z), color);
			DrawLine(new Vector3(p.X, p.Y, n.Z), new Vector3(p.X, p.Y, p.Z), color);

			DrawLine(new Vector3(p.X, n.Y, p.Z), new Vector3(n.X, n.Y, p.Z), color);
			DrawLine(new Vector3(n.X, n.Y, p.Z), new Vector3(n.X, n.Y, n.Z), color);
			DrawLine(new Vector3(n.X, n.Y, n.Z), new Vector3(p.X, n.Y, n.Z), color);
			DrawLine(new Vector3(p.X, n.Y, n.Z), new Vector3(p.X, n.Y, p.Z), color);

			DrawLine(new Vector3(p.X, p.Y, p.Z), new Vector3(p.X, n.Y, p.Z), color);
			DrawLine(new Vector3(n.X, p.Y, p.Z), new Vector3(n.X, n.Y, p.Z), color);
			DrawLine(new Vector3(n.X, p.Y, n.Z), new Vector3(n.X, n.Y, n.Z), color);
			DrawLine(new Vector3(p.X, p.Y, n.Z), new Vector3(p.X, n.Y, n.Z), color);
		}


		public void DrawBox(BoundingBox bbox, Matrix transform, Color color, float width = 1 )
		{
			var crnrs = bbox.GetCorners();

			//Vector3.TransformCoordinate( crnrs, ref transform, crnrs );

			var p = bbox.Maximum;
			var n = bbox.Minimum;

			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, p.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, p.Y, p.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, p.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, p.Y, n.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, p.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, p.Y, n.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, p.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, p.Y, p.Z), transform), color, color, width, width );
																																								
			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, n.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, n.Y, p.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, n.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, n.Y, n.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, n.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, n.Y, n.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, n.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, n.Y, p.Z), transform), color, color, width, width );
																																								
			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, p.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, n.Y, p.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, p.Y, p.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, n.Y, p.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(n.X, p.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(n.X, n.Y, n.Z), transform), color, color, width, width );
			DrawLine(Vector3.TransformCoordinate(new Vector3(p.X, p.Y, n.Z), transform), Vector3.TransformCoordinate(new Vector3(p.X, n.Y, n.Z), transform), color, color, width, width );
		}
	}
}
