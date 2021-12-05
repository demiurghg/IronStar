using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.UI.Controls {

	public class Button : Frame {

		readonly Action action;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public Button ( UIState ui, string name, int x, int y, int w, int h, Action action ) : base(ui)
		{ 
			this.Font		=	MenuTheme.NormalFont;

			this.action		=	action;
				
			Width			=	w;
			Height			=	h;
			X				=	x;
			Y				=	y;

			TextAlignment	=	Alignment.MiddleCenter;
			Border			=	0;
			Text			=	name;

			ShadowColor		=	MenuTheme.ShadowColor;
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
					//ForeColor	=	(action == null) ? Color.Red : MenuTheme.TextColorNormal;	
					ForeColor	=	MenuTheme.TextColorNormal;	
					BackColor	=	MenuTheme.ButtonColorNormal;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	MenuTheme.TextColorHovered;	
					BackColor	=	MenuTheme.ButtonColorHovered;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	MenuTheme.TextColorPushed;
					BackColor	=	MenuTheme.ButtonColorPushed;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	1;
					TextOffsetY =	0;	
				break;
			}
		}
	}
}
