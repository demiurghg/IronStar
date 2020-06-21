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
using Fusion.Drivers.Graphics;
using Fusion.Core.Extensions;
using Fusion.Engine.Audio;

namespace IronStar.Editor {
	public class EditorCamera {

		readonly RenderSystem rs;
		readonly SoundSystem ss;
		readonly Game game;
		readonly MapEditor editor;


		public Vector3	Target		=	Vector3.Zero;
		public float	Distance	=	120;
		public float	Yaw			=	45;
		public float	Pitch		=	-30;
		public float	Fov			=	90;

		Manipulation	manipulation	=	Manipulation.None;
		Point			startPoint;
		float			addYaw;
		float			addPitch;
		float			addZoom = 1;
		float			addTX = 0;
		float			addTY = 0;


		public Manipulation Manipulation {
			get {
				return manipulation;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public EditorCamera ( MapEditor editor )
		{
			this.rs		=	editor.Game.RenderSystem;
			this.ss		=	editor.Game.GetService<SoundSystem>();
			this.game	=	editor.Game;
			this.editor	=	editor;
		}



		public float PixelToWorldSize ( Vector3 point, float pixelSize )
		{
			var view	=	GetViewMatrix();
			var tpoint	=	Vector3.TransformCoordinate( point, view );
			var fovTan	=	(float)Math.Tan(MathUtil.DegreesToRadians(Fov/2));
			var vp		=	rs.DisplayBounds;

			return 2 * pixelSize / vp.Width * fovTan * Math.Abs(tpoint.Z);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		Matrix GetViewMatrix ()
		{
			if (editor.LockAzimuth) {
				Yaw = 0;
				addYaw = 0;
			}

			var yaw		=	MathUtil.DegreesToRadians( Yaw + addYaw );
			var pitch	=	MathUtil.DegreesToRadians( MathUtil.Clamp(Pitch + addPitch, -89, 89) );

			var offset	=	Matrix.RotationYawPitchRoll( yaw, pitch, 0 );

			var transZ	=	offset.Backward * Distance * addZoom;

			var transXY	=	offset.Right * Distance * addTX
						+	offset.Up * Distance * addTY
						;

			var view	=	Matrix.LookAtRH( Target + transZ + transXY, Target + transXY, Vector3.Up );

			return view;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="point"></param>
		/// <param name="rect"></param>
		/// <returns></returns>
		public bool IsInRectangle ( Vector3 point, Rectangle rect )
		{
			var vp		=	rs.DisplayBounds;
			var view	=	rs.RenderWorld.Camera.ViewMatrix;
			var proj	=	rs.RenderWorld.Camera.ProjectionMatrix;
			var vPoint	=	Vector3.TransformCoordinate( point, GetViewMatrix() );

			if (vPoint.Z>0) {
				return false;
			}

			var pPoint	=	Vector4.Transform( new Vector4( vPoint, 1 ), proj );

			pPoint.X /= pPoint.W;
			pPoint.Y /= pPoint.W;

			var x	=	(int)((pPoint.X * ( 0.5f) + 0.5) * vp.Width);
			var y	=	(int)((pPoint.Y * (-0.5f) + 0.5) * vp.Height);

			return rect.Contains( x, y );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
			Fov			=	editor.CameraFov;

			var view	=	GetViewMatrix();

			var vp		=	rs.DisplayBounds;

			var fovr	=	MathUtil.DegreesToRadians(Fov);

			var aspect	=	vp.Width / (float)vp.Height;

			rs.RenderWorld.Camera.SetView( view );
			rs.RenderWorld.Camera.SetPerspectiveFov( fovr, 0.25f, 12288, aspect );

			var camMatrix	=	rs.RenderWorld.Camera.CameraMatrix;

			ss.SetListener( camMatrix.TranslationVector, camMatrix.Forward, camMatrix.Up, Vector3.Zero );
		}



		public Ray PointToRay ( int x, int y )
		{
			var vp	=	rs.DisplayBounds;
			float fx = (x) / (float)(vp.Width);
			float fy = (y) / (float)(vp.Height);

			//Log.Message("{0} {1}", fx, fy );

			//var ray = new 
			var c = rs.RenderWorld.Camera.Frustum.GetCorners();

			var n0	=	Vector3.Lerp( c[1], c[0], fy );
			var n1	=	Vector3.Lerp( c[2], c[3], fy );
			var n	=	Vector3.Lerp( n1, n0, fx );

			var f0	=	Vector3.Lerp( c[5], c[4], fy );
			var f1	=	Vector3.Lerp( c[6], c[7], fy );
			var f	=	Vector3.Lerp( f1, f0, fx );

			return new Ray( n, (f - n ).Normalized() );
		}



		public void StartManipulation ( int x, int y, Manipulation manipulation )
		{
			this.startPoint		=	new Point( x, y );
			this.manipulation	=	manipulation;
		}


		public void UpdateManipulation ( int x, int y )
		{
			var vp		=	rs.DisplayBounds;

			var dx = x - startPoint.X;
			var dy = y - startPoint.Y;

			if (manipulation==Manipulation.Rotating) {

				addYaw		=	(-dx) / 2.5f;
				addPitch	=	(-dy) / 2.5f;

			} else if (manipulation==Manipulation.Zooming) {

				addZoom		=	(float)Math.Pow( 2, -(dx+dy)/320.0f );

			} else if (manipulation==Manipulation.Translating) {

				var tan	=	(float)Math.Tan( 0.5f * MathUtil.DegreesToRadians( Fov ) );

				addTX = - ( dx / (float)(vp.Width  / 2 ) * tan );
				addTY =   ( dy / (float)(vp.Height / 2 ) * tan );

			}
		}


		public void StopManipulation ( int x, int y )
		{
			if (editor.LockAzimuth) {
				Yaw = 0;
				addYaw = 0;
			}

			manipulation	=	Manipulation.None;

			var yaw		=	MathUtil.DegreesToRadians( Yaw + addYaw );
			var pitch	=	MathUtil.DegreesToRadians( MathUtil.Clamp(Pitch + addPitch, -89, 89) );

			var offset	=	Matrix.RotationYawPitchRoll( yaw, pitch, 0 );

			var trans	=	offset.Right * Distance * addTX
						+	offset.Up * Distance * addTY
						;

			Target		+=	trans;

			Yaw			=	Yaw + addYaw;
			Pitch		=	MathUtil.Clamp(Pitch + addPitch, -89, 89);
			Distance	=	Distance * addZoom;

			addYaw		=	0;
			addPitch	=	0;	
			addZoom		=	1;

			addTX		=	0;
			addTY		=	0;
		}

	}
}
