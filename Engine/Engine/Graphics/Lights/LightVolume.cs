using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;

namespace Fusion.Engine.Graphics {

	public class LightVolume
	{
		public Matrix WorldMatrix { get; set; }

		public int ResolutionX { get; set; } = 4;
		public int ResolutionY { get; set; } = 4;
		public int ResolutionZ { get; set; } = 4;

		public Size3 VolumeSize { get { return new Size3( ResolutionX, ResolutionY, ResolutionZ ); } }
	}
}
