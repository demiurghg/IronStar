using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Light probe g-buffer render context
	/// </summary>
	internal class LightProbeContext : IRenderContext {
		
		readonly DepthStencilSurface depthBuffer;
		readonly RenderTargetSurface colorBuffer;
		readonly RenderTargetSurface normalBuffer;
		readonly Camera camera;
		

		public LightProbeContext ( RenderSystem rs, Camera camera, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer, RenderTargetSurface normalBuffer )
		{
			this.camera			=	camera;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
			this.normalBuffer	=	normalBuffer;
		}


		public void SetupRenderTargets ( GraphicsDevice device )
		{
			if (normalBuffer!=null) {
				device.SetTargets( depthBuffer, colorBuffer, normalBuffer );
			} else {
				device.SetTargets( depthBuffer, colorBuffer );
			}
		}


		public Camera GetCamera()
		{
			return camera;
		}


		public ShaderResource GetAOBuffer() 
		{
			return null;
		}


		
		public float DepthBias { get { return 0; } }
		public float SlopeBias { get { return 0; } }
		public float FarDistance { get { return 0; } }
		

		public Viewport Viewport { 
			get {
				return new Viewport( 0, 0, depthBuffer.Width, depthBuffer.Height );
			}
		}

	
		public bool RequireShadows {
			get { 
				if (normalBuffer==null) {
					return true;
				} else {
					return false; 
				}
			} 
		}
	
		public bool Transparent {
			get { 
				return false; 
			} 
		}
	}
}
