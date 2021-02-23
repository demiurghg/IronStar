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
		public const int UpdateRegionSize = 256;
		public const int TileSize = 16;
		public const int ClusterSize = 4;

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
		public int LightGridStep { get; set; } = 16;


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
