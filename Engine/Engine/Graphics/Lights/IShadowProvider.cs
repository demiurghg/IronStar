using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Lights
{
	public interface IShadowProvider
	{
		/// <summary>
		/// Indicates that given light and its shadow 
		/// is visible and should be rendered
		/// </summary>
		bool IsVisible { get; }

		/// <summary>
		/// Gets shadow LOD 
		/// </summary>
		int ShadowLod { get; }

		/// <summary>
		/// Desired shadow view matrix
		/// </summary>
		Matrix ViewMatrix { get; }

		/// <summary>
		/// Desired shadow projection matrix
		/// </summary>
		Matrix ProjectionMatrix { get; }

		/// <summary>
		/// Actual view projection matrix used for shadow map rendering
		/// </summary>
		Matrix ShadowViewProjection { get; set; }

		/// <summary>
		/// Gets shadow mask name.
		/// If light do not use mask, return null.
		/// </summary>
		string ShadowMaskName { get; }

		/// <summary>
		/// Gets and sets shadow dirty flag.
		/// Indicates, that list of shadow casters, 
		/// projection or view settings were changed.
		/// </summary>
		bool IsShadowDirty { get; set; }

		/// <summary>
		/// Gets render list of shadow casters.
		/// </summary>
		RenderList ShadowCasters { get; }

		/// <summary>
		/// Gets shadow region
		/// </summary>
		Rectangle ShadowRegion { get; } 

		/// <summary>
		/// Gets shadow region scale translate vector
		/// </summary>
		Vector4 RegionScaleTranslate { get; }

		/// <summary>
		/// Sets shadow region
		/// </summary>
		/// <param name="region"></param>
		/// <param name="shadowMapSize"></param>
		void SetShadowRegion( Rectangle region, int shadowMapSize );
	}
}
