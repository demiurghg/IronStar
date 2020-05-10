using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity
	{

		[Config]
		[AECategory("Debug rays")]
		public int DebugX { get; set; } = 0;

		[Config]
		[AECategory("Debug rays")]
		public int DebugY { get; set; } = 0;

		[Config]
		[AECategory("Radiosity")]
		public bool SkipDilation { get; set; } = false;

		[Config]
		[AECategory("Radiosity")]
		public bool SkipDenoising { get; set; } = false;

		[Config]
		[AECategory("Radiosity")]
		[AEValueRange(0,5,1f,0.01f)]
		public float SkyFactor { get; set; } = 1;

		[Config]
		[AECategory("Radiosity")]
		[AEValueRange(0,5,1f,0.01f)]
		public float IndirectFactor { get; set; } = 1;

		[Config]
		[AECategory("Radiosity")]
		[AEValueRange(0,2,0.1f,0.01f)]
		public float SecondBounce { get; set; } = 0.5f;

		[Config]
		[AECategory("Radiosity")]
		[AEValueRange(0,10,1f,0.01f)]
		public float ShadowFilterRadius { get; set; } = 5f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Weight SH L0")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float WeightIntensitySHL0 { get; set; } = 1f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Weight SH L1")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float WeightDirectionSHL1 { get; set; } = 0f;
		//[Config]
		//[AECategory("Bilateral Filter")]
		//[AEValueRange(0,10,1f,0.01f)]
		//public float AlphaFactor { get; set; } = 10f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Falloff SH L0")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float FalloffIntensitySHL0 { get; set; } = 0.5f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Falloff SH L1")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float FalloffDirectionSHL1 { get; set; } = 0.25f;


	}
}
