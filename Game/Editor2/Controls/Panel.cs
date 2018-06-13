using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;

namespace IronStar.Editor2.Controls {
	
	public class Panel : Frame {

		public bool AllowDrag {
			get; set;
		}

		public Panel ( FrameProcessor fp, int x, int y, int w, int h ) : base( fp )
		{	
			this.BackColor		=	ColorTheme.BackgroundColor;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.Border			=	1;
			this.Padding		=	1;

			this.X				=	x;
			this.Y				=	y;
			this.Width			=	w;
			this.Height			=	h;

			this.MouseDown  +=Panel_MouseDown;
			this.MouseMove	+=Panel_MouseMove;
			this.MouseUp	+=Panel_MouseUp;
		}



		bool dragging = false;
		int dragX;
		int dragY;
		int posX;
		int posY;


		private void Panel_MouseDown( object sender, MouseEventArgs e )
		{
			if (AllowDrag) {
				dragging	=	true;
				dragX		=	Frames.MousePosition.X;
				dragY		=	Frames.MousePosition.Y;
				posX		=	X;
				posY		=	Y;
			}
		}

		private void Panel_MouseMove( object sender, MouseEventArgs e )
		{
			if (dragging) {
				X	=	posX + (Frames.MousePosition.X - dragX);
				Y	=	posY + (Frames.MousePosition.Y - dragY);
			}
		}

		private void Panel_MouseUp( object sender, MouseEventArgs e )
		{
			if (dragging) {
				dragging = false;
			}
		}
	}
}
