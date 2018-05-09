using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Input;
using Fusion.Engine.Frames.Layouts;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Common;

namespace IronStar.Editor2.Controls {
	
	public class Workspace : Frame  {

		Shelf	upperShelf;
		Shelf	lowerShelf;
		MapEditor editor;
		AEPropertyGrid grid;
		Panel	palette;

		Type[] entityTypes;

		Frame	statLabel;
		Frame	snapLabel;


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

			this.entityTypes	=	Misc.GetAllSubclassesOf( typeof(EntityFactory), false );


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
			upperShelf.AddLButton("FCS", null, ()=> editor.FocusSelection() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("SV", null, null );
			upperShelf.AddLButton("LD", null, null );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("ENTPLT", null, ()=> ToggleShowPalette() );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("UNFRZ", null, ()=> editor.UnfreezeAll() );

			upperShelf.AddFatRButton("EDITOR\rCONFIG", null, ()=> FeedProperties(editor.Config) );
			upperShelf.AddRButton	("EXIT", null, ()=> Game.GameEditor.Stop() );
 

			lowerShelf.AddFatLButton("PLAY\r[SPACE]" ,	null, () => editor.EnableSimulation = !editor.EnableSimulation );
			lowerShelf.AddFatLButton("RESET\r[ESC]"	 ,	null, () => editor.ResetWorld(true) );
			lowerShelf.AddFatLButton("BAKE\r[B]"	 ,	null, () => editor.BakeToEntity() );
			lowerShelf.AddLSplitter();				 
			lowerShelf.AddFatLButton("ACT\n[ENTER]"	 ,	null, () => editor.ActivateSelected() );
			lowerShelf.AddFatLButton("USE\n[???]"	 ,	null, () => editor.UseSelected() );


			snapLabel	=	lowerShelf.AddRIndicator("SNAP", 192 );
			lowerShelf.AddRSplitter();
			statLabel	=	lowerShelf.AddRIndicator("FPS   : 57.29\r\r", 128 );

			//
			//	setup keys & mouse :
			//
			KeyDown		+=	Workspace_KeyDown;

			MouseDown	+=	RootFrame_MouseDown;
			MouseMove	+=	RootFrame_MouseMove;
			MouseUp		+=	RootFrame_MouseUp;

			Click		+=	RootFrame_Click;
		}



		public void CloseWorkspace ()
		{
			Parent.Remove(this);
		}


		List<float> fps = new List<float>(60);


		protected override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			fps.Add( gameTime.Fps );
			while (fps.Count>30) {
				fps.RemoveAt(0);
			}	

			float curFps	=	gameTime.Fps;
			float avgFps	=	fps.Average();
			float maxFps	=	fps.Max();
			float minFps	=	fps.Min();

			statLabel.Text	=	
				string.Format(
 				  "    FPS: {0,5:000.0}\r\n" +
 				  "Max FPS: {1,5:000.0}\r\n" +
 				  "Avg FPS: {2,5:000.0}\r\n" +
 				  "Min FPS: {3,5:000.0}", curFps, maxFps, avgFps, minFps );

			snapLabel.Text	=	
				string.Format(
 				  "Move snap   : {0}\r\n" +
 				  "Rotate snap : {1}\r\n" +
 				  "\r\n" +
 				  "", 
				  editor.Config.MoveToolSnapEnable   ? editor.Config.MoveToolSnapValue  .ToString("000.00") : "Disabled",
				  editor.Config.RotateToolSnapEnable ? editor.Config.RotateToolSnapValue.ToString("000.00") : "Disabled", 
				  0,0 );
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

			if (target==null) {
				grid.Visible = false;
			} else {
				grid.Visible = true;
			}

			grid.FeedObjects( target, (target as MapEntity)?.Factory );

		}





		/// <summary>
		/// 
		/// </summary>
		public void ToggleShowPalette ()
		{
			if (palette==null) {

				palette	=	new Panel( Frames, 10, 50, 150, 10 );
				palette.Layout = new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

				palette.Add( new Button( Frames, "Decal"		, 0,0,150,20, () => editor.CreateNodeUI( new MapDecal		() ) ) );
				palette.Add( new Button( Frames, "Static Model"	, 0,0,150,20, () => editor.CreateNodeUI( new MapModel		() ) ) );
				palette.Add( new Button( Frames, "Light Probe"	, 0,0,150,20, () => editor.CreateNodeUI( new MapLightProbe	() ) ) );
				palette.Add( new Button( Frames, "Omni Light"	, 0,0,150,20, () => editor.CreateNodeUI( new MapOmniLight	() ) ) );
				palette.Add( new Button( Frames, "Spot Light"	, 0,0,150,20, () => editor.CreateNodeUI( new MapSpotLight	() ) ) );

				palette.Add( new Frame( Frames, 0,0,0,10, "", Color.Zero ) );

				foreach ( var ent in entityTypes ) {

					string name = ent.Name.Replace("Factory", "");
					Action func = () => { 
						var mapEntity = new MapEntity();
						mapEntity.Factory = (EntityFactory)Activator.CreateInstance(ent);
						editor.CreateNodeUI( mapEntity ); 
					};
					palette.Add( new Button( Frames, name, 0,0,150,20, func ) );
				}

				palette.Add( new Frame( Frames, 0,0,0,10, "", Color.Zero ) );

				palette.Add( new Button( Frames, "Close Palette", 0,0,150,20, () => palette.Visible = false ) );

				Add( palette );
			} else {
				palette.Visible = !palette.Visible;
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
			bool alt	=	e.Alt;

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
				if (e.Key==Keys.J) {
					if (ctrl) {
						editor.Config.RotateToolSnapEnable = !editor.Config.RotateToolSnapEnable;
					} else {
						editor.Config.MoveToolSnapEnable = !editor.Config.MoveToolSnapEnable;
					}
				}
				if (e.Key==Keys.D) {
					if (ctrl || shift) {
						editor.DuplicateSelection();
					}
				}
				if (e.Key==Keys.G) {
					Game.RenderSystem.SkipDebugRendering = !Game.RenderSystem.SkipDebugRendering;
				}
				if (e.Key==Keys.OemComma) {
					if (ctrl) {
						editor.Config.RotateToolSnapValue -= 5;
					} else {
						editor.Config.MoveToolSnapValue *= 0.5f;
					}
				}
				if (e.Key==Keys.OemPeriod) {
					if (ctrl) {
						editor.Config.RotateToolSnapValue += 5;
					} else {
						editor.Config.MoveToolSnapValue *= 2.0f;
					}
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
