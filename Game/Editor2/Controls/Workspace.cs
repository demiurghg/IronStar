using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Core.Input;
using Fusion.Engine.Frames.Layouts;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using Fusion.Core;
using IronStar.Core;
using Fusion.Engine.Common;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Graphics;

namespace IronStar.Editor2.Controls {
	
	public class Workspace : Frame  {

		Shelf	upperShelf;
		Shelf	lowerShelf;
		MapEditor editor;
		AEPropertyGrid grid;
		Panel	palette;
		Panel	assets;
		Panel	components;

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
			Frames.TargetFrame = this;

			//
			//	setup controls :
			//
			upperShelf	=	new Shelf( this, ShelfMode.Top );
			lowerShelf	=	new Shelf( this, ShelfMode.Bottom );


			upperShelf.AddLButton("ST", @"editor\iconToolSelect",	()=> editor.manipulator = new NullTool(editor) );
			upperShelf.AddLButton("MT", @"editor\iconToolMove",		()=> editor.manipulator = new MoveTool(editor) );
			upperShelf.AddLButton("RT", @"editor\iconToolRotate",	()=> editor.manipulator = new RotateTool(editor) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("FCS", @"editor\iconFocus",		()=> editor.FocusSelection() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("SV", null, null );
			upperShelf.AddLButton("LD", null, null );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("UNFRZ", null, ()=> editor.UnfreezeAll() );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("ENTPLT", null, ()=> ToggleShowPalette() );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("SFX"		, null, ()=> ToggleAssetsExplorer("sfx"		) );
			upperShelf.AddFatLButton("MODEL"	, null, ()=> ToggleAssetsExplorer("models"	) );
			upperShelf.AddFatLButton("ENTITY"	, null, ()=> ToggleAssetsExplorer("entities") );
			upperShelf.AddFatLButton("ITEM"		, null, ()=> ToggleAssetsExplorer("items"	) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("BUILD\rRELOAD", @"editor\iconBuild", ()=> Game.Invoker.ExecuteString("contentBuild") );

			upperShelf.AddRButton("SCR", null, ()=> Game.Invoker.ExecuteString("screenshot") );
			upperShelf.AddRButton("CONFIG", @"editor\iconComponents", ()=> ToggleShowComponents() );
			upperShelf.AddRButton("EDITOR\rCONFIG", @"editor\iconSettings", ()=> FeedProperties(editor) );
			upperShelf.AddRButton("EXIT", @"editor\iconExit", ()=> Game.Invoker.ExecuteString("killEditor") );
 

			lowerShelf.AddLButton("PLAY\r[SPACE]",	@"editor\iconSimulate2",() => editor.EnableSimulation = !editor.EnableSimulation );
			lowerShelf.AddLButton("RESET\r[ESC]" ,	@"editor\iconReset2",	() => editor.ResetWorld(true) );
			lowerShelf.AddLButton("BAKE\r[B]"	 ,	@"editor\iconBake",		() => editor.BakeToEntity() );
			lowerShelf.AddLSplitter();				 
			lowerShelf.AddLButton("ACT\n[ENTER]" ,	@"editor\iconActivate", () => editor.ActivateSelected() );
			lowerShelf.AddLButton("USE\n[U]"	 ,	@"editor\iconUse"	,	() => editor.UseSelected() );


			snapLabel	=	lowerShelf.AddRIndicator("SNAP", 200 );
			lowerShelf.AddRSplitter();
			statLabel	=	lowerShelf.AddRIndicator("FPS   : 57.29\r\r", 200 );

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
			//Frames.WipeRefs();
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

			bool vsync		=	Game.RenderSystem.VSyncInterval!=0;

			statLabel.Text	=	
				string.Format(
 				  "    FPS: {0,5:000.0} {4}\r\n" +
 				  "Max FPS: {1,5:000.0}\r\n" +
 				  "Avg FPS: {2,5:000.0}\r\n" +
 				  "Min FPS: {3,5:000.0}", curFps, maxFps, avgFps, minFps, vsync ? "VSYNC ON" : "VSYNC OFF" );

			snapLabel.Text	=	
				string.Format(
 				  "Move snap   : {0}\r\n" +
 				  "Rotate snap : {1}\r\n" +
 				  "Camera FOV  : {2}\r\n" +
 				  "", 
				  editor.MoveToolSnapEnable   ? editor.MoveToolSnapValue  .ToString("000.00") : "Disabled",
				  editor.RotateToolSnapEnable ? editor.RotateToolSnapValue.ToString("000.00") : "Disabled", 
				  editor.camera.Fov,0 );
		}



		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

			var r	=	editor.SelectionMarquee;
			var x	=	r.Left;
			var y	=	r.Top;
			var w	=	r.Width;
			var h	=	r.Height;

			spriteLayer.Draw( null,     x,     y, w, h, new Color( 32, 32, 32, 32), clipRectIndex);
			spriteLayer.Draw( null,     x,     y, w, 1, new Color(220,220,220,192), clipRectIndex);
			spriteLayer.Draw( null,     x,     y, 1, h, new Color(220,220,220,192), clipRectIndex);
			spriteLayer.Draw( null,     x, y+h-1, w, 1, new Color(220,220,220,192), clipRectIndex);
			spriteLayer.Draw( null, x+w-1,     y, 1, h, new Color(220,220,220,192), clipRectIndex);

			//spriteLayer.Draw( null,     x,     y, w, h, new Color(44,85,128,128), clipRectIndex);
			//spriteLayer.Draw( null,     x,     y, w, 1, new Color(44,85,128,128), clipRectIndex);
			//spriteLayer.Draw( null,     x,     y, 1, h, new Color(44,85,128,128), clipRectIndex);
			//spriteLayer.Draw( null,     x, y+h-1, w, 1, new Color(44,85,128,128), clipRectIndex);
			//spriteLayer.Draw( null, x+w-1,     y, 1, h, new Color(44,85,128,128), clipRectIndex);

			if (editor.manipulator!=null && editor.manipulator.IsManipulating) {
				var text  = editor.manipulator.ManipulationText;
				var lines = text.SplitLines();

				x   =	mouseX + 0;
				y	=  	mouseY + 20;
				w	=	lines.Max( line => line.Length ) * 8 + 8;
				h	=	lines.Length * 8 + 8;

				w	=	Math.Max( w, 12 * 8 );
				
				spriteLayer.Draw( null, x,y,w,h, new Color(0,0,0,160), clipRectIndex);

				foreach ( var line in lines ) {
					spriteLayer.DrawDebugString( x+4, y+4, line, ColorTheme.TextColorPushed, clipRectIndex );
					y+=8;
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void FeedProperties ( object target )
		{
			if (grid==null) {
				grid = new AEPropertyGrid(Frames);

				grid.Width	=	350;
				grid.Height	=	500;

				grid.X		=	Width - 350 - 10;
				grid.Y		=	40 + 10;

				grid.Anchor	=	FrameAnchor.Top | FrameAnchor.Right;

				grid.PropertyChanged+=Grid_PropertyChanged;

				Add( grid );
			}

			if (target==null) {
				grid.Visible = false;
			} else {
				grid.Visible = true;
			}

			grid.FeedObjects( target, (target as MapEntity)?.Factory );

		}


		private void Grid_PropertyChanged( object sender, AEPropertyGrid.PropertyChangedEventArgs e )
		{
			var mapNode = e.TargetObject as MapNode;
			mapNode?.ResetNode( editor.World );
		}


		void ArrangeLeft ( params Frame[] frames )
		{
			int x = 10;
			int y = 50;

			foreach ( var frame in frames ) {
				if (frame==null) {
					continue;
				}
				if (!frame.Visible) {
					continue;
				}
				frame.X = x;
				frame.Y = y;
				x += frame.Width;
				x += 10;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void ToggleShowPalette ()
		{
			if (palette==null) {

				palette	=	new Panel( Frames, 10, 50, 150, 10 );
				palette.Layout = new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

				palette.Add( new Label( Frames, 0,0,120,12, "Map Nodes Palette" ) { TextAlignment = Alignment.MiddleCenter } );

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

				palette.Add( new Button( Frames, "Close", 0,0,150,20, () => palette.Visible = false ) );

				Add( palette );
			} else {
				palette.Visible = !palette.Visible;
			}

			ArrangeLeft( palette, assets, components );
		}


		/// <summary>
		/// 
		/// </summary>
		public void ToggleShowComponents ()
		{
			if (components==null) {

				components	=	new Panel( Frames, 10, 50, 150, 10 );
				components.Layout = new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

				components.Add( new Label( Frames, 0,0,120,12, "Game Components" ) { TextAlignment = Alignment.MiddleCenter } );

				var componentList = Game.Components.OrderBy( c1 => c1.GetType().Name ).ToArray();

				foreach ( var component in componentList ) {

					string name = component.GetType().Name;

					Action func = () => { 
						FeedProperties( component );
					};
					components.Add( new Button( Frames, name, 0,0,150,20, func ) );
				}

				components.Add( new Frame( Frames, 0,0,0,10, "", Color.Zero ) );

				components.Add( new Button( Frames, "Close", 0,0,150,20, () => components.Visible = false ) );

				Add( components );
			} else {
				components.Visible = !components.Visible;
			}

			ArrangeLeft( palette, assets, components );
		}



		public void ToggleAssetsExplorer ( string category )
		{
			if (assets==null) {

				assets	=	new Panel( Frames, 10, 50, 200, 10 );

				assets.Add( new Label( Frames, 0,0,200,12, "Asset Explorer [" + category + "]" ) { TextAlignment = Alignment.MiddleCenter } );
				
				assets.Layout = new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

				var scrollBox = new ScrollBox( Frames, 0,0,100,400 );
				scrollBox.Padding = 1;

				var listView  = new ListBox( Frames, new[] {"xsaxsxs", "xsxsxsxs", "xsxsxsxsxsacweq"} );

				scrollBox.Add( listView );
				assets.Add( scrollBox );


				assets.Add( new Button( Frames, "New", 0,0,150,20,   () => Log.Warning("New") ) );
				assets.Add( new Button( Frames, "Close", 0,0,150,20, () => palette.Visible = false ) );

				Add( assets );
			} else {
				assets.Visible = !assets.Visible;
			}

			ArrangeLeft( palette, assets, components );
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
			
			if (e.Key==Keys.F2) {
				var vsync = Game.RenderSystem.VSyncInterval == 1;
				Game.RenderSystem.VSyncInterval = vsync ? 0 : 1;
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
					//Game.RenderSystem.RenderWorld.CaptureRadiance();
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
						editor.RotateToolSnapEnable = !editor.RotateToolSnapEnable;
					} else {
						editor.MoveToolSnapEnable = !editor.MoveToolSnapEnable;
					}
				}
				if (e.Key==Keys.D) {
					if (ctrl || shift) {
						editor.DuplicateSelection();
					}
				}
				if (e.Key==Keys.G) {
					if (ctrl||alt) {
						editor.DrawGrid = !editor.DrawGrid;
					} else {
						Game.RenderSystem.SkipDebugRendering = !Game.RenderSystem.SkipDebugRendering;
					}
				}
				if (e.Key==Keys.T) {
					editor.TargetSelection();
				}
				if (e.Key==Keys.OemComma) {
					if (ctrl) {
						editor.RotateToolSnapValue -= 5;
					} else {
						editor.MoveToolSnapValue *= 0.5f;
					}
				}
				if (e.Key==Keys.OemPeriod) {
					if (ctrl) {
						editor.RotateToolSnapValue += 5;
					} else {
						editor.MoveToolSnapValue *= 2.0f;
					}
				}
				if (e.Key==Keys.OemOpenBrackets) {
					editor.CameraFov -= 10.0f;
				}
				if (e.Key==Keys.OemCloseBrackets) {
					editor.CameraFov += 10.0f;
				}
				if (e.Key==Keys.F11) {
					Game.Invoker.ExecuteString("screenshot");
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
			mouseX	=	e.X;
			mouseY	=	e.Y;

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


		int mouseX = 0; 
		int mouseY = 0;

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
