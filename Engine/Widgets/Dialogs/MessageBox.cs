using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace Fusion.Widgets 
{
	public static class MessageBox 
	{
		static void ShowDialog ( UIState ui, string caption, string message, Color textColor, int numButtons, Action accept, Action reject )
		{
			var panel	=	new Panel( ui, 0, 0, 350, 140 );
			var label	=	new Frame( ui );

			panel.Layout	=	new PageLayout()
				.Margin(2)
				.AddRow(20, -1f)
				.AddRow(-1, -1f)
				.AddRow(25, -1f, 80,80)
				.AddRow(12, -1f);

			var captionLabel			=	new Label(ui, 0,0,0,0, caption);
			captionLabel.Padding		=	2;
			captionLabel.TextAlignment	=	Alignment.MiddleCenter;

			label.Text			=	message;
			label.ForeColor		=	textColor;
			label.BackColor		=	ColorTheme.BackgroundColorDark;
			label.Border		=	1;
			label.BorderColor	=	ColorTheme.BorderColorLight;
			label.Padding		=	4;
			label.Font			=	ColorTheme.NormalFont;

			panel.Add( captionLabel );
			panel.Add( label );
			panel.AllowDrag = true;

			if (numButtons==2) {
				var acceptBtn		=	new Button(ui, "Yes", 0,0,0,0, 
					() => {
						panel.Close();
						accept?.Invoke();
					}
				);

				var rejectBtn	=	new Button(ui, "No", 0,0,0,0, 
					() => {
						panel.Close();
						reject?.Invoke();
					}
				);

				panel.Add( Frame.CreateEmptyFrame(ui) );
				panel.Add( acceptBtn );
				panel.Add( rejectBtn );
			}
			if (numButtons==1) {
				var acceptBtn		=	new Button(ui, "OK", 0,0,0,0, 
					() => {
						panel.Close();
						accept?.Invoke();
					}
				);
				panel.Add( Frame.CreateEmptyFrame(ui) );
				panel.Add( Frame.CreateEmptyFrame(ui) );
				panel.Add( acceptBtn );
			}

			ui.ShowDialogCentered( panel, null );
		}


		public static void ShowError ( UIState ui, string message, Action accept )
		{
			ShowDialog( ui, "ERROR", message, ColorTheme.ColorRed, 1, accept, null );
		}


		public static void ShowQuestion ( UIState ui, string message, Action accept, Action reject )
		{
			ShowDialog( ui, "QUESTION", message, ColorTheme.TextColorNormal, 2, accept, reject );
		}
	}
}
