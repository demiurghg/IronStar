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

		bool contentDirty = true;
		bool regionDirty = true;
		Rectangle region = new Rectangle(0,0,0,0);

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
					spotView		= value; 
					contentDirty	= true; 
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
					contentDirty	= true;
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
					contentDirty	=	true;
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


		internal bool		Visible = true;
		internal Vector4	ShadowScaleOffset;
		internal Int3		MinExtent;
		internal Int3		MaxExtent;

		internal RenderList ShadowCasters = new RenderList();

		/// <summary>
		/// Gets and sets shadow region
		/// </summary>
		internal Rectangle	ShadowRegion
		{
			get { return region; }
			set
			{
				region			=	value;
				regionDirty		=	false;
				contentDirty	=	true;
			}
		}

		
		/// <summary>
		/// Indicates that shadow region need to be updated
		/// </summary>
		public bool IsRegionDirty {	get { return regionDirty; }	}

		
		/// <summary>
		/// Indicates that shadow map must be updated
		/// </summary>
		public bool IsContentDirty 
		{ 
			get { return contentDirty; } 
			set { contentDirty = value; } 
		}

		
		/// <summary>
		/// Actual level of detail, 
		/// depends on distance and size of the spot light
		/// </summary>
		internal int DetailLevel
		{
			get { return lod; }
			set
			{
				if (lod!=value)
				{
					lod				=	value;
					contentDirty	=	true;
					regionDirty		=	true;
				}
			}
		}
	}
}
