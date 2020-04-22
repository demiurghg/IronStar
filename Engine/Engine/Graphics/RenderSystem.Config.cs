using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Core.Configuration;
using System.ComponentModel;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	public partial class RenderSystem : GameComponent {

		public const string LightmapPath = @"maps\lightmaps";

		public const int	LightmapSize				=	2048;

		public const int	LightVolumeWidth			=	64;
		public const int	LightVolumeHeight			=	32;
		public const int	LightVolumeDepth			=	64;

		public const int	PreviewWidth				=	320;
		public const int	PreviewHeight				=	180;

		public const int	LightProbeSize				= 128;
		public const int	LightProbeMaxMips			= 6;
		public const int	LightProbeMaxSpecularMip	= LightProbeMaxMips-1;

		public const int	LightProbeBatchSize			= 32;

		public const int	MaxDecals		=	1024;
		public const int	MaxOmniLights	=	1024;
		public const int	MaxEnvLights	=	256;
		public const int	MaxSpotLights	=	16;


		/// <summary>
		/// Fullscreen
		/// </summary>
		[Config]
		public bool Fullscreen { 
			get { 
				return isFullscreen;
			}
			set { 
				if (isFullscreen!=value) {
					isFullscreen = value;
					if (Device!=null && Device.IsInitialized) {
						Device.FullScreen = value;
					}
				}
			}
		}
		bool isFullscreen = false;

		/// <summary>
		/// Screen width
		/// </summary>
		[Config]
		public int	Width { get; set; }

		/// <summary>
		/// Screen height
		/// </summary>
		[Config]
		public int	Height { get; set; }
		
		/// <summary>
		/// Stereo mode.
		/// </summary>
		[Config]
		public StereoMode StereoMode { get; set; }

		/// <summary>
		/// Interlacing mode for stereo.
		/// </summary>
		[Config]
		public InterlacingMode InterlacingMode { get; set; }

		/// <summary>
		/// Vertical synchronization interval.
		/// </summary>
		[Config]
		public int VSyncInterval { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Config]
		public bool UseDebugDevice { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Config]
		public bool MsaaEnabled { get; set; }


		/// <summary>
		/// Shows G-buffer content.
		///		0 - show final image
		///		1 - show diffuse
		///		2 - show specular
		///		3 - show normal map
		/// </summary>
		[Config]
		public int ShowGBuffer { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool ShowParticles { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool ShowExtents { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool SkipParticles { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool SkipParticleShadows { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool SkipParticlesSimulation { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool SkipSceneRendering { get; set; }

		/// <summary>
		/// Skips feed-back buffer reading
		/// </summary>
		[Config]
		public bool SkipFeedback { get; set; }


		/// <summary>
		/// 
		/// </summary>
		[Config]
		public bool SkipDebugRendering { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public QualityLevel ShadowQuality { get; set; }	= QualityLevel.Medium;

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		public bool FreezeParticles { get; set; }


		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		public bool ShowCounters { get; set; }

		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		public bool ShowLightCounters { get; set; }

		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		public bool SkipDirectLighting { get; set; } = false;

		/// <summary>
		/// 
		/// </summary>
		[Config]	
		[AECategory("Postprocessing")]	
		public bool		UseFXAA { get; set; }

		[Config]
		[AECategory("Debug Render")]
		public bool SkipGhostDebugRendering { get; set; }

		[AECategory("Lightmap")] [Config]	public bool UsePointLightmapSampling { get; set; } = false;

		[AECategory("Shadows")]  [Config]	public bool UsePointShadowSampling { get; set; } = false;
		[AECategory("Shadows")]  [Config]	public bool SnapShadowmapCascades { get; set; } = true;
		[AECategory("Shadows")]  [Config]	public bool LockLessDetailedSplit { get; set; } = false;

		[AECategory("Shadows")]  [Config]	public float ShadowGradientBiasX { get; set; } = 1;
		[AECategory("Shadows")]  [Config]	public float ShadowGradientBiasY { get; set; } = 1;

		[AECategory("Shadows")]  [Config]	public float ShadowCascadeDepth { get; set; } = 1024;
		[AECategory("Shadows")]  [Config]	public float ShadowCascadeFactor { get; set; } = 3;
		[AECategory("Shadows")]  [Config]	public float ShadowCascadeSize { get; set; } = 4;

		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias0 { get; set; } = 1;
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias1 { get; set; } = 1;
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias2 { get; set; } = 1;
		[AECategory("Shadows")]  [Config]	public float CSMSlopeBias3 { get; set; } = 1;
		[AECategory("Shadows")]  [Config]	public float CSMDepthBias0 { get; set; } = 1E-07f;
		[AECategory("Shadows")]  [Config]	public float CSMDepthBias1 { get; set; } = 1E-07f;
		[AECategory("Shadows")]  [Config]	public float CSMDepthBias2 { get; set; } = 1E-07f;
		[AECategory("Shadows")]  [Config]	public float CSMDepthBias3 { get; set; } = 1E-07f;

		[AECommand]
		public void KillParticles ()
		{
			RenderWorld.ParticleSystem.KillParticles();
		}
	}
}
