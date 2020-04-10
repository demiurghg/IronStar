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

	public class GaleryLayout : LayoutEngine {

		public int	Interval	{ get; set; } = 0;
		public int  ItemWidth	{ get; set; } = 64;
		public int  ItemHeight	{ get; set; } = 64;
		public int  NumColumns  { get; set; } = 5;

		public GaleryLayout(int numColumns, int itemWidth, int itemHeight, int interval)
		{
			Interval	=	interval;
			ItemWidth	=	itemWidth;
			ItemHeight	=	itemHeight;
			NumColumns  =	numColumns;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetFrame"></param>
		/// <param name="forceTransitions"></param>
		public override void RunLayout ( Frame targetFrame )
		{
			var gp = targetFrame.GetPaddedRectangle(false);
			var index = 0;
			
			foreach ( var child in targetFrame.Children ) {

				if (!child.Visible) {
					continue;
				}

				int row	=	index / NumColumns;
				int col	=	index % NumColumns;

				child.X			=	(ItemWidth + Interval)  * col;
				child.Y			=	(ItemHeight + Interval) * row;
				child.Width		=	ItemWidth;
				child.Height	=	ItemHeight;

				index++;
			}

			int totalRows		=	MathUtil.IntDivRoundUp( index, NumColumns );
			int totalCols		=	NumColumns;

			targetFrame.Width	=	(ItemWidth  + Interval) * totalCols;
			targetFrame.Height	=	(ItemHeight + Interval) * totalRows;
		}

		
	}
}
