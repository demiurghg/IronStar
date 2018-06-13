using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.Editor2.Controls {
	public static class MessageBox {

		public static void ShowError ( Frame owner, string message, Action accept )
		{
			var frames	=	owner.Frames;
			var panel	=	new Panel( frames, 0, 0, 350,   100 );
			var label	=	new Frame( frames );

			panel.Tag		=	frames.ModalFrame;
			panel.Closed	+=  (s,e) => frames.ModalFrame = panel.Tag as Frame;

			label.X				=	2;
			label.Y				=	14;
			label.Width			=	350-4;
			label.Height		=	100-20-14-4;
			label.Text			=	message;
			label.ForeColor		=	ColorTheme.ColorRed;
			label.BackColor		=	ColorTheme.BackgroundColorDark;
			label.Border		=	1;
			label.BorderColor	=	ColorTheme.BorderColorLight;
			label.Padding		=	4;

			var button	=	new Button(frames, "OK",     350-80-2, 100-22, 80, 20, 
				() => {
					accept?.Invoke();
					panel.Close();
				}
			);

			panel.Add( label );
			panel.Add( button );

			owner.Add( panel );
			panel.CenterFrame();
			frames.ModalFrame = panel;
		}


		public static void ShowQuestion ( FrameProcessor frames, string message, Action accept, Action reject )
		{
			var panel	=	new Panel( frames, 0, 0, 350,   100 );
			var label	=	new Frame( frames );

			panel.Tag		=	frames.ModalFrame;

			panel.Closed	+=  (s,e) => frames.ModalFrame = panel.Tag as Frame;

			label.X				=	2;
			label.Y				=	14;
			label.Width			=	350-4;
			label.Height		=	100-20-14-4;
			label.Text			=	message;
			label.ForeColor		=	ColorTheme.TextColorNormal;
			label.BackColor		=	ColorTheme.BackgroundColorDark;
			label.Border		=	1;
			label.BorderColor	=	ColorTheme.BorderColorLight;
			label.Padding		=	4;

			var acceptBtn		=	new Button(frames, "Accept",     350-160-4, 100-22, 80, 20, 
				() => {
					accept?.Invoke();
					panel.Close();
				}
			);

			var rejectBtn	=	new Button(frames, "Reject",     350-80-2, 100-22, 80, 20, 
				() => {
					reject?.Invoke();
					panel.Close();
				}
			);

			panel.Add( label );
			panel.Add( acceptBtn );
			panel.Add( rejectBtn );

			frames.RootFrame.Add( panel );
			panel.CenterFrame();
			frames.ModalFrame = panel;
		}


		//public static void ShowYesNoQuestion ( Frame frame, string message, Action acceptAction 

	}
}
