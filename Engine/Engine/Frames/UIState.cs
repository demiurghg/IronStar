using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Drivers.Graphics;
using System.Diagnostics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using System.IO;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Frames 
{
	/// <summary>
	/// Holds current root frame and current target frame.
	/// No events should passed behind the root frame.
	/// </summary>
	public class UIState
	{
		public readonly Game				Game;
		public readonly UIContextStack		Stack;
		public readonly Frame				RootFrame;

		public readonly MouseProcessor		Mouse;
		public readonly TouchProcessor		Touch;
		public readonly KeyboardProcessor	Keyboard;

		public Point MousePosition { get; internal set; }

		
		public UIState( FrameProcessor frames, bool inGame, int w, int h, Color color )
		{
			this.Game		=	frames.Game;
			this.RootFrame	=	new Frame( this, 0, 0, w, h, null, color);
			this.Stack		=	new UIContextStack( this, RootFrame );

			if (inGame)
			{
				Mouse	=	new MouseProcessor( frames.Game, this, true );
			}
			else
			{
				Mouse		=	new MouseProcessor( frames.Game, this, false );
				Touch		=	new TouchProcessor( frames.Game, this );
				Keyboard	=	new KeyboardProcessor( frames.Game, this );
			}
		}


		public void Update( GameTime gameTime )
		{
			Touch?.UpdateManipulations( gameTime );
			Keyboard?.UpdateKeyboard( gameTime );

			RootFrame?.UpdateInternalNonRecursive( gameTime );

			TrackActiveFrame();
		}


		public UIContext ShowDialog ( Frame dialog, Frame owner = null )
		{
			var context = Stack.PushUIContext( dialog, dialog, owner );
			dialog.ConstrainFrame( 0 );
			return context;
		}


		public UIContext ShowDialogCentered ( Frame dialog, Frame owner = null )
		{
			var context = Stack.PushUIContext( dialog, dialog, owner );
			dialog.CenterFrame();
			return context;
		}


		public UIContext ShowFullscreenFrame ( Frame fullscrFrame, Frame owner = null )
		{
			var context = Stack.PushUIContext( fullscrFrame, fullscrFrame, owner );
			fullscrFrame.ExpandFrame(0);
			return context;
		}


		public void CloseDialog( ref UIContext context )
		{
			Stack.PopUIContext( ref context );
		}


		internal bool IsModalFrame ( Frame frame )
		{
			return Stack.IsModalFrame( frame );
		}


		public Frame ContextRootFrame {
			get { return Stack.GetRootFrame(); }
		}


		public Frame TargetFrame {
			get { return Stack.GetTargetFrame(); }
			set { Stack.SetTargetFrame(value); }
		}


		bool IsAttachedToRoot(Frame frame)
		{
			var root = RootFrame;

			while (frame!=null)
			{
				if (frame==root) return true;
				frame = frame.Parent;
			}

			return false;
		}


		Frame oldTargetFrame = null;

		void TrackActiveFrame()
		{
			// #TODO #UI -- When target frame is removed from hierarchy we need to choose another good target frame
			if (TargetFrame!=null && !IsAttachedToRoot(TargetFrame))
			{
				Log.Warning("UI: Target frame lost");
				TargetFrame = null;
			}

			if (oldTargetFrame!=TargetFrame)
			{
				oldTargetFrame?.OnDeactivate();
				TargetFrame?.OnActivate();
				oldTargetFrame = TargetFrame;
			}
		}


		public Frame GetHoveredFrame ( Point location )
		{
			Frame mouseHoverFrame = null;

			var startFrame = ContextRootFrame;

			UpdateHoverRecursive( startFrame, location, ref mouseHoverFrame );

			return mouseHoverFrame;
		}


		public void UpdateHoverRecursive ( Frame frame, Point p, ref Frame mouseHoverFrame )
		{
			if (frame==null) 
			{
				return;
			}

			var absLeft		=	frame.GlobalRectangle.Left;
			var absTop		=	frame.GlobalRectangle.Top;
			var absRight	=	frame.GlobalRectangle.Right;
			var absBottom	=	frame.GlobalRectangle.Bottom;

			if (!frame.CanAcceptControl) 
			{
				return;
			}
			
			bool hovered	=	p.X >= absLeft 
							&&	p.X <  absRight 
							&&	p.Y >= absTop
							&&	p.Y <  absBottom;

			if (hovered) 
			{
				mouseHoverFrame = frame;
				foreach (var child in frame.Children) 
				{
					UpdateHoverRecursive( child, p, ref mouseHoverFrame );
				}
			}
		}
	}
}
