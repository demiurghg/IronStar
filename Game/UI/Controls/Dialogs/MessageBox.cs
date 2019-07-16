using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.UI.Controls.Dialogs {
	public class MessageBox : Panel{

		public MessageBox ( FrameProcessor frames, string headerText, string message, Color textColor, int numButtons, Action accept, Action reject, string acceptText="Accept", string rejectText="Reject" )
		 : base(frames, 0, 0, 400, 240) 
		{
			AllowDrag			=	true;

			var layout		=	new PageLayout();
			layout.AddRow( MenuTheme.ElementHeight, new float[] { -1 } );
			layout.AddRow(						-1, new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight, new float[] { 0.5f, 0.5f } );
			layout.Margin	=	4;

			Layout	=	layout;

			//	Header :

			var header	=	new Frame( frames );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	headerText;
			header.ForeColor	=	textColor;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	0;
			header.BorderBottom	=	1;

			Add( header );

			//	Message text :

			var label	=	new Frame( frames );

			label.Font			=	MenuTheme.NormalFont;
			label.Text			=	message;
			label.ForeColor		=	textColor;
			label.BackColor		=	MenuTheme.Transparent;
			label.Padding		=	0;
			label.MarginTop		=	10;
			//label.TextAlignment	=	Alignment.MiddleLeft;

			Add( label );

			//	Buttons :

			if (numButtons==2) {
				var acceptBtn		=	new Button(frames, acceptText, 0,0,0,0, 
					() => {
						Close();
						accept?.Invoke();
					}
				);

				var rejectBtn	=	new Button(frames, rejectText, 0,0,0,0, 
					() => {
						Close();
						reject?.Invoke();
					}
				);

				Add( acceptBtn );
				Add( rejectBtn );
			}
			if (numButtons==1) {
				var acceptBtn		=	new Button(frames, acceptText, 0,0,0,0, 
					() => {
						Close();
						accept?.Invoke();
					}
				);
				Add( acceptBtn );
			}
		}


		public static void ShowError ( Frame owner, string header, string message, Action accept )
		{
			owner.Frames.ShowDialogCentered( new MessageBox( owner.Frames, header, message, MenuTheme.ColorNegative, 1, accept, null ) );
			//ShowDialog( owner, header, message, MenuTheme.ColorNegative, 1, accept, null );
		}


		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject )
		{
			owner.Frames.ShowDialogCentered( new MessageBox( owner.Frames, header, message, MenuTheme.TextColorNormal, 2, accept, reject ) );
		}


		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject, string acceptText, string rejectText )
		{
			owner.Frames.ShowDialogCentered( new MessageBox( owner.Frames, header, message, MenuTheme.TextColorNormal, 2, accept, reject, acceptText, rejectText ) );
		}
	}
}
