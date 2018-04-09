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

	public class StackLayout : LayoutEngine {

		public int	Offset		{ get; set; }
		public int	Interval	{ get; set; }
		public bool EqualWidth	{ get; set; }
		public bool AllowResize { get; set; } = false;


		public StackLayout( int offset, int interval, bool equalWidth = false )
		{
			Offset		=	offset;
			Interval	=	interval;
			EqualWidth	=	equalWidth;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetFrame"></param>
		/// <param name="forceTransitions"></param>
		public override void RunLayout ( Frame targetFrame )
		{
			int offset = Offset;
			var gp = targetFrame.GetPaddedRectangle(false);

			foreach ( var child in targetFrame.Children ) {

				if (!child.Visible) {
					continue;
				}

				child.X = gp.X + child.MarginLeft;
				child.Y = gp.Y + child.MarginTop  + offset;

				offset += child.MarginLeft;
				offset += child.Height;
				offset += Interval;
				offset += child.MarginBottom;

				if (EqualWidth) {
					child.Width = gp.Width - child.MarginLeft - child.MarginRight;
				}

			}

			if (AllowResize) {
				targetFrame.Height = offset
					 + targetFrame.PaddingTop
					 + targetFrame.PaddingBottom
					 + targetFrame.BorderTop
					 + targetFrame.BorderBottom
					 - Interval
					 ;
			}
		}

		
	}
}
