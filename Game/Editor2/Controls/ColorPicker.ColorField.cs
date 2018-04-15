using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Engine.Input;

namespace IronStar.Editor2.Controls {
	partial class ColorPicker : Frame {

		class ColorField : Frame {

			public ColorField ( FrameProcessor fp, int x, int y, int w, int h, Action<Color> pickColor ) : base(fp,x,y,w,h,"",Color.Gray)
			{
				Border		=	1;
				BorderColor	=	Color.Black;
				BackColor	=	Color.Gray;
			}

		}
		
	}
}
