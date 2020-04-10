﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace Fusion.Widgets {
	public static class MessageBox {

		[Obsolete("Use FrameProcessor.ShowDialog")]
		static void ShowDialog ( Frame owner, string message, Color textColor, int numButtons, Action accept, Action reject )
		{
			var frames	=	owner.Frames;
			var panel	=	new Panel( frames, 0, 0, 350,   100 );
			var label	=	new Frame( frames );

			label.X				=	2;
			label.Y				=	14;
			label.Width			=	350-4;
			label.Height		=	100-20-14-4;
			label.Text			=	message;
			label.ForeColor		=	textColor;
			label.BackColor		=	ColorTheme.BackgroundColorDark;
			label.Border		=	1;
			label.BorderColor	=	ColorTheme.BorderColorLight;
			label.Padding		=	4;

			panel.Add( label );

			if (numButtons==2) {
				var acceptBtn		=	new Button(frames, "Accept",     350-160-4, 100-22, 80, 20, 
					() => {
						panel.Close();
						accept?.Invoke();
					}
				);

				var rejectBtn	=	new Button(frames, "Reject",     350-80-2, 100-22, 80, 20, 
					() => {
						panel.Close();
						reject?.Invoke();
					}
				);

				panel.Add( acceptBtn );
				panel.Add( rejectBtn );
			}
			if (numButtons==1) {
				var acceptBtn		=	new Button(frames, "Accept",     350-80-2, 100-22, 80, 20, 
					() => {
						panel.Close();
						accept?.Invoke();
					}
				);
				panel.Add( acceptBtn );
			}

			frames.ShowDialogCentered( panel, owner );
		}


		public static void ShowError ( Frame owner, string message, Action accept )
		{
			ShowDialog( owner, message, ColorTheme.ColorRed, 1, accept, null );
		}


		public static void ShowQuestion ( Frame owner, string message, Action accept, Action reject )
		{
			ShowDialog( owner, message, ColorTheme.TextColorNormal, 2, accept, reject );
		}
	}
}