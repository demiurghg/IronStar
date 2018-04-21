using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace IronStar.Editor2.Controls {
	
	public class Label : Frame {

		public Label ( FrameProcessor fp, int x, int y, int w, int h, string text ) : base( fp )
		{	
			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.Padding		=	0;

			this.ForeColor		=	ColorTheme.TextColorNormal;

			this.X				=	x;
			this.Y				=	y;
			this.Width			=	w;
			this.Height			=	h;

			this.Text			=	text;
		}

	}
}
