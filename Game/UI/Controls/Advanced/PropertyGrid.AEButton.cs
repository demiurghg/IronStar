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
using Fusion.Core;

namespace IronStar.UI.Controls.Advanced {

	public partial class PropertyGrid : Frame {

		class AEButton : AEBaseEditor {

			readonly Action action;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEButton ( PropertyGrid grid, string name, Action action ) : base(grid, name)
			{ 
				this.action		=	action;
				
				Width			=	grid.Width;
				Height			=	23;

				TextAlignment	=	Alignment.MiddleCenter;
				Border			=	0;
				Text			=	name;

				StatusChanged	+=	AESlider_StatusChanged;
				Click			+=	AEButton_Click;

				Update(new GameTime());
			}



			private void AEButton_Click( object sender, MouseEventArgs e )
			{
				action();
			}



			private void AESlider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		
						ForeColor	=	MenuTheme.TextColorNormal;	
						BackColor	=	MenuTheme.ButtonColorNormal;	
						TextOffsetX	=	0;
						TextOffsetY =	0;	
						break;
					case FrameStatus.Hovered:	
						ForeColor	=	MenuTheme.TextColorHovered;	
						BackColor	=	MenuTheme.ButtonColorHovered;	
						TextOffsetX	=	0;
						TextOffsetY =	0;	
						break;
					case FrameStatus.Pushed:	
						ForeColor	=	MenuTheme.TextColorPushed;	
						BackColor	=	MenuTheme.ButtonColorPushed;		
						TextOffsetX	=	1;
						TextOffsetY =	0;	
					break;
				}
			}


			protected override void Update( GameTime gameTime )
			{
				MarginRight = grid.Width/3;
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
