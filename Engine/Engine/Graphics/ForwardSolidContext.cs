using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	class ForwardSolidContext : ForwardContext {

		public ForwardSolidContext ( Camera camera, HdrFrame hdrFrame ) : base(camera, hdrFrame)
		{
		}


		public override void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( hdrFrame.DepthBuffer.Surface, hdrFrame.HdrTarget.Surface, hdrFrame.FeedbackBuffer.Surface );
		}
	
		public override bool RequireShadows {
			get {
				return true;
			}
		}
	
		public override bool Transparent {
			get {
				return false;
			}
		}
	}
}
