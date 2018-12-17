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

		public int Margin {
			get { return gap; }
			set { gap = value; }
		}

		int gap = 1;

		struct Row {
			public float Height;
			public float[] Width;
		}

		readonly List<Row> rows =	new List<Row>();


		public PageLayout()
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="height"></param>
		/// <param name="width"></param>
		public void AddRow ( float height, float[] width )
		{
			rows.Add( new Row() { Height = height, Width = width.ToArray() } );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="total"></param>
		/// <param name="margin"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		int[] ComputeSizes ( int total, int margin, float[] measures )
		{
			var length		=	measures.Length;
			var sizes		=	new int[ measures.Length ];
			int free		=	total - (margin * (length-1));
			var spring		=	measures.Sum( v => (v >= 0 && v < 1) ? v : 0 );
			var freeCount	=	measures.Count( v => v < 0 );

			//	sum fixed values:
			for ( int i=0; i<length; i++ ) {
				if (measures[i]>=1) {
					sizes[i] =	(int)measures[i];
					free	 -=	sizes[i];
				}
			}

			//	fixed elements get too much space:
			if (free<=0) {
				return sizes;
			}

			int free2 = free;

			//	spring elements :
			for ( int i=0; i<length; i++ ) {
				if (measures[i]>=0 && measures[i]<1) {
					sizes[i] =	(int)(measures[i] * free2);
					free	 -=	sizes[i];
				}
			}

			//	fixed elements get too much space:
			if (free<=0) {
				return sizes;
			}

			//	free elements :
			if (freeCount>0) {
				free /= freeCount;

				for ( int i=0; i<length; i++ ) {
					if (measures[i]<0) {
						sizes[i] =	free;
					}
				}
			}

			//	fix errors :
			/*int error = total - sizes.Sum();

			if (error>0) {
				
			} */

			return sizes;
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
			var rowHeights	=	ComputeSizes( h, gap, rows.Select( r => r.Height ).ToArray() );
			
			for ( int i=0; i<rows.Count; i++ ) {

				x	=	gp.X;
				var colWidths	=	ComputeSizes( w, gap, rows[i].Width );

				for ( int j=0; j<colWidths.Length; j++ ) {
					SetChildSize( targetFrame, index, x,y, colWidths[j], rowHeights[i] );
					x += colWidths[j];
					x += gap;

					index++;
				}
				
				y += rowHeights[i];
				y += gap;
			}

			#if false

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
			#endif
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
