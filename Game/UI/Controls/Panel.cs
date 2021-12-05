using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;

namespace IronStar.UI.Controls {
	
	public class Panel : Frame {

		public bool AllowDrag {
			get; set;
		}


		public bool AllowResize {
			get; set;
		}



		public Panel ( UIState ui, int x, int y, int w, int h ) : base( ui )
		{	
			this.BackColor		=	MenuTheme.BackColor;
			this.BorderTop		=	MenuTheme.CaptionHeight;
			this.Padding		=	MenuTheme.PanelContentPadding;

			this.X				=	x;
			this.Y				=	y;
			this.Width			=	w;
			this.Height			=	h;

			this.MouseDown  +=Panel_MouseDown;
			this.MouseMove	+=Panel_MouseMove;
			this.MouseUp	+=Panel_MouseUp;
		}


		bool resizeRight;
		bool resizeLeft;
		bool resizeTop;
		bool resizeBottom;

		bool dragging = false;
		int dragX;
		int dragY;
		int posX;
		int posY;
		int width;
		int height;

		const int borderArea = 1;


		int Distance ( int a, int b ) 
		{
			return Math.Abs( a - b );
		}


		private void Panel_MouseDown( object sender, MouseEventArgs e )
		{
			if (AllowDrag || AllowResize) {

				dragX		=	ui.MousePosition.X;
				dragY		=	ui.MousePosition.Y;
				posX		=	X;
				posY		=	Y;
				width		=	Width;
				height		=	Height;

				var gr = GlobalRectangle;

				if (AllowDrag) {
					dragging	=	true;
				} 
				
				if (AllowResize) {
					if ( Distance( gr.Bottom,	dragY ) <= borderArea ) { resizeBottom	= true;	dragging = false; }
					if ( Distance( gr.Top,		dragY ) <= borderArea ) { resizeTop		= true; dragging = false; }
					if ( Distance( gr.Right,	dragX ) <= borderArea ) { resizeRight	= true; dragging = false; }
					if ( Distance( gr.Left,		dragX ) <= borderArea ) { resizeLeft	= true; dragging = false; }
				}
			}
		}





		private void Panel_MouseMove( object sender, MouseEventArgs e )
		{
			var mouseX = ui.MousePosition.X;
			var mouseY = ui.MousePosition.Y;

			if (dragging) {
				X	=	posX + (mouseX - dragX);
				Y	=	posY + (mouseY - dragY);
			}

			if (resizeRight) {
				Width		=	width + (mouseX - dragX);
				MakeLayoutDirty();
			}

			if (resizeBottom) {
				Height		=	height + (mouseY - dragY);
				MakeLayoutDirty();
			}

			if (resizeTop) {
				Height		=	height - (mouseY - dragY);
				Y			=	posY + (mouseY - dragY);
				MakeLayoutDirty();
			}

			if (resizeLeft) {
				Width		=	width - (mouseX - dragX);
				X			=	posX + (mouseX - dragX);
				MakeLayoutDirty();
			}
		}

		private void Panel_MouseUp( object sender, MouseEventArgs e )
		{
			if (dragging) {
				dragging = false;
			}
			dragging		= false;
			resizeBottom	= false;
			resizeTop		= false;
			resizeRight		= false;
			resizeLeft		= false;
		}


		public Frame CreateHeader ( string text )
		{
			Frame header = new Frame( ui );

			header.Font			=	MenuTheme.HeaderFont;
			header.Text			=	text;
			header.ForeColor	=	MenuTheme.TextColorNormal;
			header.BackColor	=	MenuTheme.Transparent;
			header.Padding		=	4;
			header.Ghost		=	true;

			return header;
		}
	}
}
