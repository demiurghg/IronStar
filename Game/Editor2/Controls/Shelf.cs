using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;
using System.Windows;
using Fusion.Core.Mathematics;

namespace IronStar.Editor2.Controls {

	public enum ShelfMode {
		Top,
		Bottom,
	}


	public class Shelf : Panel {

		readonly ShelfMode ShelfMode;
		readonly List<Frame> itemsRight = new List<Frame>();
		readonly List<Frame> itemsLeft  = new List<Frame>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public Shelf ( Frame parent, ShelfMode shelfMode ) : base(parent.Frames, 0,0,600,40)
		{
			this.ShelfMode	=	shelfMode;
			
			Width	=	parent.Width;
			Height	=	40;

			parent.Add( this );

			switch (shelfMode) {
				case ShelfMode.Top:		
					Anchor	=	FrameAnchor.Left | FrameAnchor.Right | FrameAnchor.Top; 
					Y		=	0;
					break;

				case ShelfMode.Bottom:	
					Anchor	=	FrameAnchor.Left | FrameAnchor.Right | FrameAnchor.Bottom; 
					Y		=	parent.Height - Height;
					break;

				default: 
					throw new ArgumentException("shelfMode");
			}
		}


		public void ClearShelf ()
		{
			itemsRight.Clear();
			itemsLeft.Clear();
			Clear();
		}




		public override void RunLayout()
		{
			var gp = GetPaddedRectangle(false);

			int advanceX = gp.X;

			foreach (var frame in itemsLeft) {
				frame.X = advanceX;
				frame.Y = gp.Y;
				advanceX += frame.Width;
				advanceX += 1;
			}

			advanceX = gp.Width - (itemsRight.Sum( f => (f.Width+1) ) - 1) + 1;

			foreach (var frame in itemsRight) {
				frame.X = advanceX;
				frame.Y = gp.Y;
				advanceX += frame.Width;
				advanceX += 1;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddLButton ( string text, string image, Action action )
		{
			var button = new Button( Frames, text, 0,0,34,34, action);
			button.ShadowColor = ColorTheme.ShadowColor;
			button.ShadowOffset = new Vector2(1,1);
			itemsLeft.Add( button );
			Add( button );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddLSplitter ( int width = 17 )
		{
			var splitter = new Frame( Frames, 0,0,width,34, "", Color.Zero);
			itemsLeft.Add( splitter );
			Add( splitter );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddRSplitter ( int width = 17 )
		{
			var splitter = new Frame( Frames, 0,0,width,34, "", Color.Zero);
			itemsRight.Add( splitter );
			Add( splitter );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddFatLButton ( string text, string image, Action action )
		{
			var button = new Button( Frames, text, 0,0,34+34+1,34, action);
			button.ShadowColor = ColorTheme.ShadowColor;
			button.ShadowOffset = new Vector2(1,1);
			button.Padding = 1;
			button.TextAlignment = Alignment.MiddleCenter;
			itemsLeft.Add( button );
			Add( button );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddFatRButton ( string text, string image, Action action )
		{
			var button = new Button( Frames, text, 0,0,34+34+1,34, action);
			button.ShadowColor = ColorTheme.ShadowColor;
			button.ShadowOffset = new Vector2(1,1);
			itemsRight.Add( button );
			Add( button );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="image"></param>
		/// <param name="action"></param>
		public void AddRButton ( string text, string image, Action action )
		{
			var button = new Button( Frames, text, 0,0,34,34, action);
			button.ShadowColor = ColorTheme.ShadowColor;
			button.ShadowOffset = new Vector2(1,1);
			itemsRight.Add( button );
			Add( button );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="width"></param>
		/// <returns></returns>
		public Frame AddRIndicator ( string text, int width )
		{
			var label = new Frame( Frames, 0, 0, width, 34, text, ColorTheme.BackgroundColorDark );
			
			label.ForeColor		= ColorTheme.TextColorNormal;
			label.ShadowColor	= ColorTheme.ShadowColor;
			label.ShadowOffset	= new Vector2(1,1);
			label.Border		= 1;
			label.BorderColor	= ColorTheme.BorderColor;
			label.TextAlignment	= Alignment.MiddleLeft;
			label.Padding		= 1;

			itemsRight.Add( label );
			Add( label );

			return label;
		}


	}
}
