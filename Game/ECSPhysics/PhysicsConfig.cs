using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;

namespace IronStar.ECSPhysics
{
	[Config("Physics")]
	public static class PhysicsConfig
	{
		public static bool UseParallelPhysics { get; set; }
	}
}
