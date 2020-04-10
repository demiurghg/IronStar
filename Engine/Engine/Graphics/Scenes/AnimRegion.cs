using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Fusion.Engine.Graphics {

	[TypeConverter( typeof( ExpandableObjectConverter ) )]
	public class AnimRegion {

		int startFrame = 0;
		int endFrame = 1;

		public int StartFrame { 
			get { return startFrame; } 
			set { startFrame = value; }
		}
		public int EndFrame {	
			get { return endFrame; }
			set { endFrame = value; }
		}
		public int Length {
			get { return endFrame - startFrame; } 
			set { endFrame = startFrame + value; }
		}

		public float FramesPerSecond { get; set; } = 60.0f;


		public float GetFrame ( float fraction )
		{
			return startFrame + fraction * (endFrame - startFrame);
		}


		public float GetFrame ( int msec )
		{
			if (StartFrame==EndFrame) {
				return StartFrame;
			}
			return (msec * FramesPerSecond / 1000.0f) % Length + StartFrame;
		}


		public override string ToString()
		{
			return string.Format("[{0}..{1}]@{2}", startFrame, endFrame, FramesPerSecond );
		}
	}
}
