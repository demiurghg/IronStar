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
	public class Shelf : Panel {

		readonly List<Frame> itemsRight = new List<Frame>();
		readonly List<Frame> itemsLeft  = new List<Frame>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public Shelf ( FrameProcessor fp ) : base(fp, 0,0,600,40)
		{
			Width	=	fp.RootFrame.Width;
			Height	=	40;

			fp.RootFrame.Add( this );

			this.Layout	=	new StackLayout() {
				AllowResize = false,
				Interval = 1,
				EqualWidth = false,
				StackingDirection = StackLayout.Direction.HorizontalStack,
			};

			this.Anchor	=	FrameAnchor.Left | FrameAnchor.Right | FrameAnchor.Top;

			AddLButton("A", "", null);
			AddLButton("B", "", null);
			AddLButton("C", "", null);
			AddLButton("D", "", null);

			AddRButton("TB", "", null);
			AddRButton("AE", "", null);
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
		public void AddRButton ( string text, string image, Action action )
		{
			var button = new Button( Frames, text, 0,0,34,34, action);
			button.ShadowColor = ColorTheme.ShadowColor;
			button.ShadowOffset = new Vector2(1,1);
			itemsRight.Add( button );
			Add( button );
		}

	}
}
