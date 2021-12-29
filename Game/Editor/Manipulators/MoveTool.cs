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

		Handle[] handles;

		Handle	activeHandle = null;

		readonly bool isLocalSpace;


		public MoveTool ( MapEditor editor, AxisMode axisMode ) : base(editor)
		{
			isLocalSpace = axisMode==AxisMode.Local;

			handles	=	new []
			{
				new MoveHandle( editor, Vector3.UnitX, isLocalSpace, Color.Red , Move ),
				new MoveHandle( editor, Vector3.UnitY, isLocalSpace, Color.Lime, Move ),
				new MoveHandle( editor, Vector3.UnitZ, isLocalSpace, Color.Blue, Move ),
			};
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
			var transform	= target.Transform;

			var linearSize	= editor.camera.PixelToWorldSize( transform.TranslationVector, 5 );

			var ray = editor.camera.PointToRay( x, y );

			if (activeHandle!=null) 
			{
				foreach ( var handle in handles )
				{
					var active = activeHandle==handle;
					handle.Draw( transform, active ? Handle.State.Active : Handle.State.Inactive ); 
				}
				
				foreach ( var item in editor.Selection ) 
				{
					var pos   = item.Translation;
					var floor = item.Translation;
					floor.Y = 0;

					dr.DrawLine(floor, pos, Utils.GridColor);
					dr.DrawWaypoint(floor, linearSize*5, Utils.GridColor);
				}
			} 
			else 
			{
				var handleUnderCursor	=	Handle.GetHandleUnderCursor( new Point(x,y), transform, handles );

				foreach ( var handle in handles )
				{
					var highlight = handleUnderCursor==handle;
					handle.Draw( transform, highlight ? Handle.State.Highlighted : Handle.State.Default ); 
				}
			}
		}


		public override bool IsManipulating 
		{
			get 
			{
				return activeHandle!=null;
			}
		}


		public override string ManipulationText 
		{
			get 
			{
				return isLocalSpace ? "Local" : "World";
			}
		}



		public override bool StartManipulation ( int x, int y, bool useSnapping )
		{
			if (!editor.Selection.Any()) 
			{
				return false;
			}

			var snapping	=	useSnapping ? MapEditor.MoveToolSnapValue : 0;
			var target		=	editor.Selection.Last();
			var transform	=	target.Transform;
			var pickPoint	=	new Point( x, y );

			activeHandle	=	Handle.GetHandleUnderCursor( pickPoint, transform, handles );

			if (activeHandle!=null) 
			{
				activeHandle.Start( transform, pickPoint, snapping );
				moveCommand		=	new MoveCommand(editor);
				return true;
			}
			else
			{
				return false;
			}
		}



		void Move( Vector3 moveVector )
		{
			if (moveVector.Length()>0)
			{
				moveCommand.MoveVector	=	moveVector;
				moveCommand.Execute();
			}
		}


		public override void UpdateManipulation ( int x, int y )
		{
			if (activeHandle!=null) 
			{
				activeHandle.Update( new Point( x, y ) ); 
			}
		}


		public override void StopManipulation ( int x, int y )
		{
			if (activeHandle!=null) 
			{
				editor.Game.Invoker.Execute( moveCommand );
				moveCommand		=	null;
				activeHandle	=	null;
			}
		}
	}
}
