using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Engine.Frames.Layouts;

namespace IronStar.UI.Controls.Dialogs {
	public class MessageBox : Panel
	{
		public event EventHandler Accept;
		public event EventHandler Reject;


		public MessageBox ( UIState ui, string headerText, string message, Color textColor, int numButtons, string acceptText="Accept", string rejectText="Reject" )
		 : base(ui, 0, 0, 400, 240) 
		{
			AllowDrag			=	true;

			Layout		=	new PageLayout()
						.AddRow( MenuTheme.ElementHeight, new float[] { -1 } )
						.AddRow(						-1, new float[] { -1 } )
						.AddRow( MenuTheme.ElementHeight, new float[] { 0.5f, 0.5f } )
						.Margin( 4 )
						;

			//	Header :

			var header	=	new Frame( ui );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	headerText;
			header.ForeColor	=	textColor;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	0;
			header.BorderBottom	=	1;

			Add( header );

			//	Message text :

			var label	=	new Frame( ui );

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
				var acceptBtn	=	new Button(ui, acceptText, 0,0,0,0, 
					() => Accept?.Invoke(this, EventArgs.Empty)
				);

				var rejectBtn	=	new Button(ui, rejectText, 0,0,0,0, 
					() => Reject?.Invoke(this, EventArgs.Empty)
				);

				Add( acceptBtn );
				Add( rejectBtn );
			}
			if (numButtons==1) {
				var acceptBtn		=	new Button(ui, acceptText, 0,0,0,0, 
					() => Accept?.Invoke(this, EventArgs.Empty)
				);
				Add( acceptBtn );
			}
		}


		public static void ShowError ( Frame owner, string header, string message, Action accept )
		{
			var box		=	new MessageBox( owner.ui, header, message, MenuTheme.ColorNegative, 1 );
			var ctxt	=	owner.ui.ShowDialogCentered( box );
			box.Accept += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); accept?.Invoke(); };
			box.Reject += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); };
		}


		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject )
		{
			var box		=	new MessageBox( owner.ui, header, message, MenuTheme.TextColorNormal, 2 );
			var ctxt	=	owner.ui.ShowDialogCentered( box );
			box.Accept += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); accept?.Invoke(); };
			box.Reject += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); reject?.Invoke(); };
		}


		public static void ShowQuestion ( Frame owner, string header, string message, Action accept, Action reject, string acceptText, string rejectText )
		{
			var box		=	new MessageBox( owner.ui, header, message, MenuTheme.TextColorNormal, 2, acceptText, rejectText );
			var ctxt	=	owner.ui.ShowDialogCentered( box );
			box.Accept += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); accept?.Invoke(); };
			box.Reject += (e,a) => { owner.ui.Stack.PopUIContext(ref ctxt); reject?.Invoke(); };
		}
	}
}
