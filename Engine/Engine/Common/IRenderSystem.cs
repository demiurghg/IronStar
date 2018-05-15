using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Common {

	public interface IRenderSystem {

		/// <summary>
		/// Called when the game determines it is time to draw a frame.
		/// In stereo mode this method will be called twice to render left and right parts of stereo image.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		void Render ( GameTime gameTime, StereoEye stereoEye );	
		#error RenderSystem must implement IRenderSystem	
	}
}
