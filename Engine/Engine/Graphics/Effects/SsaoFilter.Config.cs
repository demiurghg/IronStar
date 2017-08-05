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


namespace Fusion.Engine.Graphics {
	internal partial class SsaoFilter {

		
		[Category("HDAO")] [Config] public QualityLevel QualityLevel { get; set; }

		[Category("HDAO")] [Config] public float	FadeoutDistance { get; set; } = 50;
		[Category("HDAO")] [Config] public float	DiscardDistance { get; set; } = 100;
		
		[Category("HDAO")] [Config] public float	AcceptRadius { get; set; } = 0.01f;
		[Category("HDAO")] [Config] public float	RejectRadius { get; set; } = 1.00f;

		[Category("Bilateral Filter")] [Config] public bool		SkipBilateralFilter { get; set; } = false;
		[Category("Bilateral Filter")] [Config] public float	BilateralDepthFactor { get; set; } = 100;
		[Category("Bilateral Filter")] [Config] public float	BilateralColorFactor { get; set; } = 8;

	}
}
