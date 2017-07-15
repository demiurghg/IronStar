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

		[Config] public bool Enabled { get; set; }


		[Config] public float PowerIntensity { get; set; } = 2;
		[Config] public float LinearIntensity { get; set; } = 1;
		
		[Config] public float FadeoutDistance { get; set; } = 50;
		[Config] public float DiscardDistance { get; set; } = 100;
		
		[Config] public float AcceptRadius { get; set; } = 0.01f;
		[Config] public float RejectRadius { get; set; } = 1.00f;

	}
}
