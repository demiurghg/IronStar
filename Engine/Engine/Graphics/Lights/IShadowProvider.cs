using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Lights
{
	internal interface IShadowProvider
	{
		/// <summary>
		/// Indicates that given light and its shadow 
		/// is visible and should be rendered
		/// </summary>
		bool IsVisible { get; }

		/// <summary>
		/// Gets and sets shadow LOD 
		/// </summary>
		int ShadowLod { get; set; }

		/// <summary>
		/// Shadow view matrix
		/// </summary>
		Matrix ViewMatrix { get; }

		/// <summary>
		/// Shadow projection matrix
		/// </summary>
		Matrix ProjectionMatrix { get; }

		/// <summary>
		/// Gets and sets region
		/// </summary>
		Rectangle ShadowRegion { get; }

		/// <summary>
		/// Indicates that shadow image is bad or 
		/// outdated and need to be rendered again
		/// </summary>
		bool ShadowDirty { get; }

		/// <summary>
		/// Indicates that region must be re-allocated
		/// </summary>
		bool RegionDirty { get; }

		/// <summary>
		/// Shadowmap normalized scale offset
		/// </summary>
		Vector4 RegionScaleOffset { get; }

		/// <summary>
		/// Sets shadow map region.
		/// </summary>
		/// <param name="region"></param>
		/// <param name="shadowMapSize"></param>
		void SetShadowRegion( Rectangle region, int shadowMapSize );
	}

}
