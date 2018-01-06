using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	interface IRenderContext {

		void		SetupRenderTargets ( GraphicsDevice device );
		
		Matrix		GetViewMatrix( StereoEye stereoEye );
		Matrix		GetProjectionMatrix( StereoEye stereoEye );
		Vector3		GetViewPosition( StereoEye stereoEye );

		float		DepthBias { get; }
		float		SlopeBias { get; }
		float		FarDistance { get; }
		Viewport	Viewport { get; }
		bool		RequireShadows { get; }
	}
}
