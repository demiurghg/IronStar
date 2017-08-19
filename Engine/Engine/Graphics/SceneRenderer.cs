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

namespace Fusion.Engine.Graphics {

	internal partial class SceneRenderer : RenderComponent {

		internal const int MaxBones = 128;

		ConstantBuffer	constBufferStage;
		ConstantBuffer	constBufferInstance;
		ConstantBuffer	constBufferBones;
		ConstantBuffer	constBufferSubset;
		Ubershader		surfaceShader;
		StateFactory	factory;

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
		public bool SetupStage ( StereoEye stereoEye, Camera camera, HdrFrame hdrFrame, ShadowContext shadowContext )
		{
			device.ResetStates();

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
			cbDataStage.ShadowGradientBiasX		=	rs.ShadowGradientBiasX;
			cbDataStage.ShadowGradientBiasY		=	rs.ShadowGradientBiasY;

			cbDataStage.GradientScaler			=	VTConfig.PageSize * VTConfig.VirtualPageCount / (float)rs.VTSystem.PhysicalPages0.Width;
			cbDataStage.DebugGradientScale		=	rs.VTSystem.DebugGradientScale;

			cbDataStage.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
			cbDataStage.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;


			//	setup stage constants :
			if (camera!=null) {

				var width	=	hdrFrame.HdrBuffer.Width;
				var height	=	hdrFrame.HdrBuffer.Height;

				cbDataStage.View			=	camera.GetViewMatrix( stereoEye );
				cbDataStage.Projection		=	camera.GetProjectionMatrix( stereoEye );
				cbDataStage.ViewPos			=	camera.GetCameraPosition4( stereoEye );
				cbDataStage.Ambient			=	rs.RenderWorld.LightSet.AmbientLevel;
				cbDataStage.ViewBounds		=	new Vector4( width, height, width, height );
				cbDataStage.VTPageScaleRCP	=	rs.VTSystem.PageScaleRCP;

				cbDataStage.GradientToNormal	=	camera.GetViewMatrix( stereoEye );
			}

			if (shadowContext!=null) {

				var width	=	shadowContext.ShadowViewport.Width;
				var height	=	shadowContext.ShadowViewport.Height;

				cbDataStage.View			=	shadowContext.ShadowView;
				cbDataStage.Projection		=	shadowContext.ShadowProjection;
				cbDataStage.ViewPos			=	Vector4.Zero;
				cbDataStage.Ambient			=	Color4.Zero;
				cbDataStage.ViewBounds		=	new Vector4( width, height, width, height );
				cbDataStage.VTPageScaleRCP	=	rs.VTSystem.PageScaleRCP;
				cbDataStage.BiasSlopeFar	=	new Vector4( shadowContext.DepthBias, shadowContext.SlopeBias, shadowContext.FarDistance, 0 );
			}

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
			device.PixelShaderResources[4]	= rs.LightManager.LightGrid.GridTexture;
			device.PixelShaderResources[5]	= rs.LightManager.LightGrid.IndexDataGpu;
			device.PixelShaderResources[6]	= rs.LightManager.LightGrid.LightDataGpu;
			device.PixelShaderResources[7]	= rs.LightManager.LightGrid.DecalDataGpu;
			device.PixelShaderResources[11]	= rs.SsaoFilter.OcclusionMap;
			
			if (shadowContext==null) {	  // because these maps are used as render targets for shadows
				device.PixelShaderResources[8]	= rs.RenderWorld.LightSet?.DecalAtlas?.Texture?.Srv;
				device.PixelShaderResources[9]	= rs.LightManager.ShadowMap.ColorBuffer;
				device.PixelShaderResources[10]	= rs.LightManager.ShadowMap.ParticleShadow;
			}


			//	setup samplers :
			var shadowSampler	=	rs.UsePointShadowSampling ? SamplerState.ShadowSamplerPoint : SamplerState.ShadowSampler;

			device.PixelShaderSamplers[0]	= rs.VTSystem.UseAnisotropic ? SamplerState.VTAnisotropic : SamplerState.VTTrilinear;
			device.PixelShaderSamplers[1]	= SamplerState.PointClamp;
			device.PixelShaderSamplers[2]	= SamplerState.AnisotropicClamp;
			device.PixelShaderSamplers[3]	= SamplerState.LinearClamp4Mips;
			device.PixelShaderSamplers[4]	= shadowSampler;
			device.PixelShaderSamplers[5]	= SamplerState.LinearClamp;

			if (surfaceShader==null || rs.SkipSceneRendering) {
				return false;
			} else {
				return true;
			}
		}



		bool SetupInstance ( SurfaceFlags stageFlag, MeshInstance instance )
		{
			#warning New pipeline state semantic: bool StateFactory.TrySetPipelineState( flags )

			bool aniso	=	rs.VTSystem.UseAnisotropic ;

			int flag = (int)( stageFlag | SurfaceFlags.RIGID );

			if (aniso && stageFlag==SurfaceFlags.FORWARD) {
				flag |= (int)SurfaceFlags.ANISOTROPIC;
			}

			device.PipelineState	=	factory[ flag ];

			cbDataInstance.AssignmentGroup	=	instance.Static ? 0 : 1;
			cbDataInstance.Color	=	instance.Color;
			cbDataInstance.World	=	instance.World;

			constBufferInstance.SetData( cbDataInstance );

			return true;
		}



		bool SetupSubset ( ref RectangleF rect )
		{
			cbDataSubset.Rectangle	=	new Vector4( rect.X, rect.Y, rect.Width, rect.Height );
			constBufferSubset.SetData( cbDataSubset );
			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="depthBuffer"></param>
		/// <param name="hdrTarget"></param>
		/// <param name="diffuse"></param>
		/// <param name="specular"></param>
		/// <param name="normals"></param>
		internal void RenderForward ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderWorld rw, bool staticOnly )
		{		
			using ( new PixEvent("RenderForward") ) {

				var instances			=	rw.Instances;

				if ( SetupStage( stereoEye, camera, frame, null ) ) {

					var hdr			=	frame.HdrBuffer.Surface;
					var depth		=	frame.DepthBuffer.Surface;
					var feedback	=	frame.FeedbackBuffer.Surface;

					device.SetTargets( depth, hdr, feedback );


					foreach ( var instance in instances ) {

						if ( SetupInstance( SurfaceFlags.FORWARD, instance ) ) {

							device.SetupVertexInput( instance.vb, instance.ib );

							foreach ( var subset in instance.Subsets ) {

								var vt		=	rw.VirtualTexture;
								var rect	=	vt.GetTexturePosition( subset.Name );

								if (SetupSubset( ref rect )) {
									device.DrawIndexed( subset.PrimitiveCount*3, subset.StartPrimitive*3, 0 );
								}
							}
						}
					}
				}

				
				//
				//	downsample feedback buffer and readback it to virtual texture :
				//
				rs.Filter.StretchRect( frame.FeedbackBufferRB.Surface, frame.FeedbackBuffer, SamplerState.PointClamp );

				var feedbackBuffer = new VTAddress[ HdrFrame.FeedbackBufferWidth * HdrFrame.FeedbackBufferHeight ];
				frame.FeedbackBufferRB.GetFeedback( feedbackBuffer );
				rs.VTSystem.Update( feedbackBuffer, gameTime );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="depthBuffer"></param>
		/// <param name="hdrTarget"></param>
		/// <param name="diffuse"></param>
		/// <param name="specular"></param>
		/// <param name="normals"></param>
		internal void RenderZPass ( GameTime gameTime, StereoEye stereoEye, Camera camera, HdrFrame frame, RenderWorld rw, bool staticOnly )
		{		
			using ( new PixEvent("RenderZPass") ) {

				var instances			=	rw.Instances;

				var view				=	camera.GetViewMatrix( stereoEye );
				var projection			=	camera.GetProjectionMatrix( stereoEye );
				var viewPosition		=	camera.GetCameraPosition( stereoEye );
				var weaponProjection	=	rw.WeaponCamera.GetProjectionMatrix( stereoEye );
				var vpWidth				=	frame.HdrBuffer.Width;
				var vpHeight			=	frame.HdrBuffer.Height;

				if ( SetupStage( stereoEye, camera, frame, null ) ) {

					var hdr			=	frame.HdrBuffer.Surface;
					var depth		=	frame.DepthBuffer.Surface;
					var feedback	=	frame.FeedbackBuffer.Surface;
					var normals		=	frame.Normals.Surface;

					#warning remove hdr and feedback targets
					device.SetTargets( depth, normals );

					foreach ( var instance in instances ) {

						if ( SetupInstance( SurfaceFlags.ZPASS, instance ) ) {

							device.SetupVertexInput( instance.vb, instance.ib );

							foreach ( var subset in instance.Subsets ) {

								var vt		=	rw.VirtualTexture;
								var rect	=	vt.GetTexturePosition( subset.Name );

								if (SetupSubset( ref rect )) {
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
		/// <param name="context"></param>
		internal void RenderShadowMapCascade ( ShadowContext shadowRenderCtxt, IEnumerable<MeshInstance> instances )
		{
			using ( new PixEvent("ShadowMap") ) {

				if ( SetupStage( StereoEye.Mono, null, null, shadowRenderCtxt ) ) {

					device.SetTargets( shadowRenderCtxt.DepthBuffer, shadowRenderCtxt.ColorBuffer );
					device.SetViewport( shadowRenderCtxt.ShadowViewport );
				
									//#warning INSTANSING!
					foreach ( var instance in instances ) {

						if (SetupInstance(SurfaceFlags.SHADOW, instance)) {

							device.SetupVertexInput( instance.vb, instance.ib );
							device.DrawIndexed( instance.indexCount, 0, 0 );
						}
					}
				}
			}
		}
	}
}
