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
	/// Shadow render context
	/// </summary>
	internal class ShadowContext : IRenderContext {
		
		readonly Rectangle region;
		readonly float farDistance;
		readonly float depthBias;
		readonly float slopeBias;
		readonly Matrix viewMatrix;
		readonly Matrix projMatrix;
		readonly DepthStencilSurface depthBuffer;
		readonly RenderTargetSurface colorBuffer;
		

		public ShadowContext ( ShadowMap.Cascade cascade, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			this.viewMatrix		=	cascade.ViewMatrix;
			this.projMatrix		=	cascade.ProjectionMatrix;
			this.farDistance	=	1;
			this.region			=	cascade.ShadowRegion;
			this.depthBias		=	cascade.DepthBias;
			this.slopeBias		=	cascade.SlopeBias;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
		}


		public ShadowContext ( SpotLight spot, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			this.viewMatrix		=	spot.SpotView;
			this.projMatrix		=	spot.Projection;
			this.farDistance	=	spot.Projection.GetFarPlaneDistance();
			this.region			=	spot.ShadowRegion;
			this.depthBias		=	spot.DepthBias;
			this.slopeBias		=	spot.SlopeBias;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
		}


		public ShadowContext ( Matrix view, Matrix proj, float depthBias, float slopeBias, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			this.viewMatrix		=	view;
			this.projMatrix		=	proj;
			this.farDistance	=	1;
			this.region			=	new Rectangle( 0,0, depthBuffer.Width, depthBuffer.Height );
			this.depthBias		=	depthBias;
			this.slopeBias		=	slopeBias;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
		}


		public void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( depthBuffer, colorBuffer );
		}


		public ShaderResource GetAOBuffer ()
		{
			return null;
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

		
		public float DepthBias { get { return depthBias; } }
		public float SlopeBias { get { return slopeBias; } }
		public float FarDistance { get { return farDistance; } }
		

		public Viewport Viewport { 
			get {
				return new Viewport( region );
			}
		}

	
		public bool RequireShadows {
			get { 
				return false; 
			} 
		}

		public virtual bool Transparent {
			get {
				return false;
			}
		}
	}
}
