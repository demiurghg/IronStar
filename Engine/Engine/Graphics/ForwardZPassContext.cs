using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	class ForwardZPassContext : ForwardContext {

		public ForwardZPassContext ( Camera camera, HdrFrame hdrFrame ) : base(camera, hdrFrame)
		{
		}


		public override void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( hdrFrame.DepthBuffer.Surface );
		}
	
		public ShaderResource GetAOBuffer() 
		{
			return null;
		}


		public override bool RequireShadows {
			get {
				return false;
			}
		}

		public override bool Transparent {
			get {
				return false;
			}
		}
	}
}
