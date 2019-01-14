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
	public class LightProbe {

		/// <summary>
		/// Environment light position
		/// </summary>
		public Matrix	ProbeMatrix { get; set; }

		/// <summary>
		/// Size of light probe
		/// </summary>
		public float	Hardness { get; set; }

		/// <summary>
		/// Unique image index in range [0..255]
		/// </summary>
		public int		ImageIndex = -1;


		internal bool	Visible = true;
		internal Int3	MinExtent;
		internal Int3	MaxExtent;


		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		public LightProbe ()
		{
		}



		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		/// <param name="position"></param>
		/// <param name="innerRadius"></param>
		/// <param name="outerRadius"></param>
		public LightProbe ( int imageIndex )
		{
			ImageIndex	=	imageIndex;
		}
		
	}
}
