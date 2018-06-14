using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Frames;


namespace Fusion.Engine.Frames.Layouts {

	public class PageLayout : LayoutEngine {

		readonly int headerHeight1;
		readonly int headerHeight2;
		readonly int numColumns;
		readonly int buttonHeights;
		readonly int buttonNum;
		readonly int footerHeight;
		readonly int gap;

		public PageLayout( int headerHeight1, int headerHeight2, int numColumns, int buttonHeights, int buttonNum, int footerHeight, int gap )
		{
			this.headerHeight1	=	headerHeight1	;
			this.headerHeight2	=	headerHeight2	;
			this.numColumns		=	numColumns		;	
			this.buttonHeights	=	buttonHeights	;
			this.buttonNum		=	buttonNum		;
			this.footerHeight	=	footerHeight	;
			this.gap			=	gap				;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetFrame"></param>
		/// <param name="forceTransitions"></param>
		public override void RunLayout ( Frame targetFrame )
		{
			var gp = targetFrame.GetPaddedRectangle(false);
			var w  = gp.Width;
			var h  = gp.Height;

			var x  = gp.X;
			var y  = gp.Y;
			var index = 0;

			// header 1
			SetChildSize( targetFrame, index,  x, y,  w, headerHeight1 );
			index++;
			y += headerHeight1;
			y += gap;

			// header 2
			if (headerHeight2>0) {
				SetChildSize( targetFrame, index,  x, y,  w, headerHeight2 );
				index++;
				y += headerHeight2;
				y += gap;
			}

			// body columns
			int bodyHeight = gp.Height - (headerHeight1 + gap) - (headerHeight2 + gap) - (buttonHeights + gap) - (footerHeight + gap);

			for (int col=0; col<numColumns; col++) {
				int bodyWidth = gp.Width / numColumns - gap;
				SetChildSize( targetFrame, index,  x+(bodyWidth+gap)*col, y,  bodyWidth, bodyHeight );
				index++;
			}
			y += bodyHeight;
			y += gap;

			// button bar :
			y = gp.Height - buttonHeights - footerHeight - gap;

			int btnHeight = buttonHeights;

			for (int btn=0; btn<buttonNum; btn++) {
				int btnWidth = gp.Width / buttonNum - gap;
				SetChildSize( targetFrame, index,  x+(btnWidth+gap)*btn, y + gp.Y,  btnWidth, btnHeight );
				index++;
			}

			//	footer :
			SetChildSize( targetFrame, index,  x, gp.Height - footerHeight + gp.Y,  w, footerHeight );
		}



		void SetChildSize( Frame targetFrame, int index, int x, int y, int w, int h )
		{
			if (index>=targetFrame.Children.Count()) {
				return;
			}

			var child = targetFrame.Children.ElementAt(index);

			child.X = x;
			child.Y = y;
			child.Width = w;
			child.Height = h;
		}
		
	}
}
