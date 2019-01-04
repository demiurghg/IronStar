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

	public partial class PropertyGrid : Frame {

		class AEBaseEditor : Frame {

			readonly public string Name;
			readonly protected PropertyGrid	grid;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEBaseEditor ( PropertyGrid grid, string name ) : base(grid.Frames)
			{ 
				this.Font			=	MenuTheme.NormalFont;

				this.BackColor		=	Color.Zero;
				this.grid			=	grid;
				this.Name			=	name;

				this.PaddingLeft	=	10;
				this.PaddingRight	=	10;

				this.StatusChanged  +=AEBaseEditor_StatusChanged;
			}


			private void AEBaseEditor_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	MenuTheme.TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	MenuTheme.TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	MenuTheme.TextColorPushed; break;
				}
				switch ( e.Status ) {
					case FrameStatus.None:		BackColor	=	MenuTheme.Transparent;	break;
					case FrameStatus.Hovered:	BackColor	=	MenuTheme.ElementLineHighlight;	break;
					case FrameStatus.Pushed:	BackColor	=	MenuTheme.ElementLineHighlight;	break;
				}
			}


			protected int ComputeItemHeight ()
			{
				return GetFontHeight() + 2 * VerticalPadding;
			}


			public override void RunLayout()
			{
				base.RunLayout();

				TextAlignment	=	Alignment.MiddleLeft;
				Text			=	Name;
			}
		}

	}
}
