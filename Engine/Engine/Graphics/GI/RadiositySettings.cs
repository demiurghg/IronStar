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
		public const int TileSize = 8;
		public const int MaxPatchesPerTile = 2048;

		/// <summary>
		///	Number of light-gird elements along X-axis
		/// </summary>
		[AECategory("Light Grid")]
		[AEDisplayName("LightGrid Width")]
		public int LightGridWidth { get; set; } = 32;

		/// <summary>
		///	Number of light-gird elements along Y-axis
		/// </summary>
		[AECategory("Light Grid")]
		[AEDisplayName("LightGrid Height")]
		public int LightGridHeight { get; set; } = 16;

		/// <summary>
		///	Number of light-gird elements along Z-axis
		/// </summary>
		[AECategory("Light Grid")]
		[AEDisplayName("LightGrid Depth")]
		public int LightGridDepth { get; set; } = 32;

		/// <summary>
		///	Distance between two neighboring grid elements
		/// </summary>
		[AECategory("Light Grid")]
		[AEDisplayName("LightGrid Step")]
		public float LightGridStep { get; set; } = 16;


		/// <summary>
		///	Offset along normal to surface to prevent self occlusion.
		///	Offset is applied when light-map gbuffer is rasterized.
		/// </summary>
		[AECategory("Light Map")]
		public float NormalOffsetBias { get; set; } = 0.25f;

		/// <summary>
		///	Number of rays used to search for light radiating patches for lightmap
		/// </summary>
		[AECategory("Quality")]
		[AEValueRange( 32, 1024, 32, 1 )]
		public int LightMapSampleCount { get; set; } = 128;

		/// <summary>
		///	Number of rays used to search for light radiating patches for lightgrid
		/// </summary>
		[AECategory("Quality")]
		[AEValueRange( 32, 1024, 32, 1 )]
		public int LightGridSampleCount { get; set; } = 64;

		/// <summary>
		///	Maximum number of radiating patches for each lightmap texel
		/// </summary>
		[AECategory("Light Map")]
		public int MaxPatches { get; set; } = 32;

		/// <summary>
		///	Threshold to discard patch: L = cos(theta) / (patchSize + R^2) * patchSize^2
		/// </summary>
		[AECategory("Light Map")]
		[AEDisplayName("Radiance Threshold")]
		[AEValueRange( 0, 0.1f, 0.01f, 0.001f )]
		public float RadianceThreshold { get; set; } = 0.02f;

		/// <summary>
		///	Threshold to discard patch: L = cos(theta) / (patchSize + R^2) * patchSize^2
		/// </summary>
		[AECategory("Light Map")]
		[AEDisplayName("Patch Threshold")]
		[AEValueRange(0,2,0.1f,0.01f)]
		public float PatchThreshold { get; set; } = 0.4f;

		/// <summary>
		///	Produce debugging images for albedo, normals, etc
		/// </summary>
		[AECategory("Debug")]
		public bool UseWhiteDiffuse { get; set; } = false;

		/// <summary>
		///	Produce debugging images for albedo, normals, etc
		/// </summary>
		[AECategory("Debug")]
		public bool DebugLightmaps { get; set; } = false;
	}
}
