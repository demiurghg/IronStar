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
using System.Runtime.CompilerServices;
using System.IO;

namespace Fusion.Engine.Graphics {

	internal partial class SceneRenderer : RenderComponent {

		internal const int MaxBones = 128;

		ConstantBuffer	constBufferStage;
		ConstantBuffer	constBufferInstance;
		ConstantBuffer	constBufferBones;
		ConstantBuffer	constBufferSubset;
		Ubershader		surfaceShader;
		StateFactory	factory;
		UserTexture		envLut;

		STAGE			cbDataStage		=	new STAGE();
		INSTANCE		cbDataInstance	=	new INSTANCE();
		SUBSET			cbDataSubset	=	new SUBSET();

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

			constBufferStage	=	new ConstantBuffer( Game.GraphicsDevice, typeof(STAGE) );
			constBufferInstance	=	new ConstantBuffer( Game.GraphicsDevice, typeof(INSTANCE) );
			constBufferBones	=	new ConstantBuffer( Game.GraphicsDevice, typeof(Matrix), MaxBones );
			constBufferSubset	=	new ConstantBuffer( Game.GraphicsDevice, typeof(SUBSET) );

			using ( var ms = new MemoryStream( Properties.Resources.envLut ) ) {
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

			if (flags.HasFlag( SurfaceFlags.SKINNED )) {
				ps.VertexInputElements	=	VertexColorTextureTBNSkinned.Elements;
			}
			
			if (flags.HasFlag( SurfaceFlags.RIGID )) {
				ps.VertexInputElements	=	VertexColorTextureTBNRigid.Elements;
			}

			if (flags.HasFlag( SurfaceFlags.SHADOW )) {
				ps.RasterizerState = RasterizerState.CullNone;
			}

			if (flags.HasFlag( SurfaceFlags.TRANSPARENT)) {
				ps.BlendState = BlendState.Opaque;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref constBufferStage );
				SafeDispose( ref constBufferInstance );
				SafeDispose( ref constBufferBones );
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
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		public bool SetupStage ( StereoEye stereoEye, IRenderContext context, InstanceGroup instanceGroup )
		{
			device.ResetStates();

			context.SetupRenderTargets( device );
			device.SetViewport( context.Viewport );


			var fog	=	rs.RenderWorld.FogSettings;

			cbDataStage.DirectLightDirection	=	new Vector4( rs.RenderWorld.LightSet.DirectLight.Direction, 0 );
			cbDataStage.DirectLightIntensity	=	rs.RenderWorld.LightSet.DirectLight.Intensity;
			cbDataStage.DirectLightAngularSize	=	rs.RenderWorld.LightSet.DirectLight.AngularSize;

			cbDataStage.CascadeViewProjection0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ViewProjectionMatrix;
			cbDataStage.CascadeViewProjection1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ViewProjectionMatrix;
			cbDataStage.CascadeViewProjection2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ViewProjectionMatrix;
			cbDataStage.CascadeViewProjection3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ViewProjectionMatrix;
			
			cbDataStage.CascadeGradientMatrix0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ComputeGradientMatrix();
			cbDataStage.CascadeGradientMatrix1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ComputeGradientMatrix();
			cbDataStage.CascadeGradientMatrix2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ComputeGradientMatrix();
			cbDataStage.CascadeGradientMatrix3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ComputeGradientMatrix();

			cbDataStage.OcclusionGridMatrix		=	rs.RenderWorld.IrradianceVolume.VolumeTransform;

			cbDataStage.VTGradientScaler		=	VTConfig.PageSize * VTConfig.VirtualPageCount / (float)rs.VTSystem.PhysicalPages0.Width;

			cbDataStage.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;

			cbDataStage.FogColor				=	rs.RenderWorld.FogSettings.Color;
			cbDataStage.FogAttenuation			=	rs.RenderWorld.FogSettings.DistanceAttenuation;

			cbDataStage.SkyAmbientLevel			=	rs.RenderWorld.SkySettings.AmbientLevel;
			cbDataStage.VTPageScaleRCP			=	rs.VTSystem.PageScaleRCP;

			cbDataStage.SsaoWeight				=	instanceGroup.HasFlag(InstanceGroup.Weapon) ? 0 : 1;

			var width	=	context.Viewport.Width;
			var height	=	context.Viewport.Height;

			cbDataStage.View			=	context.GetViewMatrix( stereoEye );
			cbDataStage.Projection		=	context.GetProjectionMatrix( stereoEye );
			cbDataStage.ProjectionFPV	=	rs.RenderWorld.WeaponCamera.GetProjectionMatrix( stereoEye );
			cbDataStage.ViewPos			=	new Vector4( context.GetViewPosition( stereoEye ), 1 );
			cbDataStage.ViewBounds		=	new Vector4( width, height, width, height );
			cbDataStage.BiasSlopeFar	=	new Vector4( context.DepthBias, context.SlopeBias, context.FarDistance, 0 );


			constBufferStage.SetData( cbDataStage );

			//	assign constant buffers :
			device.PixelShaderConstants[0]	=	constBufferStage ;
			device.VertexShaderConstants[0]	=	constBufferStage ;
			device.PixelShaderConstants[1]	=	constBufferInstance ;
			device.VertexShaderConstants[1]	=	constBufferInstance ;
			device.PixelShaderConstants[2]	=	constBufferSubset;
			device.VertexShaderConstants[2]	=	constBufferSubset;

			//	setup shader resources :		
			device.PixelShaderResources[0]	= rs.VTSystem.PageTable;
			device.PixelShaderResources[1]	= rs.VTSystem.PhysicalPages0;
			device.PixelShaderResources[2]	= rs.VTSystem.PhysicalPages1;
			device.PixelShaderResources[3]	= rs.VTSystem.PhysicalPages2;
			device.PixelShaderResources[4]	= rs.VTSystem.MipIndex;
			device.PixelShaderResources[5]	= rs.LightManager.LightGrid.GridTexture;
			device.PixelShaderResources[6]	= rs.LightManager.LightGrid.IndexDataGpu;
			device.PixelShaderResources[7]	= rs.LightManager.LightGrid.LightDataGpu;
			device.PixelShaderResources[8]	= rs.LightManager.LightGrid.DecalDataGpu;
			
			if (context.RequireShadows) {	  // because these maps are used as render targets for shadows
				device.PixelShaderResources[9]	= rs.RenderWorld.LightSet?.DecalAtlas?.Texture?.Srv;
				device.PixelShaderResources[10]	= rs.LightManager.ShadowMap.ColorBuffer;
				device.PixelShaderResources[11]	= rs.LightManager.ShadowMap.ParticleShadow;
				device.PixelShaderResources[12]	= context.GetAOBuffer();
			}

			device.PixelShaderResources[13]	=	rs.RenderWorld.IrradianceMap.IrradianceTextureRed;
			device.PixelShaderResources[14]	=	rs.RenderWorld.IrradianceMap.IrradianceTextureGreen;
			device.PixelShaderResources[15]	=	rs.RenderWorld.IrradianceMap.IrradianceTextureBlue;
			device.PixelShaderResources[16]	=	rs.RenderWorld.IrradianceVolume.IrradianceTextureRed;
			device.PixelShaderResources[17]	=	rs.RenderWorld.IrradianceVolume.IrradianceTextureGreen;
			device.PixelShaderResources[18]	=	rs.RenderWorld.IrradianceVolume.IrradianceTextureBlue;

			device.PixelShaderResources[20]	=	rs.RenderWorld.RadianceCache;
			device.PixelShaderResources[21]	=	envLut.Srv;
			device.PixelShaderResources[22]	=	rs.LightManager.LightGrid.ProbeDataGpu;


			//	setup samplers :
			var shadowSampler	=	rs.UsePointShadowSampling ? SamplerState.ShadowSamplerPoint : SamplerState.ShadowSampler;

			device.PixelShaderSamplers[0]	= rs.VTSystem.UseAnisotropic ? SamplerState.VTAnisotropic : SamplerState.VTTrilinear;
			device.PixelShaderSamplers[1]	= SamplerState.PointClamp;
			device.PixelShaderSamplers[2]	= SamplerState.AnisotropicClamp;
			device.PixelShaderSamplers[3]	= SamplerState.LinearClamp4Mips;
			device.PixelShaderSamplers[4]	= shadowSampler;
			device.PixelShaderSamplers[5]	= SamplerState.LinearClamp;
			device.PixelShaderSamplers[6]	= rs.VTSystem.UseAnisotropic ? SamplerState.VTAnisotropicIndex : SamplerState.VTTrilinearIndex;
			device.PixelShaderSamplers[7]	= SamplerState.LinearClamp;

			if (surfaceShader==null || rs.SkipSceneRendering) {
				return false;
			} else {
				return true;
			}
		}



		bool SetupInstance ( SurfaceFlags stageFlag, IRenderContext context, MeshInstance instance )
		{
			#warning New pipeline state semantic: bool StateFactory.TrySetPipelineState( flags )

			bool aniso	=	rs.VTSystem.UseAnisotropic ;

			int flag = (int)( stageFlag | SurfaceFlags.RIGID );

			if (aniso && stageFlag==SurfaceFlags.FORWARD) {
				flag |= (int)SurfaceFlags.ANISOTROPIC;
			}

			if (context.Transparent) {
				flag |= (int)SurfaceFlags.TRANSPARENT;
			}

			if ( stageFlag==SurfaceFlags.FORWARD) {
				if ( instance.Group==InstanceGroup.Static ) {
					flag |= (int)SurfaceFlags.IRRADIANCE_MAP;
				} else {
					flag |= (int)SurfaceFlags.IRRADIANCE_VOLUME;
				}
			}

			device.PipelineState	=	factory[ flag ];

			cbDataInstance.Group	=	(int)instance.Group;
			cbDataInstance.Color	=	instance.Color;
			cbDataInstance.World	=	instance.World;
			cbDataInstance.LMRegion	=	instance.LightMapScaleOffset;

			constBufferInstance.SetData( cbDataInstance );

			return true;
		}



		bool SetupSubset ( ref VTSegment segmentInfo, bool transparent )
		{
			var region = segmentInfo.Region;

			if (segmentInfo.Transparent!=transparent) {
				return false;
			}

			cbDataSubset.Rectangle	=	new Vector4( region.X, region.Y, region.Width, region.Height );
			cbDataSubset.Color		=	segmentInfo.AverageColor.ToColor4();
			cbDataSubset.MaxMip		=	segmentInfo.MaxMipLevel;
			
			constBufferSubset.SetData( cbDataSubset );

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		/// <param name="context"></param>
		/// <param name="rw"></param>
		void RenderGeneric ( string eventName, GameTime gameTime, StereoEye stereoEye, SurfaceFlags surfFlags, IRenderContext context, IEnumerable<MeshInstance> instances, InstanceGroup instanceGroup )
		{
			using ( new PixEvent(eventName) ) {

				var transparent		=	context.Transparent;
			
				if ( SetupStage( stereoEye, context, instanceGroup ) ) {

					foreach ( var instance in instances ) {

						if ( SetupInstance( surfFlags, context, instance ) ) {

							device.SetupVertexInput( instance.vb, instance.ib );

							foreach ( var subset in instance.Subsets ) {

								var vt		=	rs.RenderWorld.VirtualTexture;
								var segment	=	vt.GetTextureSegmentInfo( subset.Name );

								if (SetupSubset( ref segment, transparent )) {
									device.DrawIndexed( subset.PrimitiveCount*3, subset.StartPrimitive*3, 0 );
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

			var feedbackBuffer = new VTAddress[ HdrFrame.FeedbackBufferWidth * HdrFrame.FeedbackBufferHeight ];
			frame.FeedbackBufferRB.GetFeedback( feedbackBuffer );
			rs.VTSystem.Update( feedbackBuffer, gameTime );
		}



		internal void RenderForwardSolid ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderWorld rw, InstanceGroup mask )
		{	
			var context		=	new ForwardSolidContext( camera, frame );
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderForwardSolid", gameTime, stereoEye, SurfaceFlags.FORWARD, context, instances, mask );
		}


		internal void RenderForwardTransparent ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderWorld rw, InstanceGroup mask )
		{		
			var context		=	new ForwardTransparentContext( camera, frame );
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderForwardTransparent", gameTime, stereoEye, SurfaceFlags.FORWARD, context, instances, mask );
		}


		internal void RenderZPass ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderWorld rw, InstanceGroup mask )
		{		
			var context	=	new ForwardZPassContext( camera, frame );
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("RenderZPass", gameTime, stereoEye, SurfaceFlags.ZPASS, context, instances, mask );
		}
		

		internal void RenderShadowMap ( ShadowContext shadowContext, RenderWorld rw, InstanceGroup mask )
		{
			var instances	=	rw.Instances.Where( inst => (inst.Group & mask) != 0 );

			RenderGeneric("ShadowMap", null, StereoEye.Mono, SurfaceFlags.SHADOW, shadowContext, instances, mask );
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
