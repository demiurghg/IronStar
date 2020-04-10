using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics {
	public interface IRenderContext {

		void			SetupRenderTargets ( GraphicsDevice device );

		ShaderResource	GetAOBuffer();

		Camera			GetCamera();		

		float			DepthBias { get; }
		float			SlopeBias { get; }
		float			FarDistance { get; }
		Viewport		Viewport { get; }
		bool			RequireShadows { get; }
		bool			Transparent { get; }
	}
}
