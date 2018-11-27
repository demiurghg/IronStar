using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace Fusion.Widgets {

	public class Button : Frame {

		readonly Action action;

		public bool RedButton {
			get; set;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public Button ( FrameProcessor frames, string name, int x, int y, int w, int h, Action action ) : base(frames)
		{ 
			this.Font		=	ColorTheme.Font;

			this.action		=	action;
				
			Width			=	w;
			Height			=	h;
			X				=	x;
			Y				=	y;

			TextAlignment	=	Alignment.MiddleCenter;
			Border			=	1;
			BorderColor		=	ColorTheme.ButtonBorderColor;
			Text			=	name;

			ShadowColor		=	ColorTheme.ShadowColor;
			ShadowOffset	=	new Vector2(1,1);

			StatusChanged	+=	Button_StatusChanged;
			Click			+=	Button_Click;
		}



		private void Button_Click( object sender, MouseEventArgs e )
		{
			action?.Invoke();
		}



		private void Button_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) {
				case FrameStatus.None:		
					//ForeColor	=	(action == null) ? Color.Red : ColorTheme.TextColorNormal;	
					ForeColor	=	ColorTheme.TextColorNormal;	
					BackColor	=	RedButton ? ColorTheme.ButtonRedColorNormal : ColorTheme.ButtonColorNormal;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	ColorTheme.TextColorHovered;	
					BackColor	=	RedButton ? ColorTheme.ButtonRedColorHovered : ColorTheme.ButtonColorHovered;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	ColorTheme.TextColorPushed;
					BackColor	=	RedButton ? ColorTheme.ButtonRedColorPushed : ColorTheme.ButtonColorPushed;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	1;
					TextOffsetY =	0;	
				break;
			}
		}
	}
}
