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
using Fusion.Widgets;
using Fusion.Widgets.Advanced;

namespace IronStar.Editor.Controls {
	
	public class Workspace : Frame  {

		public Shelf UpperShelf {
			get { return upperShelf; }
		}

		public Shelf LowerShelf {
			get { return lowerShelf; }
		}

		class Hotkey {
			public Hotkey( Keys key, ModKeys modKey, Action action ) 
			{
				Key		=	key		;
				ModKey	=	modKey	;
				Action	=	action	;
			}
			public readonly Keys	Key;
			public readonly ModKeys	ModKey;
			public readonly Action	Action;
		}

		readonly List<Hotkey> hotkeys = new List<Hotkey>();
		readonly List<Palette> palettes = new List<Palette>();

		Shelf	upperShelf;
		Shelf	lowerShelf;
		MapEditor editor;
		AEPropertyGrid grid;
		ScrollBox	gridScrollBox;
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
		/// Cloeses workspace and remove it from parent frame node
		/// </summary>
		public void CloseWorkspace ()
		{
			Parent.Remove(this);
		}


		/// <summary>
		/// Adds hotkey action
		/// </summary>
		/// <param name="key"></param>
		/// <param name="modKey"></param>
		/// <param name="action"></param>
		public void AddHotkey ( Keys key, ModKeys modKey, Action action )
		{
			hotkeys.Add( new Hotkey( key, modKey, action ) );
		}


		/// <summary>
		/// Toggles (and adds, if necessary) palette.
		/// </summary>
		/// <param name="palette"></param>
		public void TogglePalette ( Palette palette )
		{
			if (!palettes.Contains(palette)) {
				palettes.Add(palette);
				this.Add( palette );
				palette.Visible = true;
			} else {
				palette.Visible = !palette.Visible;
			}

			ArrangeLeft( palettes );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frames"></param>
		void ArrangeLeft ( IEnumerable<Frame> frames )
		{
			int x = 10;
			int y = 50;

			foreach ( var frame in frames ) {
				if (frame==null) { continue; }
				if (!frame.Visible) { continue;	}
				frame.X = x;
				frame.Y = y;
				x += frame.Width;
				x += 10;
			}
		}

		List<float> fps = new List<float>(60);


		/// <summary>
		/// Updates workspace internal state
		/// </summary>
		/// <param name="gameTime"></param>
		protected override void Update( GameTime gameTime )
		{
			base.Update( gameTime );
			ArrangeLeft( palettes );
		}


		/// <summary>
		/// Draws workspace:
		///  - Selection rectangle 
		///  - Manipulator hints
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="spriteLayer"></param>
		/// <param name="clipRectIndex"></param>
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
		/// Feeds object properties to workspace property grid
		/// </summary>
		/// <param name="target"></param>
		public void FeedProperties ( object target )
		{
			if (grid==null) {

				gridScrollBox = new ScrollBox( Frames, Width - 320, 40, 320, Height - 40-40 ) {
					BorderColor = ColorTheme.BorderColor,
					Border		= 1,
				};

				gridScrollBox.ScrollMarkerSize = 5;

				grid = new AEPropertyGrid(Frames);

				grid.Width	=	300;
				grid.Height	=	500;

				grid.X		=	Width - 300 - 10;
				grid.Y		=	40 + 10;

				grid.Anchor	=	FrameAnchor.Top | FrameAnchor.Right;

				grid.PropertyChanged+=Grid_PropertyChanged;

				Add( gridScrollBox );
				gridScrollBox.Add( grid );
			}

			if (target==null) {
				gridScrollBox.Visible = false;
			} else {
				gridScrollBox.Visible = true;
			}

			grid.TargetObject = target;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_PropertyChanged( object sender, AEPropertyGrid.PropertyChangedEventArgs e )
		{
			editor.SelectedPropertyChange( e.TargetObject );
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

			foreach ( var hotkey in hotkeys ) {

				if (hotkey.Key==e.Key) {

					if ( shift == (hotkey.ModKey==ModKeys.Shift) && alt == (hotkey.ModKey==ModKeys.Alt) && ctrl == (hotkey.ModKey==ModKeys.Ctrl) ) {

						hotkey.Action?.Invoke();

					}
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RootFrame_Click( object sender, Frame.MouseEventArgs e )
		{
			//	TODO: Manipulation, selection and camera manipulating are
			//	common operations for each type of editors
			if (editor.camera.Manipulation==Manipulation.None && !editor.manipulator.IsManipulating) {
				var shift =	Game.Keyboard.IsKeyDown(Keys.LeftShift) || Game.Keyboard.IsKeyDown(Keys.RightShift);
				editor.Select( e.X, e.Y, shift );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RootFrame_MouseDown( object sender,  Frame.MouseEventArgs e )
		{
			//	TODO: Manipulation, selection and camera manipulating are
			//	common operations for each type of editors
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RootFrame_MouseMove( object sender, Frame.MouseEventArgs e )
		{
			//	TODO: Manipulation, selection and camera manipulating are
			//	common operations for each type of editors
			editor.camera.UpdateManipulation( e.X, e.Y );
			editor.manipulator.UpdateManipulation( e.X, e.Y );
			editor.UpdateMarqueeSelection( e.X, e.Y );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RootFrame_MouseUp( object sender, Frame.MouseEventArgs e )
		{
			//	TODO: Manipulation, selection and camera manipulating are
			//	common operations for each type of editors
			editor.camera.StopManipulation( e.X, e.Y );
			editor.manipulator.StopManipulation( e.X, e.Y );
			editor.StopMarqueeSelection( e.X, e.Y );
		}
	}
}
