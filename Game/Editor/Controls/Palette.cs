using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.Editor.Controls {

	public class Palette : Panel {

		Frame  splitter;
		Button closeButton;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frames"></param>
		/// <param name="caption"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public Palette ( FrameProcessor frames, string caption, int x, int y, int w, int h ) : base(frames, x,y,w,h)
		{
			Layout = new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

			Add( new Label( Frames, 0,0,w,12, caption ) { TextAlignment = Alignment.MiddleCenter } );

			Add( new Frame( Frames, 0,0,0,10, "", Color.Zero ) );
			Add( new Button( Frames, "Close", 0,0,w,20, () => Visible = false ) );
		}


		/// <summary>
		/// Adds splitter to palette 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="action"></param>
		public void AddSplitter()
		{
			int count = Children.Count();
			Insert(count-2, new Frame( Frames, 0,0,0,10, "", Color.Zero ) );
		}


		/// <summary>
		/// Adds button to palette
		/// </summary>
		/// <param name="text"></param>
		/// <param name="action"></param>
		public void AddButton( string text, Action action )
		{
			int count	=	Children.Count();
			var button	=	new Button( Frames, text, 0,0,Width,20, action );

			button.TextAlignment = Alignment.MiddleLeft;
			button.PaddingLeft	 = 5;
			button.PaddingRight	 = 5;

			Insert(count-2, button );
		}
	}
}
