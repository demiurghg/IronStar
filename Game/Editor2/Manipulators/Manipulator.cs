using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion;
using IronStar.Mapping;

namespace IronStar.Editor2.Manipulators {
	public abstract class Manipulator {

		readonly protected RenderSystem rs;
		readonly protected Game game;
		readonly protected MapEditor editor;

		public abstract bool IsManipulating { get; }


		/// <summary>
		/// Constrcutor
		/// </summary>
		public Manipulator ( MapEditor editor )
		{
			this.rs		=	editor.Game.RenderSystem;
			this.game	=	editor.Game;
			this.editor	=	editor;
		}


		public abstract bool StartManipulation ( int x, int y );
		public abstract void UpdateManipulation ( int x, int y );
		public abstract void StopManipulation ( int x, int y );
		public abstract void Update ( GameTime gameTime, int x, int y );
		public abstract string ManipulationText { get; }



		/// <summary>
		/// Draw standard arrow.
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="dir"></param>
		/// <param name="color"></param>
		/// <param name="length"></param>
		/// <param name="scale"></param>
		protected void DrawArrow ( DebugRender dr, Ray pickRay, Vector3 origin, Vector3 dir, Color color )
		{
			var p0 = origin;
			var p1 = p0 + dir * editor.camera.PixelToWorldSize( origin, 90 );
			var p2 = p1 + dir * editor.camera.PixelToWorldSize( origin, 20 );

			dr.DrawLine(p0,p1, color, color, 2,2 );
			dr.DrawLine(p1,p2, color, color, 9,1 );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="pickRay"></param>
		/// <param name="origin"></param>
		/// <param name="axisA"></param>
		/// <param name="axisB"></param>
		/// <param name="color"></param>
		protected void DrawRing ( DebugRender dr, Ray pickRay, Vector3 origin, Vector3 axis, Color color, float size = 90)
		{
				axis	=	Vector3.Normalize( axis );
			var axisA	=	Vector3.Cross( axis, Vector3.Up ).Normalized();	

			if (axisA.LengthSquared()<0.001f) {
				axisA	=	Vector3.Cross( axis, Vector3.Right ).Normalized();
			}

			var axisB	=	Vector3.Cross( axisA, axis ).Normalized();

			int N = 64;
			Vector3[] points = new Vector3[N + 1];

			var radius = editor.camera.PixelToWorldSize(origin, size);

			for (int i = 0; i <= N; i++)
			{
				var p = origin;
				p += axisA * radius * (float)Math.Cos(Math.PI * 2 * i / N);
				p += axisB * radius * (float)Math.Sin(Math.PI * 2 * i / N);
				points[i] = p;
			}

			for (int i = 0; i < N; i++)
			{
				dr.DrawLine(points[i], points[i + 1], color, color, 2, 2);
			}
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="dir"></param>
		/// <param name="pickPoint"></param>
		/// <param name="hitPoint"></param>
		/// <returns></returns>
		protected HandleIntersection IntersectArrow ( Vector3 origin, Vector3 dir, Point pickPoint )
		{
			var length		=	editor.camera.PixelToWorldSize(origin, 110);
			var tolerance	=	editor.camera.PixelToWorldSize(origin, 7);
			var arrowRay	=	new Ray( origin, dir * length);
			var pickRay		=	editor.camera.PointToRay( pickPoint.X, pickPoint.Y );

			Vector3 temp, hitPoint;
			float t1, t2;

			var dist = Utils.RayIntersectsRay(ref pickRay, ref arrowRay, out temp, out hitPoint, out t1, out t2 );

			var pickDistance = Vector3.Distance( hitPoint, pickRay.Position );

			if ( (dist < tolerance) && (t2 > 0) && (t2 < 1) && (t1 > 0)) {
				return new HandleIntersection( true, pickDistance, dist, hitPoint );
			} else {
				return new HandleIntersection( false, pickDistance, dist, hitPoint );
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="dir"></param>
		/// <param name="pickPoint"></param>
		/// <param name="hitPoint"></param>
		/// <returns></returns>
		protected HandleIntersection IntersectRing ( Vector3 origin, Vector3 axis, Point pickPoint, float size = 90 )
		{
			var radius		=	editor.camera.PixelToWorldSize(origin, size);
			var tolerance	=	editor.camera.PixelToWorldSize(origin, 7);
			var pickRay		=	editor.camera.PointToRay( pickPoint.X, pickPoint.Y );

			var plane		=	new Plane( origin, axis );

			Vector3 hitPoint;

			if ( plane.Intersects( ref pickRay, out hitPoint ) ) {

				var originHitPointDistance	=	Vector3.Distance( origin, hitPoint );
				var pickDistance			=	Vector3.Distance( hitPoint, pickRay.Position );

				var hitRing	=	(originHitPointDistance > radius - tolerance) && (originHitPointDistance < radius + tolerance);

				return new HandleIntersection( hitRing, pickDistance, 0, hitPoint );
				
			} else {
				return new HandleIntersection( false, float.PositiveInfinity, float.PositiveInfinity, Vector3.Zero );
			}

		}




		public float Snap ( float value, float snapValue )
		{
			return (float)(Math.Round( value / snapValue ) * snapValue);
		}


		public float Snap ( float value, float snapValue, bool enable )
		{
			if (enable) {
				return (float)(Math.Round( value / snapValue ) * snapValue);
			} else {
				return value;
			}
		}


		public Vector3 Snap ( Vector3 value, float snapValue )
		{
			return new Vector3( Snap( value.X, snapValue ), Snap( value.Y, snapValue ), Snap( value.Z, snapValue ) );
		}
	}
}
