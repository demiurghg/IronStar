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
	public class EnvLight {

		internal int radianceCacheIndex;

		/// <summary>
		/// Environment light position
		/// </summary>
		public Vector3	Position { get; set; }

		/// <summary>
		/// Size of light probe
		/// </summary>
		public float	InnerRadius { get; set; }

		/// <summary>
		/// Size of light probe
		/// </summary>
		public float	OuterRadius { get; set; }


		internal bool		Visible = true;
		internal Int3		MinExtent;
		internal Int3		MaxExtent;


		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		public EnvLight ()
		{
			Position	=	Vector3.Zero;
			InnerRadius	=	0;
			OuterRadius	=	1;
		}



		/// <summary>
		/// Creates instance of EnvLight
		/// </summary>
		/// <param name="position"></param>
		/// <param name="innerRadius"></param>
		/// <param name="outerRadius"></param>
		public EnvLight ( Vector3 position, float innerRadius, float outerRadius )
		{
			this.Position		=	position;
			this.InnerRadius	=	innerRadius;
			this.OuterRadius	=	outerRadius;
		}
		
	}
}
