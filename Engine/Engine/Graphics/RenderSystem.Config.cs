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
using Fusion.Widgets.Advanced;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics 
{
	public enum VisualizeBuffer
	{
		None,
		Normals,
		DofCOC,
		DofForeground,
		HdrTarget,
		SSAOBuffer,
		Shadows,
		ParticleShadows,
		ParticleLightmap,
		VTFeedbackBuffer,
		VTPhysicalTexture,
		VTPageTexture,
	}

	public partial class RenderSystem : GameComponent 
	{
		[ShaderDefine] public const int		MaxBones					=	128;

		[ShaderDefine] public const int		LightClusterGridWidth		=	16;
		[ShaderDefine] public const int		LightClusterGridHeight		=	 8;
		[ShaderDefine] public const int		LightClusterGridDepth		=	24;
		[ShaderDefine] public const float	LightClusterExpScale		=	0.015625f;

		[ShaderDefine]	public const int	VTVirtualPageCount			=	VTConfig.VirtualPageCount;
		[ShaderDefine]	public const int	VTPageSize					=	VTConfig.PageSize;
		[ShaderDefine]	public const int	VTMaxMip					=	VTConfig.MaxMipLevel;
		[ShaderDefine]	public const int	VTMipSelectorScale			=	(VTConfig.PageSize >> VTMaxMip) * VTConfig.VirtualPageCount;

		[ShaderDefine]	public const uint	LightTypeNone				=	0;
		[ShaderDefine]	public const uint	LightTypeOmni				=	1;
		[ShaderDefine]	public const uint	LightTypeOmniShadow			=	2;
		[ShaderDefine]	public const uint	LightTypeSpotShadow			=	3;
		[ShaderDefine]	public const uint	LightTypeAmbient			=	4;
		[ShaderDefine]	public const uint	LightSpotShapeSquare		=	0x00010000;
		[ShaderDefine]	public const uint	LightSpotShapeRound			=	0x00020000;

		[ShaderDefine]	public const uint	LightProbeSize				=	128;
		[ShaderDefine]	public const uint	LightProbeMaxMips			=	7;
		[ShaderDefine]	public const uint	LightProbeMaxMip			=	6;
		[ShaderDefine]	public const uint	LightProbeMaxSpecularMip	=	LightProbeMaxMip - 1;

		[ShaderDefine]	public const uint	InstanceGroupStatic			=	(int)InstanceGroup.Static;
		[ShaderDefine]	public const uint	InstanceGroupLightmap		=	(int)InstanceGroup.Lightmap;
		[ShaderDefine]	public const uint	InstanceGroupLightmapProxy	=	(int)InstanceGroup.LightmapProxy;
		[ShaderDefine]	public const uint	InstanceGroupWeapon			=	(int)InstanceGroup.Weapon;

		public static float MetersToGameUnit( float v ) { return v / 0.32f; }
		public static float GameUnitToMeters( float v ) { return v * 0.32f; }

		public const string LightmapPath	= @"maps\lightmaps";
		public const string LightProbePath	= @"maps\lightprobes";

		public const int	LightmapSize				=	2048;

		public const int	LightVolumeWidth			=	64;
		public const int	LightVolumeHeight			=	32;
		public const int	LightVolumeDepth			=	64;

		public const int	PreviewWidth				=	320;
		public const int	PreviewHeight				=	180;

		public const int	LightProbeBatchSize			= 32;

		public const int	MaxDecals		=	1024;
		public const int	MaxOmniLights	=	1024;
		public const int	MaxEnvLights	=	256;
		public const int	MaxSpotLights	=	16;



		/// <summary>
		/// Fullscreen
		/// </summary>
		[Config]
		[AECategory("Video")]
		// #TODO #CFG -- restore
		public static bool Fullscreen { get; set; } = false;

		/// <summary>
		/// Screen width
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static int	Width { get; set; } = 1024;

		/// <summary>
		/// Screen height
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static int	Height { get; set; } = 768;
		
		/// <summary>
		/// Stereo mode.
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static StereoMode StereoMode { get; set; } = StereoMode.Disabled;

		/// <summary>
		/// Interlacing mode for stereo.
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static InterlacingMode InterlacingMode { get; set; }	= InterlacingMode.HorizontalLR;

		/// <summary>
		/// Vertical synchronization interval.
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static int VSyncInterval { get; set; } = 1;

		/// <summary>
		/// Vertical synchronization interval.
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static bool ClearBackbuffer { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static bool UseDebugDevice { get; set; } = false;

		/// <summary>
		/// 
		/// </summary>
		[Config]
		[AECategory("Video")]
		public static bool UseRenderDoc { get; set; } = false;


		[Config]
		[AECategory("Performance")]
		public static bool UseBatching { get; set; } = true;


		[Config]
		[AECategory("Debug")]
		public static VisualizeBuffer VisualizeBuffer { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool ShowParticles { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool ShowExtents { get; set; }


		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipParticles { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipParticleShadows { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipParticlesSimulation { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipSceneRendering { get; set; }

		/// <summary>
		/// Skips feed-back buffer reading
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipFeedback { get; set; }


		/// <summary>
		/// Skips feed-back buffer reading
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipZPass { get; set; }


		/// <summary>
		/// Skips feed-back buffer reading
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipShadows { get; set; }


		[Config]
		[AECategory("Performance")]
		public static bool SkipSpotLights { get; set; }

		[Config]
		[AECategory("Performance")]
		public static bool SkipDecals { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipDebugRendering { get; set; }

		/// <summary>
		/// Shows particles statistics.
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool FreezeParticles { get; set; }


		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool ShowCounters { get; set; }

		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool ShowLightCounters { get; set; }


		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool ShowLightComplexity { get; set; }

		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipDirectLighting { get; set; } = false;

		/// <summary>
		/// Shows counters
		/// </summary>
		[Config]
		[AECategory("Debug")]
		public static bool SkipBackgroundBlur { get; set; } = false;

		/// <summary>
		/// 
		/// </summary>
		[Config]	
		[AECategory("Postprocessing")]	
		public static bool		UseFXAA { get; set; } = true;

		[Config]
		[AECategory("Debug Render")]
		public static bool SkipGhostDebugRendering { get; set; }

		[AECategory("Lightmap")] [Config]	public static bool UsePointLightmapSampling { get; set; } = false;

		[Config]	
		[AECategory("Lightmap")] 
		[AESlider(0,10,1f,0.1f)]
		public static float SpecularLightmapFactor { get; set; } = 1.0f;
		[Config]	
		[AECategory("Lightmap")]
		[AESlider(0,1,0.1f,0.01f)]
		public static float SpecularLightmapThreshold { get; set; } = 0.5f;

		[AECommand]
		public static void KillParticles ()
		{
			//	#TODO #CONFIG #CFG 
			//	RenderWorld.ParticleSystem.KillParticles();
		}

		[AECategory("Visibility")]  [Config]	public static bool LockVisibility { get; set; } = false;
		[AECategory("Visibility")]  [Config]	public static bool SkipFrustumCulling { get; set; } = false;
		[AECategory("Visibility")]  [Config]	public static bool ShowBoundingBoxes { get; set; } = false;
	}
}
