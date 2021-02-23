using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;

namespace Fusion.Engine.Graphics.GI2
{
	public interface ILightMapProvider
	{
		Size2			LightMapSize { get; }
		Size3			VolumeSize { get; }
		Rectangle		GetRegion( string name );
		Vector4			GetRegionMadST( string name );
		Matrix			WorldToVolume { get; }
		Matrix			VoxelToWorld { get; }
		ShaderResource	GetLightmap( int band );
		ShaderResource	GetVolume( int band );
	}
}
