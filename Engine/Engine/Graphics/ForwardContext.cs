using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	abstract class ForwardContext : IRenderContext {

		readonly protected Camera	camera;
		readonly protected HdrFrame	hdrFrame;	 


		public ForwardContext ( Camera camera, HdrFrame hdrFrame )
		{
			this.camera		=	camera;
			this.hdrFrame	=	hdrFrame;
		}


		public abstract void SetupRenderTargets ( GraphicsDevice device );


		public virtual ShaderResource GetAOBuffer() 
		{
			return hdrFrame.AOBuffer;
		}


		public Camera GetCamera()
		{
			return camera;
		}


		public float DepthBias { get { return 0; } }
		public float SlopeBias { get { return 0; } }
		public float FarDistance { get { return 0; } }
		

		public Viewport Viewport { 
			get {
				return new Viewport( 0,0, hdrFrame.HdrBuffer.Width, hdrFrame.HdrBuffer.Height );
			}
		}

	
		public abstract bool RequireShadows {
			get;
		}
	
		public abstract bool Transparent {
			get;
		}
	}
}
