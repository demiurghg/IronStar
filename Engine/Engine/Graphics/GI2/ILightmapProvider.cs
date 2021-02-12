﻿using System;
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
		Size2			GetLightmapSize();
		BoundingBox		GetVolumeBounds();
		RectangleF		GetRegion( string name );
		ShaderResource	GetLightmap( int band );
		ShaderResource	GetVolume( int band );
	}
}
