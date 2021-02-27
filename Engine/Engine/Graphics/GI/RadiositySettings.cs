using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.GI
{
	public class RadiositySettings
	{
		public const int TileSize = 16;
		public const int ClusterSize = 4;

		[AECategory("Quality")]
		[AEValueRange( 32, 1024, 32, 1 )]
		public int NumRays { get; set; } = 128;

		[AECategory("Quality")]
		[AEValueRange( 1, 3, 1, 1 )]
		public int NumBounces { get; set; } = 1;

		[AECategory("Quality")]
		public bool UseFilter { get; set; } = true;

		[AECategory("Debug")]
		public bool WhiteDiffuse { get; set; } = true;
	}
}
