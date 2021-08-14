using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Lights;

namespace Fusion.Engine.Graphics 
{
	public class SpotLight : IShadowProvider 
	{
		internal uint Timer = 0;

		bool isShadowDirty = true;
		Rectangle shadowRegion = new Rectangle(0,0,0,0);
		Vector4 regionScaleTranslate;

		Matrix	spotView;
		Matrix	spotProjection;
		int		lod;

		public override string ToString()
		{
			return string.Format("Spot: Mask:{0}, LOD:{1}", ShadowMaskName, ShadowLod);
		}

		/// <summary>
		/// Spot-light view matrix.
		/// </summary>
		public Matrix ViewMatrix
		{
			get { return spotView; }
			set 
			{ 
				if (spotView!=value)
				{
					spotView		= value; 
					isShadowDirty	= true; 
				}
			}
		}

		/// <summary>
		/// Spot-light projection matrix.
		/// </summary>
		public Matrix ProjectionMatrix
		{
			get { return spotProjection; }
			set
			{
				if (spotProjection!=value)
				{
					spotProjection	= value;
					isShadowDirty	= true;
				}
			}
		}

		public Matrix ShadowViewProjection { get; set; }

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
		/// 
		/// </summary>
		public string ShadowMaskName { get; set; }

		/// <summary>
		/// Decrease size of the shadow map region
		/// </summary>
		public int LodBias { get; set; }

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

		public bool		IsVisible { get; set; }
		internal Int3	MinExtent;
		internal Int3	MaxExtent;

		readonly RenderList shadowCasters = new RenderList();
		public RenderList ShadowCasters { get { return shadowCasters; } }

		
		/// <summary>
		/// Indicates that shadow map must be updated
		/// </summary>
		public bool IsShadowDirty 
		{ 
			get { return isShadowDirty; } 
			set { isShadowDirty = value; } 
		}
		
		/// <summary>
		/// Actual level of detail, 
		/// depends on distance and size of the spot light
		/// </summary>
		public int ShadowLod
		{
			get { return lod; }
			internal set
			{
				if (lod!=value)
				{
					lod				=	value;
					isShadowDirty	=	true;
				}
			}
		}

		public Rectangle ShadowRegion 
		{ 
			get { return shadowRegion; }
		}

		public Vector4 RegionScaleTranslate
		{
			get { return regionScaleTranslate; }
		}

		public void SetShadowRegion( Rectangle region, int shadowMapSize )
		{
			shadowRegion			=	region;
			regionScaleTranslate	=	shadowRegion.GetMadOpScaleOffsetOffCenterProjectToNDC( shadowMapSize, shadowMapSize );
		}
	}
}
