using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Input;

namespace IronStar.Editor2.Controls {
	
	public class Workspace : Frame  {

		Shelf	upperShelf;
		Shelf	lowerShelf;
		MapEditor editor;
		AEPropertyGrid grid;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		public Workspace ( MapEditor editor, Frame parent ) : base( parent.Frames )
		{	
			this.editor			=	editor;

			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.Padding		=	0;

			this.X				=	0;
			this.Y				=	0;
			this.Width			=	parent.Width;
			this.Height			=	parent.Height;

			this.Anchor			=	FrameAnchor.All;

			parent.Add(this);

			//
			//	setup controls :
			//
			upperShelf	=	new Shelf( this, ShelfMode.Top );
			lowerShelf	=	new Shelf( this, ShelfMode.Bottom );


			upperShelf.AddLButton("ST", null, ()=> editor.manipulator = new NullTool(editor) );
			upperShelf.AddLButton("MT", null, ()=> editor.manipulator = new MoveTool(editor) );
			upperShelf.AddLButton("RT", null, ()=> editor.manipulator = new RotateTool(editor) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("SV", null, null );
			upperShelf.AddLButton("LD", null, null );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("TRG", null, null );
			upperShelf.AddLButton("SFX", null, null );
			upperShelf.AddLButton("SPP", null, null );

			upperShelf.AddRButton("ES", null, ()=> FeedProperties(editor.Config) );


			lowerShelf.AddFatLButton("PLAY",	null, () => editor.EnableSimulation = !editor.EnableSimulation );
			lowerShelf.AddFatLButton("RESET",	null, () => editor.ResetWorld(true) );
			lowerShelf.AddFatLButton("BAKE",	null, () => editor.BakeToEntity() );
			lowerShelf.AddLSplitter();
			lowerShelf.AddFatLButton("ACT",		null, () => editor.ActivateSelected() );
			lowerShelf.AddFatLButton("USE",		null, () => editor.UseSelected() );

			//
			//	setup keys & mouse :
			//
			KeyDown		+=	Workspace_KeyDown;

			MouseDown	+=	RootFrame_MouseDown;
			MouseMove	+=	RootFrame_MouseMove;
			MouseUp		+=	RootFrame_MouseUp;

			Click		+=	RootFrame_Click;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void FeedProperties ( object target )
		{
			if (grid==null) {
				grid = new AEPropertyGrid(Frames);

				grid.Width	=	300;
				grid.Height	=	400;

				grid.X		=	Width - 300 - 10;
				grid.Y		=	40 + 10;

				grid.Anchor	=	FrameAnchor.Top | FrameAnchor.Right;

				Add( grid );
			}

			grid.TargetObject = target;

			if (target==null) {
				grid.Visible = false;
			} else {
				grid.Visible = true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Workspace_KeyDown( object sender, KeyEventArgs e )
		{
			bool shift	=	e.Shift;
			bool ctrl	=	e.Ctrl;

			e.Handled = true;

			if (e.Key==Keys.F) {
				editor.FocusSelection();
			}
			if (!editor.manipulator.IsManipulating) {
				if (e.Key==Keys.Q) {
					editor.manipulator = new NullTool(editor);
				}
				if (e.Key==Keys.W) {
					editor.manipulator = new MoveTool(editor);
				}
				if (e.Key==Keys.E) {
					editor.manipulator = new RotateTool(editor);
				}
				if (e.Key==Keys.Delete) {
					editor.DeleteSelection();
				}
				if (e.Key==Keys.Space) {
					editor.EnableSimulation = !editor.EnableSimulation;
				}
				if (e.Key==Keys.Escape) {
					editor.EnableSimulation = false;
					editor.ResetWorld(false);
				}
				if (e.Key==Keys.K) {
					editor.ResetWorld(true);
				}
				if (e.Key==Keys.R) {
					Game.RenderSystem.RenderWorld.CaptureRadiance();
				}
				if (e.Key==Keys.B) {
					editor.BakeToEntity();
				}
				if (e.Key==Keys.Enter) {
					editor.ActivateSelected();
				}
				if (e.Key==Keys.U) {
					editor.UseSelected();
				}
				if (e.Key==Keys.D) {
					if (ctrl || shift) {
						editor.DuplicateSelection();
					}
				}
				if (e.Key==Keys.G) {
					Game.RenderSystem.SkipDebugRendering = !Game.RenderSystem.SkipDebugRendering;
				}
			}
		}



		private void RootFrame_Click( object sender, Frame.MouseEventArgs e )
		{
			if (editor.camera.Manipulation==Manipulation.None && !editor.manipulator.IsManipulating) {
				var shift =	Game.Keyboard.IsKeyDown(Keys.LeftShift) || Game.Keyboard.IsKeyDown(Keys.RightShift);
				editor.Select( e.X, e.Y, shift );
			}
		}



		private void RootFrame_MouseDown( object sender,  Frame.MouseEventArgs e )
		{
			if (Game.Keyboard.IsKeyDown(Keys.LeftAlt)) {
				if (e.Key==Keys.LeftButton) {
					editor.camera.StartManipulation( e.X, e.Y, Manipulation.Rotating );
				} else
				if (e.Key==Keys.RightButton) {
					editor.camera.StartManipulation( e.X, e.Y, Manipulation.Zooming );
				} else 
				if (e.Key==Keys.MiddleButton) {
					editor.camera.StartManipulation( e.X, e.Y, Manipulation.Translating );
				} else {
					editor.camera.StartManipulation( e.X, e.Y, Manipulation.None );
				}
			} else {
				if (!editor.manipulator.StartManipulation( e.X, e.Y )) {
					editor.StartMarqueeSelection( e.X, e.Y, Game.Keyboard.IsKeyDown(Keys.LeftShift) );
				}
			}
		}



		private void RootFrame_MouseMove( object sender, Frame.MouseEventArgs e )
		{
			editor.camera.UpdateManipulation( e.X, e.Y );
			editor.manipulator.UpdateManipulation( e.X, e.Y );
			editor.UpdateMarqueeSelection( e.X, e.Y );
		}



		private void RootFrame_MouseUp( object sender, Frame.MouseEventArgs e )
		{
			editor.camera.StopManipulation( e.X, e.Y );
			editor.manipulator.StopManipulation( e.X, e.Y );
			editor.StopMarqueeSelection( e.X, e.Y );
		}
	}
}
