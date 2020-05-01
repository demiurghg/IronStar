using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Engine.Common;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {
	internal partial class SsaoFilter {

		
		[AECategory("HDAO")] [Config] public QualityLevel QualityLevel { get; set; }

		[AECategory("HDAO")] [Config] public float	FadeoutDistance { get; set; } = 50;
		[AECategory("HDAO")] [Config] public float	DiscardDistance { get; set; } = 100;
		
		[AECategory("HDAO")] [Config] public float	AcceptRadius { get; set; } = 0.01f;
		[AECategory("HDAO")] [Config] public float	RejectRadius { get; set; } = 1.00f;

		[AECategory("Bilateral Filter")] [Config] public bool	SkipBilateralFilter { get; set; } = false;
		[AECategory("Bilateral Filter")] [Config] public float	BilateralDepthFactor { get; set; } = 2;
		[AECategory("Bilateral Filter")] [Config] public float	BilateralColorFactor { get; set; } = 0;
		[AECategory("Bilateral Filter")] [Config] public float	BilateralFalloff { get; set; } = 0.2f;

	}
}
