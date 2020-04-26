using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics.GI
{
	public class RadiositySettings
	{
		public const int MapPatchLevels = 6;

		/// <summary>
		///	Number of light-gird elements along X-axis
		/// </summary>
		[Category("Light Grid")]
		[AEDisplayName("LightGrid Width")]
		public int LightGridWidth { get; set; } = 32;

		/// <summary>
		///	Number of light-gird elements along Y-axis
		/// </summary>
		[Category("Light Grid")]
		[AEDisplayName("LightGrid Height")]
		public int LightGridHeight { get; set; } = 16;

		/// <summary>
		///	Number of light-gird elements along Z-axis
		/// </summary>
		[Category("Light Grid")]
		[AEDisplayName("LightGrid Depth")]
		public int LightGridDepth { get; set; } = 32;

		/// <summary>
		///	Distance between two neighboring grid elements
		/// </summary>
		[Category("Light Grid")]
		[AEDisplayName("LightGrid Step")]
		public float LightGridStep { get; set; } = 16;


		/// <summary>
		///	Offset along normal to surface to prevent self occlusion.
		///	Offset is applied when light-map gbuffer is rasterized.
		/// </summary>
		[Category("Light Map")]
		public float NormalOffsetBias { get; set; } = 0.25f;

		/// <summary>
		///	Number of rays used to search for light radiating patches for lightmap
		/// </summary>
		[Category("Quality")]
		public int LightMapSampleCount { get; set; } = 128;

		/// <summary>
		///	Number of rays used to search for light radiating patches for lightgrid
		/// </summary>
		[Category("Quality")]
		public int LightGridSampleCount { get; set; } = 64;

		/// <summary>
		///	Maximum number of radiating patches for each lightmap texel
		/// </summary>
		[Category("Light Map")]
		public int MaxPatches { get; set; } = 32;

		/// <summary>
		///	Threshold to discard patch: L = cos(theta) / (patchSize + R^2) * patchSize^2
		/// </summary>
		[Category("Light Map")]
		[AEDisplayName("Radiance Threshold")]
		public float RadianceThreshold { get; set; } = 0.02f;

		/// <summary>
		///	Produce debugging images for albedo, normals, etc
		/// </summary>
		public bool DebugLightmaps { get; set; } = false;
	}
}
