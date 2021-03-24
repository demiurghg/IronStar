﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using IronStar.Editor.Manipulators;
using Fusion;
using Fusion.Core;
using IronStar.Mapping;
using IronStar.Editor.Commands;

namespace IronStar.Editor.Manipulators 
{
	public class MoveTool : Manipulator 
	{
		MoveCommand moveCommand;

		public MoveTool ( MapEditor editor ) : base(editor)
		{
		}


		public override void Update ( GameTime gameTime, int x, int y )
		{
			var dr = rs.RenderWorld.Debug;
			var mp = game.Mouse.Position;

			if (!editor.Selection.Any()) 
			{
				return;
			}

			var target		= editor.Selection.Last();
			var origin		= target.Translation;

			var linerSize	= editor.camera.PixelToWorldSize( origin, 5 );

			var ray = editor.camera.PointToRay( x, y );

			if (manipulating) 
			{
				DrawArrow( dr, ray, origin, direction, Utils.SelectColor  );

				dr.DrawPoint(initialPoint, linerSize, Utils.GridColor);
				dr.DrawPoint(currentPoint, linerSize, Utils.GridColor);
				dr.DrawLine(initialPoint, currentPoint, Utils.GridColor);

				foreach ( var item in editor.Selection ) 
				{
					var pos   = item.Translation;
					var floor = item.Translation;
					floor.Y = 0;

					dr.DrawLine(floor, pos, Utils.GridColor);
					dr.DrawWaypoint(floor, linerSize*5, Utils.GridColor);
				}
			} 
			else 
			{
				var hitX	=	IntersectArrow( target.Translation, Vector3.UnitX, mp );
				var hitY	=	IntersectArrow( target.Translation, Vector3.UnitY, mp );
				var hitZ	=	IntersectArrow( target.Translation, Vector3.UnitZ, mp );

				int hitInd	=	HandleIntersection.PollIntersections( hitX, hitY, hitZ );

				DrawArrow( dr, ray, origin, Vector3.UnitX, hitInd == 0 ? Utils.SelectColor : Color.Red  );
				DrawArrow( dr, ray, origin, Vector3.UnitY, hitInd == 1 ? Utils.SelectColor : Color.Lime );
				DrawArrow( dr, ray, origin, Vector3.UnitZ, hitInd == 2 ? Utils.SelectColor : Color.Blue );
			}
		}


		public override bool IsManipulating 
		{
			get {
				return manipulating;
			}
		}


		public override string ManipulationText 
		{
			get 
			{
				var target = targets?.LastOrDefault();

				if (target==null) 
				{
					return "---";
				}

				var distance	= Vector3.Distance( initialPoint, currentPoint );
				var translateX	= target.TranslateX;
				var translateY	= target.TranslateY;
				var translateZ	= target.TranslateZ;
				return string.Format(
					"X {0,8:###.00}\r" +
					"Y {1,8:###.00}\r" +
					"Z {2,8:###.00}\r" +
					"D {3,8:###.00}\r",
					translateX,
					translateY,
					translateZ,
					distance);
			}
		}


		bool	manipulating;
		Vector3 direction;
		Vector3 initialPoint;
		Vector3 currentPoint;

		bool		snapEnable;
		float		snapValue;

		MapNode[] targets = null;
		Vector3[] initPos = null;


		public override bool StartManipulation ( int x, int y, bool useSnapping )
		{
			if (!editor.Selection.Any()) 
			{
				return false;
			}

			snapEnable	=	useSnapping;
			snapValue	=	editor.MoveToolSnapValue;

			targets	=	editor.Selection.ToArray();
			initPos	=	targets.Select( t => t.Translation ).ToArray();

			var origin	=	initPos.Last();
			var mp		=	new Point( x, y );


			var intersectX	=	IntersectArrow( origin, Vector3.UnitX, mp );
			var intersectY	=	IntersectArrow( origin, Vector3.UnitY, mp );
			var intersectZ	=	IntersectArrow( origin, Vector3.UnitZ, mp );

			var index		=	HandleIntersection.PollIntersections( intersectX, intersectY, intersectZ );

			if (index<0) 
			{
				return false;
			}

			if (index==0) 
			{		
				manipulating	=	true;
				direction		=	Vector3.UnitX;
				initialPoint	=	intersectX.HitPoint;
				currentPoint	=	intersectX.HitPoint;
				moveCommand		=	new MoveCommand(editor);
				return true;
			}

			if (index==1) 
			{		
				manipulating	=	true;
				direction		=	Vector3.UnitY;
				initialPoint	=	intersectY.HitPoint;
				currentPoint	=	intersectY.HitPoint;
				moveCommand		=	new MoveCommand(editor);
				return true;
			}

			if (index==2) 
			{		
				manipulating	=	true;
				direction		=	Vector3.UnitZ;
				initialPoint	=	intersectZ.HitPoint;
				currentPoint	=	intersectZ.HitPoint;
				moveCommand		=	new MoveCommand(editor);
				return true;
			}
			
			return false;
		}


		public override void UpdateManipulation ( int x, int y )
		{
			if (manipulating && targets.Any()) 
			{
				var origin	=	initialPoint;
				var mp		=	new Point( x, y );

				var result	=	IntersectArrow( origin, direction, mp );
				
				currentPoint	= result.HitPoint;

				//	compute delta vector :
				var lastItemPosition	=	initPos.Last();
				var newItemPosition		=	Snap( lastItemPosition + (currentPoint - initialPoint), snapEnable ? snapValue : 0 ); 
				var translationDelta	=	newItemPosition - lastItemPosition;

				translationDelta	*=	direction;

				if (translationDelta.Length()>0)
				{
					moveCommand.MoveVector	=	translationDelta;
					moveCommand.Execute();
				}
			}
		}


		public override void StopManipulation ( int x, int y )
		{
			if (manipulating) 
			{
				editor.Game.Invoker.Execute( moveCommand );
				moveCommand		=	null;
				manipulating	=	false;
			}
		}
	}
}
