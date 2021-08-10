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
		bool regionDirty = true;
		Rectangle region = new Rectangle(0,0,0,0);

		Matrix	spotView;
		Matrix	spotProjection;
		int		lodBias;
		int		lod;

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
		public string ShadowMaskName { get; set; }

		/// <summary>
		/// Decrease size of the shadow map region
		/// </summary>
		public int LodBias
		{
			get { return lodBias; }
			set 
			{
				if (lodBias!=value)
				{
					lodBias			=	value;
					isShadowDirty	=	true;
					regionDirty		=	true;
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


		public bool		IsVisible { get; set; }
		public Vector4	RegionScaleOffset { get; private set; }
		internal Int3		MinExtent;
		internal Int3		MaxExtent;

		readonly RenderList shadowCasters = new RenderList();
		public RenderList ShadowCasters { get { return shadowCasters; } }

		/// <summary>
		/// Gets and sets shadow region
		/// </summary>
		public Rectangle ShadowRegion
		{
			get { return region; }
			set
			{
				regionDirty		=	false;

				if (region!=value)
				{
					region			=	value;
					isShadowDirty	=	true;
				}
			}
		}


		public void SetShadowRegion( Rectangle region, int shadowMapSize )
		{
			regionDirty		=	false;

			if (this.region!=region)
			{
				this.region			=	region;
				isShadowDirty		=	true;
				RegionScaleOffset	=	region.GetMadOpScaleOffsetOffCenterProjectToNDC( shadowMapSize, shadowMapSize );
			}
		}

		public void ResetShadow()
		{
			regionDirty		=	true;
			IsShadowDirty	=	true;
		}


		/// <summary>
		/// Indicates that shadow region need to be updated
		/// </summary>
		public bool IsRegionDirty {	get { return regionDirty; }	}

		
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
					regionDirty		=	true;
				}
			}
		}
	}
}
