﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.UI.Controls {

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
			this.Font		=	MenuTheme.NormalFont;

			this.action		=	action;
				
			Width			=	w;
			Height			=	h;
			X				=	x;
			Y				=	y;

			TextAlignment	=	Alignment.MiddleCenter;
			Border			=	1;
			BorderColor		=	MenuTheme.ButtonBorderColor;
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
					BackColor	=	RedButton ? MenuTheme.ButtonRedColorNormal : MenuTheme.ButtonColorNormal;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Hovered:	
					ForeColor	=	MenuTheme.TextColorHovered;	
					BackColor	=	RedButton ? MenuTheme.ButtonRedColorHovered : MenuTheme.ButtonColorHovered;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	0;
					TextOffsetY =	0;	
					break;
				case FrameStatus.Pushed:	
					ForeColor	=	MenuTheme.TextColorPushed;
					BackColor	=	RedButton ? MenuTheme.ButtonRedColorPushed : MenuTheme.ButtonColorPushed;	
					ImageColor	=	ForeColor;
					TextOffsetX	=	1;
					TextOffsetY =	0;	
				break;
			}
		}
	}
}
