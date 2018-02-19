using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	class ForwardTransparentContext : ForwardContext {

		public ForwardTransparentContext ( Camera camera, HdrFrame hdrFrame ) : base(camera, hdrFrame)
		{
		}


		public override void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( 
				hdrFrame.DepthBuffer.Surface, 			
				hdrFrame.HdrBufferGlass.Surface, 
				hdrFrame.FeedbackBuffer.Surface,
				hdrFrame.DistortionGlass.Surface
			);
		}
	
		public override bool RequireShadows {
			get {
				return true;
			}
		}
	}
}
