﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.UI.Controls.Dialogs {
	public class TextBoxDialog : Panel{

		public TextBoxDialog ( FrameProcessor frames, string headerText, string messageText, string defaultText, Func<string,bool> accept )
		 : base(frames, 0, 0, 400, 240) 
		{
			AllowDrag			=	true;

			var layout		=	new PageLayout();
			layout.AddRow( MenuTheme.ElementHeight,		new float[] { -1 } );
			layout.AddRow( -1,							new float[] { -1 } );
			layout.AddRow( -1,							new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight/2,	new float[] { -1 } );
			layout.AddRow( MenuTheme.ElementHeight,		new float[] { 0.5f, 0.5f } );
			layout.Margin	=	4;

			Layout	=	layout;

			//	Header :

			var header	=	new Frame( frames );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	headerText;
			header.ForeColor	=	MenuTheme.TextColorNormal;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	0;
			header.BorderBottom	=	1;

			Add( header );

			//	Message text :

			var label	=	new Frame( frames );

			label.Font			=	MenuTheme.NormalFont;
			label.Text			=	messageText;
			label.ForeColor		=	MenuTheme.TextColorNormal;
			label.BackColor		=	MenuTheme.Transparent;
			label.Padding		=	0;
			label.MarginTop		=	10;
			//label.TextAlignment	=	Alignment.MiddleLeft;

			Add( label );


			//	Text editor :

			var textBox	=	new TextBox( frames );

			textBox.Font			=	MenuTheme.NormalFont;
			textBox.Text			=	defaultText;
			textBox.ForeColor		=	MenuTheme.TextColorNormal;
			textBox.BackColor		=	MenuTheme.ElementColor;
			textBox.Padding			=	4;
			textBox.TextAlignment	=	Alignment.MiddleLeft;
			textBox.Border			=	2;
			textBox.BorderColor		=	MenuTheme.AccentColor;

			Add( textBox );
			Add( Frame.CreateEmptyFrame(Frames) );


			//	Buttons :

			var acceptBtn		=	new Button(frames, "OK", 0,0,0,0, 
				() => {
					if (accept.Invoke(textBox.Text)) {
						Close();
					}
				}
			);

			var rejectBtn	=	new Button(frames, "Cancel", 0,0,0,0, 
				() => {
					Close();
				}
			);

			Add( acceptBtn );
			Add( rejectBtn );
		}
	}
}