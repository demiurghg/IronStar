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
	internal class ShadowContextTransparent : ShadowContext {
		
		readonly Rectangle region;
		readonly float farDistance;
		readonly float depthBias;
		readonly float slopeBias;
		readonly Matrix viewMatrix;
		readonly Matrix projMatrix;
		readonly DepthStencilSurface depthBuffer;
		readonly RenderTargetSurface colorBuffer;
		

		public ShadowContextTransparent ( ShadowMap.Cascade cascade, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		:base( cascade, depthBuffer, colorBuffer )
		{
		}


		public ShadowContextTransparent ( SpotLight spot, DepthStencilSurface depthBuffer, RenderTargetSurface colorBuffer )
		:base( spot, depthBuffer, colorBuffer )
		{
		}


		public override bool Transparent {
			get {
				return true;
			}
		}
	}
}
