using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	class ForwardContext : IRenderContext {

		readonly Camera		camera;
		readonly HdrFrame	hdrFrame;	 
		readonly bool		zpass;


		public ForwardContext ( Camera camera, HdrFrame hdrFrame, bool zpass )
		{
			this.zpass		=	zpass;
			this.camera		=	camera;
			this.hdrFrame	=	hdrFrame;
		}


		public void SetupRenderTargets ( GraphicsDevice device )
		{
			if (zpass) {
				device.SetTargets( hdrFrame.DepthBuffer.Surface );
			} else {
				device.SetTargets( hdrFrame.DepthBuffer.Surface, hdrFrame.HdrBuffer.Surface, hdrFrame.FeedbackBuffer.Surface );
			}
		}


		public Matrix GetViewMatrix( StereoEye stereoEye )
		{
			return camera.GetViewMatrix( stereoEye );
		}


		public Matrix GetProjectionMatrix( StereoEye stereoEye )
		{
			return camera.GetProjectionMatrix( stereoEye );
		}


		public Vector3 GetViewPosition( StereoEye stereoEye )
		{
			return camera.GetCameraPosition( stereoEye );
		}

		
		public float DepthBias { get { return 0; } }
		public float SlopeBias { get { return 0; } }
		public float FarDistance { get { return 0; } }
		

		public Viewport Viewport { 
			get {
				return new Viewport( 0,0, hdrFrame.HdrBuffer.Width, hdrFrame.HdrBuffer.Height );
			}
		}

	
		public bool RequireShadows {
			get { 
				return true; 
			} 
		}
	}
}
