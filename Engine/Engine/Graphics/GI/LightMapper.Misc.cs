using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Scenes;
using System.IO;

namespace Fusion.Engine.Graphics.Lights {

	partial class LightMapper 
	{

		/// <summary>
		/// 
		/// </summary>
		/// <param name="instances"></param>
		/// <param name="ray"></param>
		/// <returns></returns>
		[Obsolete]
		Color4 GetAlbedo ( MeshInstance[] instances, ref RtcRay ray )
		{
			var geomId	=	ray.GeometryId;
			var primId	=	ray.PrimitiveId;

			if (geomId==RtcRay.InvalidGeometryID) {
				return Color4.Zero;
			}

			var instance = instances[geomId];

			foreach ( var subset in instance.Subsets ) 
			{
				if (primId >= subset.StartPrimitive && primId < subset.StartPrimitive + subset.PrimitiveCount) 
				{
					var segment = rs.RenderWorld.VirtualTexture.GetTextureSegmentInfo( subset.Name );
					return segment.AverageColor.ToColor4();
				}
			}

			return new Color4(1,0,1,1);
		}

	}
}
