using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.GI2
{
	internal interface ILightmapProvider
	{
		Size2		LightmapSize { get; }
		BoundingBox VolumeBounds { get; }

		ShaderResource	LightmapL0 { get; }
		ShaderResource	LightmapL1 { get; }
		ShaderResource	LightmapL2 { get; }
		ShaderResource	LightmapL3 { get; }

		ShaderResource	VolumeL0 { get; }
		ShaderResource	VolumeL1 { get; }
		ShaderResource	VolumeL2 { get; }
		ShaderResource	VolumeL3 { get; }
	}
}
