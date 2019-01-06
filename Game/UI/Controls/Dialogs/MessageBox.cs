using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.UI.Controls.Dialogs {
	public static class MessageBox {

		static void ShowDialog ( Frame owner, string headerText, string message, Color textColor, int numButtons, Action accept, Action reject, string acceptText="Accept", string rejectText="Reject" )
		{
			var frames	=	owner.Frames;
			var panel	=	new Panel( frames, 0, 0, 400, 240 );

			panel.Tag		=	frames.ModalFrame;
			panel.AllowDrag	=	true;

			var layout		=	new PageLayout();
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { 0.5f, 0.5f } );
			layout.Margin	=	4;

			panel.Layout	=	layout;

			//	Header :

			var header	=	new Frame( frames );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	headerText;
			header.ForeColor	=	textColor;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	0;
			header.BorderBottom	=	1;

			panel.Add( header );

			//	Message text :

			var label	=	new Frame( frames );

			label.Font			=	MenuTheme.NormalFont;
			label.Text			=	message;
			label.ForeColor		=	textColor;
			label.BackColor		=	MenuTheme.Transparent;
			label.Padding		=	0;
			label.MarginTop		=	10;
			//label.TextAlignment	=	Alignment.MiddleLeft;

			panel.Add( label );

			//	Buttons :

			if (numButtons==2) {
				var acceptBtn		=	new Button(frames, acceptText, 0,0,0,0, 
					() => {
						accept?.Invoke();
						panel.Close();
					}
				);

				var rejectBtn	=	new Button(frames, rejectText, 0,0,0,0, 
					() => {
						reject?.Invoke();
						panel.Close();
					}
				);

				panel.Add( acceptBtn );
				panel.Add( rejectBtn );
			}
			if (numButtons==1) {
				var acceptBtn		=	new Button(frames, acceptText, 0,0,0,0, 
					() => {
						accept?.Invoke();
						panel.Close();
					}
				);
				panel.Add( acceptBtn );
			}

			frames.PushModalFrame( panel, owner );
			panel.CenterFrame();
		}


		public static void ShowError ( Frame owner, string header, string message, Action accept )
		{
			ShowDialog( owner, header, message, MenuTheme.ColorNegative, 1, accept, null );
		}


		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject )
		{
			ShowDialog( owner, header, message, MenuTheme.TextColorNormal, 2, accept, reject );
		}

		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject, string acceptText, string rejectText )
		{
			ShowDialog( owner, header, message, MenuTheme.TextColorNormal, 2, accept, reject, acceptText, rejectText );
		}
	}
}
