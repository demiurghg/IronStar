﻿using System;
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

namespace Fusion.Engine.Graphics {

	public partial class RenderSystem : GameComponent {

		public const int MaxBones = 128;

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
		[ShaderDefine]	public const uint	InstanceGroupDynamic		=	(int)InstanceGroup.Dynamic;
		[ShaderDefine]	public const uint	InstanceGroupCharacter		=	(int)InstanceGroup.Character;
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
		public bool ShowLightComplexity { get; set; }

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

		[AECategory("Visibility")]  [Config]	public bool LockVisibility { get; set; } = false;
		[AECategory("Visibility")]  [Config]	public bool SkipFrustumCulling { get; set; } = false;
		[AECategory("Visibility")]  [Config]	public bool ShowBoundingBoxes { get; set; } = false;
	}
}
