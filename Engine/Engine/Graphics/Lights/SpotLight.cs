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

	public class SpotLight {
		
		internal uint Timer = 0;

		/// <summary>
		/// Spot-light view matrix.
		/// </summary>
		public Matrix	SpotView;

		/// <summary>
		/// Spot-light projection matrix.
		/// </summary>
		public Matrix	Projection;

		/// <summary>
		/// Spot-light position
		/// </summary>
		public Vector3	Position;

		/// <summary>
		/// Spot-light intensity.
		/// </summary>
		public Color4	Intensity;

		/// <summary>
		/// Spot-light intensity.
		/// </summary>
		public Color4	Intensity2 { get { return Intensity * LightStyleController.RunLightStyle((int)Timer, LightStyle); } }

		/// <summary>
		/// Spot-light mask texture index.
		/// </summary>
		public int		TextureIndex;

		/// <summary>
		/// Spot-light inner radius.
		/// </summary>
		public float	RadiusInner;

		/// <summary>
		/// Spot-light outer radius.
		/// </summary>
		public float	RadiusOuter;

		/// <summary>
		/// 
		/// </summary>
		public float	SlopeBias = 2;

		/// <summary>
		/// 
		/// </summary>
		public float	DepthBias = 1f / 1024f;

		/// <summary>
		/// 
		/// </summary>
		public LightStyle LightStyle;


		internal bool		Visible = true;
		internal int		DetailLevel;   
		internal Rectangle	ShadowRegion;
		internal Vector4	ShadowScaleOffset;
		internal Int3		MinExtent;
		internal Int3		MaxExtent;
	}
}
