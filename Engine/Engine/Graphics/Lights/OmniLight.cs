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

	public class OmniLight {

		internal uint Timer = 0;
		
		/// <summary>
		/// Omni-light position
		/// </summary>
		public Vector3	Position0;
		
		/// <summary>
		/// Omni-light position
		/// </summary>
		public Vector3	Position1;

		/// <summary>
		/// Omni-light intensity
		/// </summary>
		public Color4	Intensity;

		/// <summary>
		/// Omni-light inner radius.
		/// </summary>
		public float	RadiusInner;

		/// <summary>
		/// Omni-light outer radius.
		/// </summary>
		public float	RadiusOuter;

		/// <summary>
		/// Omni-light outer radius.
		/// </summary>
		public LightStyle	LightStyle;

		/// <summary>
		/// Ambient-only
		/// </summary>
		public bool Ambient;

		internal bool	Visible;
		internal Int3	MinExtent;
		internal Int3	MaxExtent;


		public Vector3 CenterPosition { get { return 0.5f * Position0 + 0.5f * Position1; } }


		/// <summary>
		/// 
		/// </summary>
		public OmniLight ()
		{
			Position0	=	Vector3.Zero;
			Position1	=	Vector3.Zero;
			Intensity	=	Color4.Zero;
			RadiusInner	=	0;
			RadiusOuter	=	1;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="color"></param>
		/// <param name="radius"></param>
		public OmniLight ( Vector3 position, Color4 color, float radius )
		{
			Position0	=	position;
			Position1	=	position;
			Intensity	=	color;
			RadiusInner	=	0;
			RadiusOuter	=	radius;
		}
	}
}
