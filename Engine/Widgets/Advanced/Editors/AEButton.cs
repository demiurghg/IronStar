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
using Fusion.Widgets;
using Fusion.Core;

namespace Fusion.Widgets.Advanced 
{
	class AEButton : AEBaseEditor 
	{
		readonly Action action;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public AEButton ( AEPropertyGrid grid, string name, Action action ) : base(grid, name)
		{ 
			this.action		=	action;
				
			Width			=	grid.Width;
			Height			=	23;

			TextAlignment	=	Alignment.MiddleCenter;
			Border			=	1;
			BorderColor		=	ColorTheme.ButtonBorderColor;
			Text			=	name;

			StatusChanged	+=	AESlider_StatusChanged;
			Click			+=	AEButton_Click;

			Update(GameTime.Zero);
		}



		private void AEButton_Click( object sender, MouseEventArgs e )
		{
			action();
		}



		private void AESlider_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) {
				case FrameStatus.None:		
					ForeColor	=	ColorTheme.TextColorNormal;	
					BackColor	=	ColorTheme.ButtonColorNormal;	
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	ColorTheme.TextColorHovered;	
					BackColor	=	ColorTheme.ButtonColorHovered;	
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	ColorTheme.TextColorPushed;	
					BackColor	=	ColorTheme.ButtonColorPushed;		
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
