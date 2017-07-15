using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;

#pragma warning disable 649


namespace Fusion.Engine.Graphics {
	/// <summary>
	/// 
	/// TODO:
	///		1. [DONE] Variable depth-dependent sampling radius.
	///		2. [DONE] Bleeding edge due to upsampling. Solved via half-pixel offset.
	///		3. Sharpenss as parameter (now its hardcoded in shader).
	///		4. Performance measurement.
	///		5. Normals reconstruction from depth (optional?).
	///		6. Sample count/quality configuration.
	///		8. Far-plane flickering.
	/// 
	/// </summary>
	[RequireShader("hdao", true)]
	internal partial class SsaoFilter : RenderComponent {

		public ShaderResource	OcclusionMap { 
			get {
				return occlusionMap;
			}
		}

		Ubershader		shader;
		StateFactory	factory;

		ConstantBuffer	paramsCB;

		RenderTarget2D	interleavedDepth;
		RenderTarget2D	occlusionMap;
		RenderTarget2D	temporaryMap;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=512)]
		struct Params {
			public	Matrix	ProjMatrix;
			public	Matrix	View;
			public	Matrix	ViewProj;
			public	Matrix	InvViewProj;
			public	Matrix	InvProj;
			
			public	float	PowerIntensity;
			public	float	LinearIntensity;
			public	float	FadeoutDistance;
			public	float	DiscardDistance;
			public	float	AcceptRadius;
			public	float	RejectRadius;

		}


		enum Flags {	
			INTERLEAVE		=	0x001,
			DEINTERLEAVE	=	0x002,
			HDAO			=	0x004,
			BILATERAL_X		=	0x008,
			BILATERAL_Y		=	0x010,
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public SsaoFilter ( RenderSystem rs ) : base(rs)
		{
		}


		/// <summary>
		/// /
		/// </summary>
		public override void Initialize ()
		{
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(Params) );

			CreateTargets();
			LoadContent();

			Game.RenderSystem.DisplayBoundsChanged += (s,e) => CreateTargets();
			Game.Reloading += (s,e) => LoadContent();
		}


		/// <summary>
		/// 
		/// </summary>
		void CreateTargets ()
		{
			var disp	=	Game.GraphicsDevice.DisplayBounds;

			var newWidth	=	Math.Max(64, disp.Width);
			var newHeight	=	Math.Max(64, disp.Height);

			SafeDispose( ref interleavedDepth );
			SafeDispose( ref occlusionMap );
			SafeDispose( ref temporaryMap );

			interleavedDepth	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.R32F,  newWidth,	newHeight,	 false, true );
			occlusionMap		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba8, newWidth,	newHeight,	 false, true );
			temporaryMap		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba8, newWidth,	newHeight,	 false, true );
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			SafeDispose( ref factory );
			shader	=	Game.Content.Load<Ubershader>("hdao");
			factory	=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty, BlendState.Opaque, RasterizerState.CullNone, DepthStencilState.None ); 
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref factory );
				SafeDispose( ref interleavedDepth );
				SafeDispose( ref occlusionMap );
				SafeDispose( ref temporaryMap );
				SafeDispose( ref paramsCB	 );
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Performs luminance measurement, tonemapping, applies bloom.
		/// </summary>
		/// <param name="target">LDR target.</param>
		/// <param name="hdrImage">HDR source image.</param>
		public void Render ( StereoEye stereoEye, Camera camera, ShaderResource depthBuffer, ShaderResource wsNormals )
		{
			var device	=	Game.GraphicsDevice;
			var filter	=	Game.RenderSystem.Filter;

			device.ResetStates();

			var view		=	camera.GetViewMatrix( stereoEye );
			var projection	=	camera.GetProjectionMatrix( stereoEye );
			var vp			=	device.DisplayBounds;
			

			if (!Enabled) {
				device.Clear( occlusionMap.Surface, Color4.White );
				return;
			}

			using (new PixEvent("HDAO Render")) {

				using (new PixEvent("HDAO Pass")) {

					//
					//	Setup parameters :
					//
					var paramsData				=	new Params();

					paramsData.ProjMatrix		=	projection;
					paramsData.View				=	view;
					paramsData.ViewProj			=	view * projection;
					paramsData.InvViewProj		=	Matrix.Invert( view * projection );
					paramsData.InvProj			=	Matrix.Invert(projection);

					paramsData.PowerIntensity	=	PowerIntensity	;
					paramsData.LinearIntensity	=	LinearIntensity	;
					paramsData.FadeoutDistance	=	FadeoutDistance	;
					paramsData.DiscardDistance	=	DiscardDistance	;
					paramsData.AcceptRadius		=	AcceptRadius	;
					paramsData.RejectRadius		=	RejectRadius	;


					paramsCB.SetData( paramsData );

					device.ComputeShaderConstants[0]	=	paramsCB;
					device.ComputeShaderResources[0]	=	depthBuffer;

					device.SetCSRWTexture( 0, occlusionMap.Surface );

					device.PipelineState = factory[ (int)Flags.HDAO ];
			
					int tgx = MathUtil.IntDivRoundUp( vp.Width, 16 );
					int tgy = MathUtil.IntDivRoundUp( vp.Height, 16 );
					int tgz = 1;

					device.Dispatch( tgx, tgy, tgz );
			
					device.ResetStates();
				}

				/*using (new PixEvent("Bilateral Filter")) {
					if (BlurSigma!=0) {
						filter.GaussBlurBilateral( occlusionMapHalf, occlusionMapFull, temporary, depthBuffer, wsNormals, BlurSigma, Sharpness, 0 );
					} else {
						filter.StretchRect( occlusionMapFull.Surface, occlusionMapHalf );
					}
				}*/
			}
		}

	}
}
