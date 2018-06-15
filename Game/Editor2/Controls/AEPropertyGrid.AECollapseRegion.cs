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
using Fusion.Engine.Frames.Layouts;
using Fusion.Core;

namespace IronStar.Editor2.Controls {

	public partial class AEPropertyGrid : Frame {

		class AECollapseRegion : Frame {

			readonly AEPropertyGrid	grid;
			readonly int nestingLevel;

			public readonly string Category;

			Frame buttonCollapse;
			bool collapsed = true;

			object enclosingObject;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AECollapseRegion ( AEPropertyGrid grid, string category, int nestingLevel, object obj ) : base(grid.Frames)
			{ 
				enclosingObject		=	obj;

				this.grid			=	grid;
				this.Category		=	category;
				this.nestingLevel	=	nestingLevel;

				TextAlignment	=	Alignment.MiddleCenter;
				BorderColor		=	ColorTheme.BackgroundColorDark;
				BackColor		=	Color.Zero;
				Border			=	0;
				Padding			=	0;
				BorderLeft		=	3;
				PaddingLeft		=	1;

				Width			=	100;

				Collapsed		=	true;
				Collapsed		=	nestingLevel > 0;

				buttonCollapse		=	new Frame( grid.Frames ) {
					Width			=	0, // does not matter due to layout engine
					Height			=	12,
					Border			=	1,
					Text			=	category,
					TextAlignment	=	Alignment.MiddleCenter,
					BorderColor		=	ColorTheme.ButtonBorderColor,
					BackColor		=	ColorTheme.ButtonColorNormal,
					ForeColor		=	ColorTheme.TextColorNormal,
					MarginBottom	=	0,
				};

				base.Add( buttonCollapse );

				buttonCollapse.StatusChanged +=ButtonCollapse_StatusChanged;
				buttonCollapse.Click+=ButtonCollapse_Click;

				Layout	=	new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };
			}



			protected override void DrawFrame(GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex)
			{
				base.DrawFrame(gameTime, spriteLayer, clipRectIndex);

				//if (enclosingObject==null) {
				//	Text = Category;
				//} else {
				//	Text = Category + ": " + enclosingObject.ToString();
				//}
			}


			public override void Add( Frame frame )
			{
				base.Add( frame );
				if (collapsed) {
					frame.Visible = false;
				}
			}


			public bool Collapsed {
				get {
					return collapsed;
				}
				set {
					if (collapsed!=value) {
						collapsed = value;

						MarginBottom = (collapsed || nestingLevel==0) ? 0 : 2;

						foreach ( var child in Children ) {
							if (child!=buttonCollapse) {
								child.Visible = !collapsed;
							}
						}

						MakeLayoutDirty();
					}
				}
			}



			private void ButtonCollapse_Click( object sender, MouseEventArgs e )
			{
				Collapsed = !Collapsed;
			}



			private void ButtonCollapse_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		
						buttonCollapse.ForeColor	=	ColorTheme.TextColorNormal;	
						buttonCollapse.BackColor	=	ColorTheme.ButtonColorNormal;	
						buttonCollapse.TextOffsetX	=	0;
						buttonCollapse.TextOffsetY =	0;	
						break;
					case FrameStatus.Hovered:	
						buttonCollapse.ForeColor	=	ColorTheme.TextColorHovered;	
						buttonCollapse.BackColor	=	ColorTheme.ButtonColorHovered;	
						buttonCollapse.TextOffsetX	=	0;
						buttonCollapse.TextOffsetY =	0;	
						break;
					case FrameStatus.Pushed:	
						buttonCollapse.ForeColor	=	ColorTheme.TextColorPushed;	
						buttonCollapse.BackColor	=	ColorTheme.ButtonColorPushed;		
						buttonCollapse.TextOffsetX	=	1;
						buttonCollapse.TextOffsetY =	0;	
					break;
				}


				switch ( e.Status ) {
					case FrameStatus.None:		
						BorderColor		=	ColorTheme.BorderColorLight;
						break;
					case FrameStatus.Hovered:	
						BorderColor		=	ColorTheme.ButtonColorHovered;
						break;
					case FrameStatus.Pushed:	
						BorderColor		=	ColorTheme.ButtonColorPushed;
						break;
				}
			}
		}

	}
}
