using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Drivers.Graphics;

namespace Fusion.Core {

	public interface IRenderSystem {

		/// <summary>
		/// Called when the game determines it is time to draw a frame.
		/// In stereo mode this method will be called twice to render left and right parts of stereo image.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		void RenderView ( GameTime gameTime, StereoEye stereoEye );	

		/// <summary>
		/// Called when graphics system needs graphics parameters from render system.
		/// </summary>
		/// <param name="parameters"></param>
		void ApplyParameters ( ref GraphicsParameters parameters );
		#warning RenderSystem must implement IRenderSystem	

		/// <summary>
		/// Gets VSync interval
		/// </summary>
		int VSyncInterval { get; }
	}
}
