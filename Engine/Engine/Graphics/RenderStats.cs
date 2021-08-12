using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics 
{
	public class RenderStats 
	{
		public int	InstanceCount;
		public int	OmniLightCount;
		public int	SpotLightCount;
		public int	DecalCount;

		public int CascadeCount;
		public int ShadowMapCount;

		public int	DeadParticles;

		public void Reset()
		{
			InstanceCount	=	0;
			OmniLightCount	=	0;
			SpotLightCount	=	0;
			DecalCount		=	0;

			CascadeCount	=	0;
			ShadowMapCount	=	0;

			DeadParticles	=	-1;
		}
	}
}
