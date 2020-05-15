using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Native.Embree;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics 
{
	public class RenderList : List<RenderInstance>
	{
		public RenderList()
		{
		}

		public RenderList(int capacity) : base(capacity)
		{		
		}

		public RenderList(IEnumerable<RenderInstance> collection) : base(collection)
		{
		}
	}
}
