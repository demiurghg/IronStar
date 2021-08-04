using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;

namespace Fusion.Engine.Graphics 
{
	public class SpotLight 
	{
		internal uint Timer = 0;

		bool shadowDirty = true;

		Matrix	spotView;
		Matrix	spotProjection;
		int		lodBias;
		int		lod;

		/// <summary>
		/// Spot-light view matrix.
		/// </summary>
		public Matrix SpotView
		{
			get { return spotView; }
			set 
			{ 
				if (spotView!=value)
				{
					spotView	= value; 
					shadowDirty	= true; 
				}
			}
		}

		/// <summary>
		/// Spot-light projection matrix.
		/// </summary>
		public Matrix Projection
		{
			get { return spotProjection; }
			set
			{
				if (spotProjection!=value)
				{
					spotProjection	= value;
					shadowDirty		= true;
				}
			}
		}

		/// <summary>
		/// Omni-light position
		/// </summary>
		public Vector3 Position0;
		
		/// <summary>
		/// Omni-light position
		/// </summary>
		public Vector3 Position1;

		/// <summary>
		/// Spot-light intensity.
		/// </summary>
		public Color4 Intensity;

		/// <summary>
		/// Spot-light intensity.
		/// </summary>
		public Color4 Intensity2 { get { return Intensity * LightStyleController.RunLightStyle((int)Timer, LightStyle); } }

		/// <summary>
		/// 
		/// </summary>
		public string SpotMaskName;

		/// <summary>
		/// 
		/// </summary>
		public int LodBias
		{
			get { return lodBias; }
			set 
			{
				if (lodBias!=value)
				{
					lodBias		=	value;
					shadowDirty	=	true;
				}
			}
		}

		/// <summary>
		/// Spot-light inner radius.
		/// </summary>
		public float RadiusInner;

		/// <summary>
		/// Spot-light outer radius.
		/// </summary>
		public float RadiusOuter;

		/// <summary>
		/// 
		/// </summary>
		public LightStyle LightStyle;

		/// <summary>
		/// 
		/// </summary>
		public bool EnableGI;

		public Vector3 CenterPosition { get { return 0.5f * Position0 + 0.5f * Position1; } }


		internal bool		Visible = true;
		internal Rectangle	ShadowRegion;
		internal Vector4	ShadowScaleOffset;
		internal Int3		MinExtent;
		internal Int3		MaxExtent;

		internal int DetailLevel
		{
			get { return lod; }
			set
			{
				if (lod!=value)
				{
					lod = value;
					shadowDirty = true;
				}
			}
		}
	}
}
