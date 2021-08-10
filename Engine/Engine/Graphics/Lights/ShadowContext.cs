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
using Fusion.Engine.Graphics.Lights;

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

		const float depthBiasScale = 1.0f / 65536.0f;
		

		public ShadowContext ( RenderSystem rs, Camera camera, IShadowProvider shadowProvider, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		{
			var projection		=	shadowProvider.ProjectionMatrix;

			this.camera			=	camera;
			this.farDistance	=	projection.IsOrthographic ? 1 : projection.GetFarPlaneDistance();
			this.region			=	shadowProvider.ShadowRegion;
			this.depthBuffer	=	depthBuffer;
			this.colorBuffer	=	colorBuffer;
			this.depthBias		=	rs.ShadowSystem.SpotDepthBias * depthBiasScale;
			this.slopeBias		=	rs.ShadowSystem.SpotSlopeBias;
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
