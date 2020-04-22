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
using System.IO;
using Fusion.Core.Content;

namespace Fusion.Engine.Graphics.Lights 
{
	/// <summary>
	/// 
	/// </summary>
	class LightMapGroup 
	{
		public LightMapGroup ( int size, Guid guid, IEnumerable<MeshInstance> instances, int bias )
		{
			Guid		=	guid;

			if (bias>0)
			{
				size *= 2;
			}
			if (bias<0)
			{
				size /= 2;
			}

			Region		=	new Rectangle(0,0,size,size);
			Instances	=	instances.ToArray();
		}
		public Rectangle Region;
		public readonly Guid Guid;
		public readonly MeshInstance[] Instances;
	}
}
