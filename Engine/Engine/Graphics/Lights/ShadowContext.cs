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

namespace Fusion.Engine.Graphics 
{
	/// <summary>
	/// Shadow render context
	/// </summary>
	internal class ShadowContext : IRenderContext 
	{
		readonly Rectangle region;
		readonly Camera camera;
		readonly float farDistance;
		readonly float depthBias;
		readonly float slopeBias;
		readonly DepthStencilSurface depthBuffer;
		readonly RenderTargetSurface colorBuffer;
		

		public ShadowContext ( Camera camera, ShadowMap.Cascade cascade, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			this.camera			=	camera;
			this.farDistance	=	1;
			this.region			=	cascade.ShadowRegion;
			this.depthBias		=	cascade.DepthBias;
			this.slopeBias		=	cascade.SlopeBias;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
		}


		public ShadowContext ( Camera camera, SpotLight spot, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			this.camera			=	camera;
			this.farDistance	=	spot.Projection.GetFarPlaneDistance();
			this.region			=	spot.ShadowRegion;
			this.depthBias		=	spot.DepthBias;
			this.slopeBias		=	spot.SlopeBias;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
		}


		public Camera GetCamera()
		{
			return camera;
		}


		public void SetupRenderTargets ( GraphicsDevice device )
		{
			device.SetTargets( depthBuffer, colorBuffer );
		}


		public ShaderResource GetAOBuffer ()
		{
			return null;
		}


		public float DepthBias { get { return depthBias; } }
		public float SlopeBias { get { return slopeBias; } }
		public float FarDistance { get { return farDistance; } }
		

		public Viewport Viewport 
		{ 
			get 
			{
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
