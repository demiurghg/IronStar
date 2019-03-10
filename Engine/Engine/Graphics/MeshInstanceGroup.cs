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

namespace Fusion.Engine.Graphics {

	/// <summary>
	/// Represnets mesh instance
	/// </summary>
	public class MeshInstanceGroup {

		readonly Guid Guid;
		readonly MeshInstance[] instances;
		readonly public int Count;


		public Rectangle LightMapRegion { get; set; }


		public MeshInstanceGroup ( Guid guid, IEnumerable<MeshInstance> instances )
		{
			this.Guid		=	guid;
			this.instances	=	instances.ToArray();
			this.Count		=	this.instances.Length;
		}


		public MeshInstance this [int index] {
			get {
				return instances[index];
			}
		}
		
	}
}
