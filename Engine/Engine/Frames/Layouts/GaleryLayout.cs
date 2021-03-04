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


namespace Fusion.Engine.Frames.Layouts 
{
	public enum GaleryGrowMode
	{
		Vertical
	}

	public class GaleryLayout : LayoutEngine 
	{
		public int	Interval	{ get; set; } = 0;
		public int  ItemWidth	{ get; set; } = 64;
		public int  ItemHeight	{ get; set; } = 64;
		public GaleryGrowMode GrowMode { get; set; } = GaleryGrowMode.Vertical;

		public GaleryLayout(int itemWidth, int itemHeight, int interval)
		{
			Interval	=	interval;
			ItemWidth	=	itemWidth;
			ItemHeight	=	itemHeight;
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
			
			int numColumns	=	Math.Max(1, targetFrame.GetPaddedRectangle(false).Width / ItemWidth);
			
			foreach ( var child in targetFrame.Children ) 
			{
				if (!child.Visible) 
				{
					continue;
				}

				int row	=	index / numColumns;
				int col	=	index % numColumns;

				child.X			=	(ItemWidth + Interval)  * col;
				child.Y			=	(ItemHeight + Interval) * row;
				child.Width		=	ItemWidth;
				child.Height	=	ItemHeight;

				index++;
			}

			int totalRows		=	MathUtil.IntDivRoundUp( index, numColumns );
			int totalCols		=	numColumns;

			//targetFrame.Width	=	(ItemWidth  + Interval) * totalCols;
			targetFrame.Height	=	(ItemHeight + Interval) * totalRows;
		}

		
	}
}
