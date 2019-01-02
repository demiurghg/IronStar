using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.UI.Controls {

	public class BigButton : Frame {

		readonly Action action;

		public bool RedButton {
			get; set;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public BigButton ( FrameProcessor frames, string name, int x, int y, int w, int h, Action action ) : base(frames)
		{ 
			this.Font		=	MenuTheme.NormalFont;

			this.action		=	action;
				
			Width			=	w;
			Height			=	h;
			X				=	x;
			Y				=	y;

			PaddingLeft		=	20;

			TextAlignment	=	Alignment.MiddleLeft;
			Border			=	0;
			BorderLeft		=	4;
			BorderColor		=	MenuTheme.Transparent;
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
					BackColor	=	MenuTheme.BigButtonColorNormal;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					BorderColor	=	MenuTheme.Transparent;
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	MenuTheme.TextColorHovered;	
					BackColor	=	MenuTheme.BigButtonColorHovered;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					BorderColor	=	MenuTheme.TextColorHovered;
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	MenuTheme.TextColorPushed;
					BackColor	=	MenuTheme.BigButtonColorPushed;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	1;
					TextOffsetY =	0;	
					BorderColor	=	MenuTheme.TextColorHovered;
				break;
			}
		}
	}
}
