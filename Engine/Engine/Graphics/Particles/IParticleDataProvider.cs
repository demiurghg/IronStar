using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.Particles
{
	public interface IParticleDataProvider
	{
		ConstantBuffer		SimulationParameters { get; }
		ConstantBuffer		ImageAtlasRegions { get; }

		StructuredBuffer	InjectionBuffer { get; }
		StructuredBuffer	SimulationBuffer { get; }
		StructuredBuffer	DeadParticlesIndices { get; }
		StructuredBuffer	LightMapRegions { get; }
	}
}
