using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;

namespace IronStar.Editor2.Controls {
	
	public class Panel : Frame {

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
		}

	}
}
