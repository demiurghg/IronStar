﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class AECollapseRegion : Frame {

			readonly AEPropertyGrid	grid;

			public readonly string Category;

			bool visible = true;

			Frame buttonCollapse;
			bool collapsed = false;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AECollapseRegion ( AEPropertyGrid grid, string category ) : base(grid.Frames)
			{ 
				this.grid		=	grid;
				this.Category	=	category;

				TextAlignment	=	Alignment.MiddleCenter;
				BorderColor		=	ColorBorder;
				Border			=	0;
				Padding			=	0;
				PaddingBottom	=	0;

				Width			=	100;

				buttonCollapse		=	new Frame( grid.Frames ) {
					Width			=	0, // does not matter due to layout engine
					Height			=	12,
					Border			=	1,
					Text			=	category,
					TextAlignment	=	Alignment.MiddleCenter,
					BorderColor		=	ButtonBorderColor,
					BackColor		=	ButtonColorNormal,
					ForeColor		=	TextColorNormal,
					MarginBottom	=	0,
				};

				Add( buttonCollapse );

				buttonCollapse.StatusChanged +=ButtonCollapse_StatusChanged;
				buttonCollapse.Click+=ButtonCollapse_Click;

				Layout	=	new StackLayout(0,1,true) { AllowResize = true };
			}



			public void Collapse ( bool collapse )
			{
				collapsed = collapse;

				foreach ( var child in Children ) {
					if (child!=buttonCollapse) {
						child.Visible = !collapsed;
					}
				}

				grid.RunLayout();
			}



			private void ButtonCollapse_Click( object sender, MouseEventArgs e )
			{
				Collapse( !collapsed );
			}



			private void ButtonCollapse_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		
						buttonCollapse.ForeColor	=	TextColorNormal;	
						buttonCollapse.BackColor	=	ButtonColorNormal;	
						buttonCollapse.TextOffsetX	=	0;
						buttonCollapse.TextOffsetY =	0;	
						break;
					case FrameStatus.Hovered:	
						buttonCollapse.ForeColor	=	TextColorHovered;	
						buttonCollapse.BackColor	=	ButtonColorHovered;	
						buttonCollapse.TextOffsetX	=	0;
						buttonCollapse.TextOffsetY =	0;	
						break;
					case FrameStatus.Pushed:	
						buttonCollapse.ForeColor	=	TextColorPushed;	
						buttonCollapse.BackColor	=	ButtonColorPushed;		
						buttonCollapse.TextOffsetX	=	1;
						buttonCollapse.TextOffsetY =	0;	
					break;
				}
			}
		}

	}
}
