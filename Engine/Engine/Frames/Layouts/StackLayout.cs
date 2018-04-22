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

		public enum Direction {
			VerticalStack,
			HorizontalStack,
		}

		public int	Offset		{ get; set; } = 0;
		public int	Interval	{ get; set; } = 0;
		public bool EqualWidth	{ get; set; } = false;
		public bool AllowResize { get; set; } = false;

		public Direction StackingDirection { get; set; } = Direction.VerticalStack;


		public StackLayout()
		{
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

			bool vertical = StackingDirection==Direction.VerticalStack;


			foreach ( var child in targetFrame.Children ) {

				if (!child.Visible) {
					continue;
				}

				if (vertical) {
					child.X = gp.X + child.MarginLeft;
					child.Y = gp.Y + child.MarginTop  + offset;
				} else {
					child.X = gp.X + child.MarginLeft + offset;
					child.Y = gp.Y + child.MarginTop;
				}

				if (vertical) {
					offset += child.MarginTop;
					offset += child.Height;
					offset += Interval;
					offset += child.MarginBottom;
				} else {
					offset += child.MarginLeft;
					offset += child.Width;
					offset += Interval;
					offset += child.MarginRight;
				}

				if (EqualWidth) {
					if (vertical) {
						child.Width = gp.Width - child.MarginLeft - child.MarginRight;
					} else {
						child.Height = gp.Height - child.MarginTop - child.MarginBottom;
					}
				}

			}

			if (AllowResize) {
				if (vertical) {
					targetFrame.Height = offset
							+ targetFrame.PaddingTop
							+ targetFrame.PaddingBottom
							+ targetFrame.BorderTop
							+ targetFrame.BorderBottom
							- Interval;
				} else {
					targetFrame.Width = offset
							+ targetFrame.PaddingLeft
							+ targetFrame.PaddingRight
							+ targetFrame.BorderLeft
							+ targetFrame.BorderRight
							- Interval;
				}
							
			}
		}

		
	}
}
