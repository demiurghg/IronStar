using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;
using Fusion.Widgets;

namespace Fusion.Widgets {

	public class Palette : Panel 
	{
		Frame		captionFrame;
		Frame		itemList;
		ScrollBox	scrollBox;
		Button		closeButton;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="frames"></param>
		/// <param name="caption"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public Palette ( UIState ui, string caption, int x, int y, int w, int h ) : base(ui, x,y,w,h)
		{
			AllowDrag	=	true;
			AllowResize	=	true;

			var pageLayout = new PageLayout();
			pageLayout.AddRow(  17, new[] { -1f } );
			pageLayout.AddRow( -1f, new[] { -1f } );
			pageLayout.AddRow(  25, new[] { -1f } );

			Layout	=	pageLayout;

			captionFrame	=	new Label( ui, 0,0,0,0, caption ) { TextAlignment = Alignment.MiddleLeft };
			scrollBox		=	new ScrollBox( ui, 0,0,0,0 );
			closeButton		=	new Button( ui, "Close", 0,0,0,0, () => Visible = false );

			itemList		=	new Frame( ui, 0,0,0,0, "", Color.Zero );
			itemList.Layout	=	new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };

			Add( captionFrame );
			Add( scrollBox );
			Add( closeButton );

			scrollBox.Add( itemList );
		}


		/// <summary>
		/// Adds splitter to palette 
		/// </summary>
		/// <param name="text"></param>
		/// <param name="action"></param>
		public void AddSplitter()
		{
			itemList.Add( new Frame( ui, 0,0,0,10, "", Color.Zero ) );
		}


		/// <summary>
		/// Adds button to palette
		/// </summary>
		/// <param name="text"></param>
		/// <param name="action"></param>
		public void AddButton( string text, Action action )
		{
			var button	=	new Button( ui, text, 0, 0, 0, 23, action );

			button.TextAlignment = Alignment.MiddleLeft;
			button.PaddingLeft	 = 5;
			button.PaddingRight	 = 5;

			itemList.Add( button );
		}
	}
}
