using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.Editor2.Controls {

	class Button : Frame {

		readonly Action action;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public Button ( FrameProcessor frames, string name, int x, int y, int w, int h, Action action ) : base(frames)
		{ 
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
					ForeColor	=	(action == null) ? Color.Red : ColorTheme.TextColorNormal;	
					BackColor	=	ColorTheme.ButtonColorNormal;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	ColorTheme.TextColorHovered;	
					BackColor	=	ColorTheme.ButtonColorHovered;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	ColorTheme.TextColorPushed;	
					BackColor	=	ColorTheme.ButtonColorPushed;		
					ImageColor	=	ForeColor;
					TextOffsetX	=	1;
					TextOffsetY =	0;	
				break;
			}
		}
	}
}
