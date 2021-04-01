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
using Fusion.Engine.Common;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Graphics;
using Fusion.Widgets;
using Fusion.Widgets.Advanced;
using IronStar.Editor.Manipulators;

namespace IronStar.Editor.Controls 
{
	public class Workspace : Frame  
	{
		public AEPropertyGrid Grid
		{
			get { return grid; }
		}

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
		readonly List<Frame> palettes = new List<Frame>();

		Shelf	upperShelf;
		Shelf	lowerShelf;
		MapEditor editor;
		AEPropertyGrid grid;
		Panel	gridPanel;
		Label	gridLabel;

		public ITool manipulator;

		public ITool Manipulator 
		{
			get { return manipulator; }
			set 
			{
				if (!manipulator.IsManipulating) 
				{
					manipulator = value;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		public Workspace ( FrameProcessor frames, MapEditor editor ) : base( frames )
		{	
			this.editor			=	editor;

			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.Padding		=	0;

			this.X				=	0;
			this.Y				=	0;
			this.Width			=	frames.RootFrame.Width;
			this.Height			=	frames.RootFrame.Height;

			this.Anchor			=	FrameAnchor.All;

			//	set null manipulator :
			manipulator		=	new NullTool();

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

			FeedProperties(null);
		}



		/// <summary>
		/// Closes workspace and remove it from parent frame node
		/// </summary>
		public void CloseWorkspace ()
		{
			Close();
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
		public void TogglePalette ( Frame palette )
		{
			if (!palettes.Contains(palette)) 
			{
				palettes.Add(palette);
				this.Add( palette );
				palette.Visible = true;
			} 
			else 
			{
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

			foreach ( var frame in frames ) 
			{
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

			if (manipulator!=null && manipulator.IsManipulating) 
			{
				var text  = manipulator.ManipulationText;
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
			if (grid==null) 
			{
				gridPanel			=	new Panel( Frames, Width - 320, 40, 320, Height - 40 - 40 );
				gridPanel.Anchor	=	FrameAnchor.Top | FrameAnchor.Bottom | FrameAnchor.Right;
				gridPanel.Margin	=	3;

				gridPanel.Layout	=	new PageLayout()
									.AddRow( 20, -1 )
									.AddRow( -1, -1 )
									.AddRow( 20, 0.5f, 0.5f )
									;

				gridLabel	=	new Label( Frames, 0,0,0,0, "Property Grid");
				gridLabel.TextAlignment = Alignment.MiddleLeft;
				gridLabel.Padding	= 3;

				var scrollBox = new ScrollBox( Frames, 0,0,0,0 ) 
				{
					BorderColor = ColorTheme.BorderColor,
					Border		= 1,
				};

				scrollBox.ScrollMarkerSize = 5;

				grid = new AEPropertyGrid(Frames);

				grid.Width	=	300;
				grid.Height	=	500;

				scrollBox.Add( grid );

				gridPanel.Add( gridLabel );
				gridPanel.Add( scrollBox );

				gridPanel.Add( new Button(Frames, "<", 0,0,0,0, () => { gridPanel.Width = gridPanel.Width + 20; gridPanel.X = gridPanel.X - 20; } ) );
				gridPanel.Add( new Button(Frames, ">", 0,0,0,0, () => { gridPanel.Width = gridPanel.Width - 20; gridPanel.X = gridPanel.X + 20; } ) );

				Add( gridPanel );
			}

			gridPanel.Visible = (target!=null);

			grid.Frames.TargetFrame = grid;

			grid.TargetObject = target;

			gridLabel.Text = target?.ToString() ?? "null";
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

			if (!manipulator.IsManipulating)
			{
				foreach ( var hotkey in hotkeys ) 
				{
					if (hotkey.Key==e.Key) 
					{
						if ( shift == (hotkey.ModKey==ModKeys.Shift) && alt == (hotkey.ModKey==ModKeys.Alt) && ctrl == (hotkey.ModKey==ModKeys.Ctrl) ) 
						{
							hotkey.Action?.Invoke();
						}
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
			if (editor.camera.Manipulation==Manipulation.None && !manipulator.IsManipulating) 
			{
				editor.PickSelection( e.X, e.Y, e.Shift );
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

			bool useSnap	=	!Game.Keyboard.IsKeyDown(Keys.J);

			if (Game.Keyboard.IsKeyDown(Keys.LeftAlt)) 
			{
				switch (e.Key)
				{
					case Keys.LeftButton:	editor.camera.StartManipulation( e.X, e.Y, Manipulation.Rotating );	break;
					case Keys.RightButton:	editor.camera.StartManipulation( e.X, e.Y, Manipulation.Zooming );	break;
					case Keys.MiddleButton:	editor.camera.StartManipulation( e.X, e.Y, Manipulation.Translating );	break;
				}
			} 
			else 
			{
				if (!manipulator.StartManipulation( e.X, e.Y, useSnap )) 
				{
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
			manipulator.UpdateManipulation( e.X, e.Y );
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
			manipulator.StopManipulation( e.X, e.Y );
			editor.StopMarqueeSelection( e.X, e.Y );
		}
	}
}
