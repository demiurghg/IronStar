using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.GI2
{
	internal interface ILightProbeProvider
	{
		ShaderResource	GetLightProbeCubeArray();
		int				GetLightProbeIndex( Guid guid );
	}
}
