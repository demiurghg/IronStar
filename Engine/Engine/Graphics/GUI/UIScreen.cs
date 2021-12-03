using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;

namespace Fusion.Engine.Graphics.GUI
{
	public class UIScreen
	{
		public Frame	Root;
		public Matrix	Transform;

		public float	Vignette;
		public float	Saturation;
		public float	Interference;
		public float	Noise;
		public float	Glitch;

		public float	Abberation;
		public float	Scanline;
	}
}
