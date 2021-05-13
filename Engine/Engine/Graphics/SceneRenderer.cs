using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Development;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Engine.Graphics.Scenes;
using System.Runtime.CompilerServices;
using System.IO;

namespace Fusion.Engine.Graphics 
{
	internal partial class SceneRenderer : RenderComponent 
	{
		[ShaderDefine]
		const int BatchSize = 128;

		static FXConstantBuffer<GpuData.CAMERA>				regCamera			= new CRegister( 0, "Camera"			);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight		= new CRegister( 1, "DirectLight"		);
		static FXConstantBuffer<STAGE>						regStage			= new CRegister( 2, "Stage"				);
		static FXConstantBuffer<INSTANCE>					regInstance			= new CRegister( 3, BatchSize,				"Instance"	);
		static FXConstantBuffer<SUBSET>						regSubset			= new CRegister( 4,							"Subset"	);
		static FXConstantBuffer<Matrix>						regBones			= new CRegister( 5, RenderSystem.MaxBones,	"Bones"		);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow	= new CRegister( 6, "CascadeShadow"		);
		static FXConstantBuffer<Fog.FOG_DATA>				regFog				= new CRegister( 7, "Fog"				);

		static FXTexture2D<Vector4>				regTexture0				=	new TRegister( 0, "Texture0"			);
		static FXTexture2D<Vector4>				regTexture1				=	new TRegister( 1, "Texture1"			);
		static FXTexture2D<Vector4>				regTexture2				=	new TRegister( 2, "Texture2"			);
		static FXTexture2D<Vector4>				regTexture3				=	new TRegister( 3, "Texture3"			);
		static FXTexture2D<Vector4>				regMipIndex				=	new TRegister( 4, "MipIndex"			);

		static FXTexture3D<UInt2>				regClusterArray				=	new TRegister( 5, "ClusterArray"			);
		static FXBuffer<uint>					regClusterIndexBuffer		=	new TRegister( 6, "ClusterIndexBuffer"		);			 
		static FXStructuredBuffer<LIGHT>		regClusterLightBuffer		=	new TRegister( 7, "ClusterLightBuffer"		);
		static FXStructuredBuffer<DECAL>		regClusterDecalBuffer		=	new TRegister( 8, "ClusterDecalBuffer"		);			 
		static FXStructuredBuffer<LIGHTPROBE>	regClusterLightProbeBuffer	=	new TRegister( 9, "ClusterLightProbeBuffer"	);

		static FXTexture2D<Vector4>				regDecalImages			=	new TRegister(10, "DecalImages"			);
		static FXTexture2D<Vector4>				regShadowMap			=	new TRegister(11, "ShadowMap"			);
		static FXTexture2D<Vector4>				regShadowMapParticles	=	new TRegister(12, "ShadowMapParticles"	);
		static FXTexture2D<Vector4>				regAmbientOcclusion		=	new TRegister(13, "AmbientOcclusion"	);

		static FXTexture2D<Vector4>				regIrradianceMapL0		=	new TRegister(14, "IrradianceMapL0"		);
		static FXTexture2D<Vector4>				regIrradianceMapL1		=	new TRegister(15, "IrradianceMapL1"		);
		static FXTexture2D<Vector4>				regIrradianceMapL2		=	new TRegister(16, "IrradianceMapL2"		);
		static FXTexture2D<Vector4>				regIrradianceMapL3		=	new TRegister(17, "IrradianceMapL3"		);
		static FXTexture3D<Vector4>				regIrradianceVolumeL0	=	new TRegister(18, "IrradianceVolumeL0"	);
		static FXTexture3D<Vector4>				regIrradianceVolumeL1	=	new TRegister(19, "IrradianceVolumeL1"	);
		static FXTexture3D<Vector4>				regIrradianceVolumeL2	=	new TRegister(20, "IrradianceVolumeL2"	);
		static FXTexture3D<Vector4>				regIrradianceVolumeL3	=	new TRegister(21, "IrradianceVolumeL3"	);

		static FXTextureCubeArray<Vector4>		regRadianceCache		=	new TRegister(22, "RadianceCache"		);
		static FXTexture2D<Vector4>				regEnvLut				=	new TRegister(23, "EnvLut"				);
		static FXTexture3D<Vector4>				regFogVolume			=	new TRegister(24, "FogVolume"			);

		static FXSamplerState					regSamplerLinear		=	new SRegister( 0, "SamplerLinear"		);
		static FXSamplerState					regSamplerPoint			=	new SRegister( 1, "SamplerPoint"		);
		static FXSamplerState					regSamplerLightmap		=	new SRegister( 2, "SamplerLightmap"		);
		static FXSamplerState					regDecalSampler			=	new SRegister( 3, "DecalSampler"		);
		static FXSamplerState					regParticleSampler		=	new SRegister( 4, "ParticleSampler"		);
		static FXSamplerState					regMipSampler			=	new SRegister( 5, "MipSampler"			);
		static FXSamplerState					regSamplerLinearClamp	=	new SRegister( 6, "SamplerLinearClamp"	);
		static FXSamplerComparisonState			regShadowSampler		=	new SRegister( 7, "ShadowSampler"		);

		Ubershader		surfaceShader;
		StateFactory	factory;
		UserTexture		envLut;
		VTSystem		vt;
		ShadowSystem	ss;
		RenderWorld		rw;

		STAGE			cbDataStage		=	new STAGE();

		ConstantBuffer		constBufferStage	;
		ConstantBuffer		constBufferBones	;
		ConstantBuffer		constBufferInstance	;
		ConstantBuffer		constBufferInstanceBatch	;
		ConstantBuffer		constBufferSubset	;

		/// <summary>
		/// Gets pipeline state factory
		/// </summary>
		internal StateFactory Factory {
			get {
				return factory;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="Game"></param>
		public SceneRenderer ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{
			LoadContent();

			constBufferStage			=	new ConstantBuffer( Game.GraphicsDevice, typeof(STAGE) );
			constBufferBones			=	new ConstantBuffer( Game.GraphicsDevice, typeof(Matrix), RenderSystem.MaxBones );
			constBufferInstance			=	new ConstantBuffer( Game.GraphicsDevice, typeof(INSTANCE) );
			constBufferInstanceBatch	=	new ConstantBuffer( Game.GraphicsDevice, typeof(INSTANCE), BatchSize );
			constBufferSubset			=	new ConstantBuffer( Game.GraphicsDevice, typeof(SUBSET) );

			using ( var ms = new MemoryStream( Properties.Resources.envLut ) ) 
			{
				envLut    =   UserTexture.CreateFromTga( rs, ms, false );
			}

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			surfaceShader	=	Game.Content.Load<Ubershader>("surface");
			factory			=	surfaceShader.CreateFactory( typeof(SurfaceFlags), (ps,i) => Enum(ps, (SurfaceFlags)i ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void Enum ( PipelineState ps, SurfaceFlags flags )
		{
			ps.RasterizerState	=	RasterizerState.CullCW;

			if (flags.HasFlag( SurfaceFlags.SKINNED )) 
			{
				ps.VertexInputElements	=	VertexColorTextureTBNSkinned.Elements;
			}
			
			if (flags.HasFlag( SurfaceFlags.RIGID )) 
			{
				ps.VertexInputElements	=	VertexColorTextureTBNRigid.Elements;
			}

			if (flags.HasFlag( SurfaceFlags.SHADOW )) 
			{
				if (flags.HasFlag( SurfaceFlags.SPOT )) ps.RasterizerState = rs.ShadowSystem.SpotShadowRasterizerState;
				if (flags.HasFlag( SurfaceFlags.CSM )) 	ps.RasterizerState = rs.ShadowSystem.CascadeShadowRasterizerState;
			}

			if (flags.HasFlag( SurfaceFlags.TRANSPARENT)) 
			{
				ps.BlendState = BlendState.Opaque;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref constBufferStage );
				SafeDispose( ref constBufferBones );
				SafeDispose( ref constBufferInstance );
				SafeDispose( ref constBufferInstanceBatch );
				SafeDispose( ref constBufferSubset );
				SafeDispose( ref envLut );
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="view"></param>
		/// <param name="proj"></param>
		/// <param name="viewPos"></param>
		/// <param name="vpWidth"></param>
		/// <param name="vpHeight"></param>
		public bool SetupStage ( StereoEye stereoEye, IRenderContext context, InstanceGroup instanceGroup )
		{
			device.ResetStates();

			context.SetupRenderTargets( device );
			device.SetViewport( context.Viewport );
			device.SetScissorRect( context.Viewport.Bounds );
			
			var width	=	context.Viewport.Width;
			var height	=	context.Viewport.Height;

			cbDataStage.WorldToLightVolume	=	rw.LightMap.WorldToVolume;
			cbDataStage.VTGradientScaler	=	VTConfig.PageSize * VTConfig.VirtualPageCount / (float)rs.VTSystem.PhysicalPages0.Width;
			cbDataStage.VTPageScaleRCP		=	rs.VTSystem.PageScaleRCP;
			cbDataStage.SsaoWeight			=	instanceGroup.HasFlag(InstanceGroup.Weapon) ? 0 : 1;
			cbDataStage.ViewportSize		=	new Vector4( width, height, 1.0f / width, 1.0f / height );
			cbDataStage.DepthBias			=	context.DepthBias;
			cbDataStage.SlopeBias			=	context.SlopeBias;
			cbDataStage.DirectLightFactor	=	rs.SkipDirectLighting ? 0 : 1;
			cbDataStage.IndirectLightFactor	=	rs.Radiosity.MasterIntensity;
			cbDataStage.ShowLightComplexity	=	rs.ShowLightComplexity ? 1 : 0;

			constBufferStage.SetData( ref cbDataStage );

			//-----------------------------

			device.GfxConstants[ regCamera			]	= context.GetCamera().CameraData;
			device.GfxConstants[ regDirectLight		]	= rs.LightManager.DirectLightData;
			device.GfxConstants[ regStage			]	= constBufferStage;
			device.GfxConstants[ regInstance		]	= rs.UseBatching ? constBufferInstanceBatch : constBufferInstance;
			device.GfxConstants[ regSubset			]	= constBufferSubset;
			device.GfxConstants[ regBones			]	= constBufferBones;
			device.GfxConstants[ regCascadeShadow	]	= rs.ShadowSystem.ShadowMap.UpdateCascadeShadowConstantBuffer();
			device.GfxConstants[ regFog				]	= rs.Fog.FogData;

			//-----------------------------

			device.GfxResources[ regTexture0				]	=	rs.VTSystem.PageTable;
			device.GfxResources[ regTexture1				]	=	rs.VTSystem.PhysicalPages0;
			device.GfxResources[ regTexture2				]	=	rs.VTSystem.PhysicalPages1;
			device.GfxResources[ regTexture3				]	=	rs.VTSystem.PhysicalPages2;
			device.GfxResources[ regMipIndex				]	=	rs.VTSystem.MipIndex;

			device.GfxResources[ regClusterArray			]	=	rs.LightManager.LightGrid.GridTexture;
			device.GfxResources[ regClusterIndexBuffer		]	=	rs.LightManager.LightGrid.IndexDataGpu;						
			device.GfxResources[ regClusterLightBuffer		]	=	rs.LightManager.LightGrid.LightDataGpu;
			device.GfxResources[ regClusterDecalBuffer		]	=	rs.LightManager.LightGrid.DecalDataGpu;						

			device.GfxResources[ regDecalImages				]	=	rs.RenderWorld.LightSet?.DecalAtlas?.Texture?.Srv;
			device.GfxResources[ regShadowMap				]	=	context.RequireShadows ? rs.ShadowSystem.ShadowMap.ShadowTexture : null;
			device.GfxResources[ regShadowMapParticles		]	=	context.RequireShadows ? rs.ShadowSystem.ShadowMap.ParticleShadowTexture : null;
			device.GfxResources[ regAmbientOcclusion		]	=	context.RequireShadows ? rs.RenderWorld.HdrFrame.AOBuffer.GetShaderResource(0) : null;

			device.GfxResources[ regIrradianceMapL0			]	=	rw.LightMap?.GetLightmap(0);
			device.GfxResources[ regIrradianceMapL1			]	=	rw.LightMap?.GetLightmap(1);
			device.GfxResources[ regIrradianceMapL2			]	=	rw.LightMap?.GetLightmap(2);
			device.GfxResources[ regIrradianceMapL3			]	=	rw.LightMap?.GetLightmap(3);

			device.GfxResources[ regIrradianceVolumeL0		]	=	rw.LightMap?.GetVolume(0);
			device.GfxResources[ regIrradianceVolumeL1		]	=	rw.LightMap?.GetVolume(1);
			device.GfxResources[ regIrradianceVolumeL2		]	=	rw.LightMap?.GetVolume(2);
			device.GfxResources[ regIrradianceVolumeL3		]	=	rw.LightMap?.GetVolume(3);

			device.GfxResources[ regRadianceCache			]	=	rs.RenderWorld.LightProbes?.GetLightProbeCubeArray();
			device.GfxResources[ regEnvLut					]	=	envLut.Srv;
			device.GfxResources[ regClusterLightProbeBuffer	]	=	rs.LightManager.LightGrid.ProbeDataGpu;
			device.GfxResources[ regFogVolume				]	=	rs.Fog.FogGrid;

			//-----------------------------

			device.GfxSamplers[ regSamplerLinear		]	=	rs.VTSystem.UseAnisotropic ? SamplerState.VTAnisotropic : SamplerState.VTTrilinear;
			device.GfxSamplers[ regSamplerPoint			]	=	SamplerState.PointClamp;
			device.GfxSamplers[ regSamplerLightmap		]	=	rs.UsePointLightmapSampling ? SamplerState.LightmapSamplerPoint : SamplerState.LightmapSamplerLinear;
			device.GfxSamplers[ regDecalSampler			]	=	SamplerState.LinearClamp4Mips;
			device.GfxSamplers[ regParticleSampler		]	=	SamplerState.LinearClamp;
			device.GfxSamplers[ regMipSampler			]	=	rs.VTSystem.UseAnisotropic ? SamplerState.VTAnisotropicIndex : SamplerState.VTTrilinearIndex;
			device.GfxSamplers[ regSamplerLinearClamp	]	=	SamplerState.LinearClamp;
			device.GfxSamplers[ regShadowSampler		]	=	ss.UsePointShadowSampling ? SamplerState.ShadowSamplerPoint : SamplerState.ShadowSampler;

			//-----------------------------

			if (surfaceShader==null || rs.SkipSceneRendering) {
				return false;
			} else {
				return true;
			}
		}



		bool SetupInstance ( SurfaceFlags stageFlag, IRenderContext context, RenderInstance instance, bool instanced )
		{
			if (!instance.Visible) return false;

			bool aniso	=	vt.UseAnisotropic ;

			var  flag	=	stageFlag | (instance.IsSkinned ? SurfaceFlags.SKINNED : SurfaceFlags.RIGID);

			if (aniso && stageFlag==SurfaceFlags.FORWARD) {
				flag |= SurfaceFlags.ANISOTROPIC;
			}

			if (context.Transparent) {
				flag |= SurfaceFlags.TRANSPARENT;
			}

			if ( stageFlag==SurfaceFlags.FORWARD || stageFlag==SurfaceFlags.RADIANCE ) {
				if ( instance.Group==InstanceGroup.Static ) {
					flag |= SurfaceFlags.IRRADIANCE_MAP;
				} else {
					flag |= SurfaceFlags.IRRADIANCE_VOLUME;
				}
			}

			if (!factory.IsCombinationSupported( (int)flag ))
			{
				return false;
			}

			device.PipelineState	=	factory[ (int)flag ];

			if (instance.IsSkinned)
			{
				constBufferBones.SetData( instance.BoneTransforms );
			}

			if (!instanced) 
			{
				constBufferInstance.SetData( new INSTANCE(instance) );
			}

			return true;
		}



		bool SetupAndDrawSubset ( RenderInstance instance, int subsetIndex, bool transparentPass, bool instanced = false, int instanceCount = 0 )
		{
			int startPrimitive;
			int primitiveCount;
			bool isTransparent;
			SUBSET subsetData = new SUBSET();

			instance.GetSubsetData( subsetIndex, ref subsetData, out isTransparent, out startPrimitive, out primitiveCount );

			if (isTransparent==transparentPass)
			{
				constBufferSubset.SetData( ref subsetData );

				if (instanced)
				{
					device.DrawInstancedIndexed( primitiveCount*3, instanceCount, startPrimitive*3, 0, 0 );
				}
				else
				{
					device.DrawIndexed( primitiveCount*3, startPrimitive*3, 0 );
				}

				return true;
			}
			else
			{
				return false;
			}
		}


		INSTANCE[] instanceData = new INSTANCE[ BatchSize ];

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		/// <param name="context"></param>
		/// <param name="rw"></param>
		void RenderGeneric ( string eventName, GameTime gameTime, StereoEye stereoEye, SurfaceFlags surfFlags, IRenderContext context, IEnumerable<RenderInstance> instances, InstanceGroup instanceGroup )
		{
			vt	=	rs.VTSystem;
			ss	=	rs.ShadowSystem;
			rw	=	rs.RenderWorld;
			
			using ( new PixEvent(eventName) ) 
			{
				var transparent		=	context.Transparent;
			
				if ( SetupStage( stereoEye, context, instanceGroup ) ) 
				{
					if (rs.UseBatching)
					{
						var batchGroups = instances.GroupBy( inst => inst.InstanceRef ).Select( g => g.ToArray() );

						foreach ( var group in batchGroups )
						{
							var template		=	group.First();  
							var instanceCount	=	group.Count();
							var passCount		=	MathUtil.IntDivRoundUp( instanceCount, BatchSize );
							/*var instData	= group.Select( g => new INSTANCE(g) ).ToArray();
							instanceData.SetData( instData );					 */
							for (int passId=0; passId<passCount; passId++)
							{
								int  passInstanceCount = Math.Min( BatchSize, instanceCount - passId * BatchSize );
								
								for (int instanceId=0; instanceId<passInstanceCount; instanceId++)
								{
									instanceData[ instanceId ] = new INSTANCE(group[ passId * BatchSize + instanceId ]);
								}

								constBufferInstanceBatch.SetData( instanceData );

								if ( SetupInstance( surfFlags, context, template, true ) ) 
								{
									device.SetupVertexInput( template.vb, template.ib );

									for ( int subsetIndex = 0; subsetIndex < template.GetSubsetCount(); subsetIndex++ )
									{
										SetupAndDrawSubset( template, subsetIndex, transparent, true, passInstanceCount );
									}
								}
							}
						}
					}
					else
					{
						foreach ( var instance in instances ) 
						{
							if ( SetupInstance( surfFlags, context, instance, false ) ) 
							{
								device.SetupVertexInput( instance.vb, instance.ib );

								for ( int subsetIndex = 0; subsetIndex < instance.GetSubsetCount(); subsetIndex++ )
								{
									SetupAndDrawSubset( instance, subsetIndex, transparent );
								}
							}
						}
					}
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		internal void GatherVTFeedbackAndUpdate ( GameTime gameTime, HdrFrame frame )
		{
			rs.Filter.StretchRect( frame.FeedbackBufferRB.Surface, frame.FeedbackBuffer, SamplerState.PointClamp );

			var feedbackBuffer = vt.FeedbackBufferPool.Alloc();
			
			frame.FeedbackBufferRB.GetFeedback( feedbackBuffer );
			vt.Update( feedbackBuffer, gameTime );

			vt.FeedbackBufferPool.Recycle( feedbackBuffer );
		}



		internal void RenderForwardSolid ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderList renderList, InstanceGroup mask )
		{	
			var context		=	new ForwardSolidContext( camera, frame );
			var instances	=	renderList.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderForwardSolid", gameTime, stereoEye, SurfaceFlags.FORWARD, context, instances, mask );
		}


		internal void RenderForwardTransparent ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderList renderList, InstanceGroup mask )
		{		
			var context		=	new ForwardTransparentContext( camera, frame );
			var instances	=	renderList.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderForwardTransparent", gameTime, stereoEye, SurfaceFlags.FORWARD, context, instances, mask );
		}


		internal void RenderZPass ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderList renderList, InstanceGroup mask )
		{		
			if (rs.SkipZPass) 
			{
				return;
			}

			var context		=	new ForwardZPassContext( camera, frame );
			var instances	=	renderList.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderZPass", gameTime, stereoEye, SurfaceFlags.ZPASS, context, instances, mask );
		}
		

		internal void RenderShadowMap ( ShadowContext shadowContext, RenderList renderList, InstanceGroup mask, bool csm )
		{
			if (rs.SkipShadows)
			{
				return;
			}

			var instances	=	renderList.Where( inst => ((inst.Group & mask) != 0) && !inst.NoShadow );

			var flags = SurfaceFlags.SHADOW | (csm ? SurfaceFlags.CSM : SurfaceFlags.SPOT );

			RenderGeneric("ShadowMap", null, StereoEye.Mono, flags, shadowContext, instances, mask );
		}


		internal void RenderLightProbeGBuffer ( LightProbeContext context, RenderWorld rw, InstanceGroup mask )
		{
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("LightProbeGBuffer", null, StereoEye.Mono, SurfaceFlags.GBUFFER, context, instances, mask );
		}


		internal void RenderLightProbeRadiance ( LightProbeContext context, RenderWorld rw, InstanceGroup mask )
		{
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("LightProbeRadiance", null, StereoEye.Mono, SurfaceFlags.RADIANCE, context, instances, mask );
		}
	}
}
