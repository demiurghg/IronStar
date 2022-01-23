using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;
using Fusion.Core.Mathematics;
using Fusion.Widgets.Advanced;

namespace Fusion.Engine.Graphics.GI
{
	public class RadiositySettings
	{
		public const int TileSize = 16;
		public const int ClusterSize = 4;

		[AECategory("Quality")]
		[AESlider( 32, 1024, 32, 1 )]
		public int NumRays { get; set; } = 128;

		[AECategory("Quality")]
		[AESlider( 1, 3, 1, 1 )]
		public int NumBounces { get; set; } = 1;

		[AECategory("Quality")]
		[AESlider( 0.5f, 6f, 1, 0.1f )]
		public int IndirectFactor { get; set; } = 1;

		[AECategory("Quality")]
		[AESlider( -1, 1, 1, 1 )]
		public int Bias { get; set; } = 0;

		[AECategory("Filtering")]
		public bool UseBilateral { get; set; } = true;

		[AECategory("Filtering")]
		public bool UseDilate { get; set; } = true;

		[AECategory("Filtering")]
		public float FilterFalloff { get; set; } = 0.5f;

		[AECategory("Filtering")]
		public float FilterWeight { get; set; } = 0.5f;

		[AECategory("Filtering")]
		[AESlider( 0, 100, 10, 1 )]
		public float AlphaWeight { get; set; } = 20.0f;

		[AECategory("Debug")]
		public bool WhiteDiffuse { get; set; } = true;

		[AECategory("Debug")]
		public bool DilateFirst { get; set; } = false;
	}
}
