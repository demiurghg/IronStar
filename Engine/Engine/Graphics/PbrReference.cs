using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics
{
	public static class PbrReference
	{
		public static Color DielectricMax { get { return new Color( 240, 240, 240, 255 ); } }
		public static Color DielectricMin { get { return new Color(  30,  30,  30,  30 ); } }

		public static Color MetalMax { get { return new Color( 255, 255, 255, 255 ); } }
		public static Color MetalMin { get { return new Color( 180, 180, 180, 180 ); } }
	}
}
