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
using Fusion.Widgets.Advanced;

namespace Fusion.Engine.Graphics.GI
{
	public partial class Radiosity
	{
		[Config]
		[AECategory("Radiosity")]
		public bool SkipFiltering { get; set; } = false;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,10,0.5f,0.01f)]
		public float MasterIntensity { get; set; } = 1;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,5,1f,0.01f)]
		public float SkyFactor { get; set; } = 1;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,5,1f,0.01f)]
		public float IndirectFactor { get; set; } = 1;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,2,0.1f,0.01f)]
		public float SecondBounce { get; set; } = 0.5f;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,1,0.25f,0.01f)]
		public float ColorBounce { get; set; } = 1.0f;

		[Config]
		[AECategory("Radiosity")]
		[AESlider(0,10,1f,0.01f)]
		public float ShadowFilterRadius { get; set; } = 5f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Weight SH L0")]
		[AESlider(0, 1, 0.1f, 0.01f)]
		public float WeightIntensitySHL0 { get; set; } = 1f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Weight SH L1")]
		[AESlider(0, 1, 0.1f, 0.01f)]
		public float WeightDirectionSHL1 { get; set; } = 0f;
		//[Config]
		//[AECategory("Bilateral Filter")]
		//[AESlider(0,10,1f,0.01f)]
		//public float AlphaFactor { get; set; } = 10f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Falloff SH L0")]
		[AESlider(0, 1, 0.1f, 0.01f)]
		public float FalloffIntensitySHL0 { get; set; } = 0.5f;

		[Config]
		[AECategory("Bilateral Filter")]
		[AEDisplayName("Falloff SH L1")]
		[AESlider(0, 1, 0.1f, 0.01f)]
		public float FalloffDirectionSHL1 { get; set; } = 0.25f;

		[Config]
		[AECategory("Performance")]
		[AEDisplayName("Lock Region")]
		public bool LockRegion { get; set; } = false;

		[Config]
		[AECategory("Performance")]
		[AEDisplayName("Max RPF")]
		[AESlider(0,8,1,1)]
		public int MaxRPF { get; set; } = 1;

	}
}
