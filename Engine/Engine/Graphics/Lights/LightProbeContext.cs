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
		
		readonly Matrix viewMatrix;
		readonly Matrix projMatrix;
		readonly DepthStencilSurface depthBuffer;
		readonly RenderTargetSurface colorBuffer;
		readonly RenderTargetSurface normalBuffer;
		

		public LightProbeContext ( LightProbe lightProbe, CubeFace face, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer, RenderTargetSurface normalBuffer )
		{
			var camera = new Camera();
			camera.SetupCameraCubeFace( lightProbe.Position, face, 0.125f, 4096 );
			
			this.viewMatrix		=	camera.GetViewMatrix( StereoEye.Mono );
			this.projMatrix		=	camera.GetProjectionMatrix( StereoEye.Mono );
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
			this.normalBuffer	=	normalBuffer;
		}


		public void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( depthBuffer, colorBuffer, normalBuffer );
		}


		public Matrix GetViewMatrix( StereoEye stereoEye )
		{
			return viewMatrix;
		}


		public Matrix GetProjectionMatrix( StereoEye stereoEye )
		{
			return projMatrix;
		}


		public Vector3 GetViewPosition( StereoEye stereoEye )
		{
			//	for shadows position does not matter
			return Vector3.Zero;
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
				return false; 
			} 
		}
	}
}
