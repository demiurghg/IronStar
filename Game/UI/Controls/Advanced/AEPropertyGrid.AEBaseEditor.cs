using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using IronStar.UI.Controls;

namespace IronStar.UI.Controls.Advanced {

	public partial class AEPropertyGrid : Frame {

		class AEBaseEditor : Frame {

			readonly public string Name;
			readonly protected AEPropertyGrid	grid;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEBaseEditor ( AEPropertyGrid grid, string name ) : base(grid.Frames)
			{ 
				this.Font		=	ColorTheme.NormalFont;

				this.BackColor	=	Color.Zero;
				this.grid		=	grid;
				this.Name		=	name;

				this.Height		=	Font.LineHeight + 4;
			}


			protected int ComputeItemHeight ()
			{
				return GetFontHeight() + 2 * VerticalPadding;
			}


			public override void RunLayout()
			{
				base.RunLayout();

				TextAlignment	=	Alignment.MiddleRight;
				TextOffsetX		=	-Width / 2 - 8;

				Text			=	Name;
			}
		}

	}
}
