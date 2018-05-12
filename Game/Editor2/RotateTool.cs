﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using IronStar.Editor2.Manipulators;
using Fusion;				  
using IronStar.Mapping;

namespace IronStar.Editor2 {
	public class RotateTool : Manipulator {

		enum Rotation {
			Yaw, Pitch, Roll
		}


		/// <summary>
		/// 
		/// </summary>
		public RotateTool ( MapEditor editor ) : base(editor)
		{
		}


		Vector3 GetAxis ( int index )
		{
			var target = editor.Selection.LastOrDefault();

			if (target==null) {
				switch (index) {
					case 0: return Vector3.UnitX;
					case 1: return Vector3.UnitY;
					case 2: return Vector3.UnitZ;
					case 3: return Vector3.ForwardRH;
				}
			} else {
				float yaw   = MathUtil.DegreesToRadians( target.RotateYaw );
				float pitch = MathUtil.DegreesToRadians( target.RotatePitch );
				float roll  = MathUtil.DegreesToRadians( target.RotateRoll );
				switch (index) {
					case 0: return Matrix.RotationYawPitchRoll(yaw,pitch,0).Right;
					case 1: return Matrix.RotationYawPitchRoll(yaw,0,0).Up;
					case 2: return Matrix.RotationYawPitchRoll(yaw,pitch,roll).Forward;
					case 3: return Matrix.RotationYawPitchRoll(yaw,0,0).Forward;
				}
			}

			return Vector3.Zero;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime, int x, int y )
		{
			var dr = rs.RenderWorld.Debug;
			var mp = game.Mouse.Position;

			if (!editor.Selection.Any()) {
				return;
			}

			var target		= editor.Selection.Last();
			var origin		= target.TranslateVector;

			var linerSize	= editor.camera.PixelToWorldSize( origin, 5 );
			var ray			= editor.camera.PointToRay( x, y );




			if (!manipulating) {
				var hitX	=	IntersectRing( target.TranslateVector, GetAxis(0), mp );
				var hitY	=	IntersectRing( target.TranslateVector, GetAxis(1), mp );
				var hitZ	=	IntersectRing( target.TranslateVector, GetAxis(2), mp );

				int hitInd	=	HandleIntersection.PollIntersections( hitX, hitY, hitZ );

				DrawRing( dr, ray, origin, GetAxis(0), hitInd == 0 ? Utils.SelectColor : Color.Red  );
				DrawRing( dr, ray, origin, GetAxis(1), hitInd == 1 ? Utils.SelectColor : Color.Lime );
				DrawRing( dr, ray, origin, GetAxis(2), hitInd == 2 ? Utils.SelectColor : Color.Blue );

			} else {

				DrawRing( dr, ray, origin, GetAxis(axisIndex), Utils.SelectColor );

				var vecSize	=	editor.camera.PixelToWorldSize(origin, 110);

				dr.DrawLine( origin, origin + vector0 * vecSize, Utils.SelectColor, Utils.SelectColor, 2, 2 );
				dr.DrawLine( origin, origin + vector1 * vecSize, Utils.SelectColor, Utils.SelectColor, 2, 2 );
			}


			var a	=	editor.camera.PixelToWorldSize(origin, 110);
			var b	=	editor.camera.PixelToWorldSize(origin, 140);
			var c	=	editor.camera.PixelToWorldSize(origin, 150);

			var fwd	=	GetAxis(3);

			var clr	=	manipulating ? Utils.SelectColor : Color.Lime;

			dr.DrawLine( origin + fwd*a, origin + fwd*b, clr, clr,  4, 4 );
			dr.DrawLine( origin + fwd*b, origin + fwd*c, clr, clr, 16, 2 );
		}




		public override bool IsManipulating {
			get {
				return manipulating;
			}
		}


		public override string ManipulationText {
			get {
				var target = targets?.LastOrDefault();
				if (target==null) {
					return "---";
				}

				return string.Format(
					"{0}\r" +
					"{1,7:000.00}\r" +
					"{2,7:000.00}\r" +
					"{3,7:000.00}\r" + 
					"{3,7:000.00}", 
					rotation.ToString().ToUpper(), 
					target.RotateYaw, 
					target.RotatePitch, 
					target.RotateRoll,
					angle);
			}
		}



		bool		manipulating;
		Rotation	rotation; // yaw=0,pitch=1,roll=2
		Vector3		initialPoint;
		Vector3		currentPoint;
		int			axisIndex;

		Vector3 vector0, vector1;
		float	angle;

		bool		snapEnable;
		float		snapValue;


		MapNode[] targets	= null;
		float[]	  angles	= null;


		public override bool StartManipulation ( int x, int y )
		{
			if (!editor.Selection.Any()) {
				return false;
			}

			snapEnable	=	editor.Config.RotateToolSnapEnable;
			snapValue	=	editor.Config.RotateToolSnapValue;
			angle		=	0;
			vector0		=	Vector3.Zero;
			vector1		=	Vector3.Zero;

			targets		=	editor.GetSelection();

			var target	=	targets.LastOrDefault();

			var origin	=	targets.Last().TranslateVector;
			var mp		=	new Point( x, y );


			var intersectX	=	IntersectRing( origin, GetAxis(0), mp );
			var intersectY	=	IntersectRing( origin, GetAxis(1), mp );
			var intersectZ	=	IntersectRing( origin, GetAxis(2), mp );

			axisIndex		=	HandleIntersection.PollIntersections( intersectX, intersectY, intersectZ );

			if (axisIndex<0) {
				return false;
			}

			if (axisIndex==0) {		
				manipulating	=	true;
				rotation		=	Rotation.Pitch;
				initialPoint	=	intersectX.HitPoint;
				currentPoint	=	intersectX.HitPoint;
				angles			=	targets.Select( t => t.RotatePitch ).ToArray();
				return true;
			}

			if (axisIndex==1) {		
				manipulating	=	true;
				rotation		=	Rotation.Yaw;
				initialPoint	=	intersectY.HitPoint;
				currentPoint	=	intersectY.HitPoint;
				angles			=	targets.Select( t => t.RotateYaw ).ToArray();
				return true;
			}

			if (axisIndex==2) {		
				manipulating	=	true;
				rotation		=	Rotation.Roll;
				initialPoint	=	intersectZ.HitPoint;
				currentPoint	=	intersectZ.HitPoint;
				angles			=	targets.Select( t => t.RotateRoll ).ToArray();
				return true;
			}
			
			return false;
		}


		public override void UpdateManipulation ( int x, int y )
		{
			if (manipulating) {

				var origin	=	targets.Last().TranslateVector;
				var mp		=	new Point( x, y );

				var result	=	IntersectRing( origin, GetAxis(axisIndex), mp );
				
				currentPoint	=	result.HitPoint;

				vector0			=	(initialPoint - origin).Normalized();
				vector1			=	(currentPoint - origin).Normalized();

				var sine		=	Vector3.Dot( GetAxis(axisIndex), Vector3.Cross( vector0, vector1 ) );
				var cosine		=	Vector3.Dot( vector0, vector1 );

				angle			=	MathUtil.RadiansToDegrees ( (float)Math.Atan2( sine, cosine ) );

				for ( int i=0; i<targets.Length; i++) {
					var target	=	targets[i];

					switch (rotation) {
						case Rotation.Yaw  : target.RotateYaw   = Snap( angles[i] + angle, snapValue, snapEnable ); break;
						case Rotation.Pitch: target.RotatePitch = Snap( angles[i] + angle, snapValue, snapEnable ); break;
						case Rotation.Roll : target.RotateRoll  = Snap( angles[i] + angle, snapValue, snapEnable ); break;
					}

					target.ResetNode( this.editor.World );
				}
			}
		}


		public override void StopManipulation ( int x, int y )
		{
			if (manipulating) {
				manipulating	=	false;
			}
		}

	}
}
