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
		ConstantBuffer	filterCB;

		RenderTarget2D	interleavedDepth;
		RenderTarget2D	occlusionMap;
		RenderTarget2D	temporaryMap;

		RenderTarget2D	depthSliceMap0;
		RenderTarget2D	depthSliceMap1;
		RenderTarget2D	depthSliceMap2;
		RenderTarget2D	depthSliceMap3;

		[ShaderDefine]
		const int BlockSizeX = 32;

		[ShaderDefine]
		const int BlockSizeY = 32;

		[ShaderDefine]
		const int InterleaveBlockSizeX = 16;

		[ShaderDefine]
		const int InterleaveBlockSizeY = 16;

		[ShaderDefine]
		const int BilateralBlockSizeX = 16;

		[ShaderDefine]
		const int BilateralBlockSizeY = 16;

		[ShaderDefine]
		const int PatternSize = 16*16;

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=80)]
		struct HdaoParams {
			public	Vector4	InputSize;

			public	float	CameraTangentX;
			public	float	CameraTangentY;

			public	float   LinDepthScale;
			public	float   LinDepthBias;
			
			public	float	PowerIntensity;
			public	float	LinearIntensity;
			public	float	FadeoutDistance;
			public	float	DiscardDistance;

			public	float	AcceptRadius;
			public	float	RejectRadius;
			public	float	RejectRadiusRcp;
			public	float	Dummy0;

			public	Int2	WriteOffset;

		}

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=16)]
		struct FilterParams {

			public	float   LinDepthScale;
			public	float   LinDepthBias;

			public	float	DepthFactor;
			public	float	NormalFactor;
		}


		enum Flags {	
			INTERLEAVE		=	0x001,
			DEINTERLEAVE	=	0x002,
			HDAO			=	0x004,
			BILATERAL		=	0x008,

			VERTICAL		=	0x010,
			HORIZONTAL		=	0x020,

			LOW				=	0x100,
			MEDIUM			=	0x200,
			HIGH			=	0x400,
			ULTRA			=	0x800,
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
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(HdaoParams) );
			filterCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(FilterParams) );

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

			depthSliceMap0		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.R32F, newWidth/2,	newHeight/2, false, true );
			depthSliceMap1		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.R32F, newWidth/2,	newHeight/2, false, true );
			depthSliceMap2		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.R32F, newWidth/2,	newHeight/2, false, true );
			depthSliceMap3		=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.R32F, newWidth/2,	newHeight/2, false, true );
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
				SafeDispose( ref filterCB	 );

				SafeDispose( ref depthSliceMap0 );
				SafeDispose( ref depthSliceMap1 );
				SafeDispose( ref depthSliceMap2 );
				SafeDispose( ref depthSliceMap3 );
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

			var view		=	camera.GetViewMatrix( stereoEye );
			var projection	=	camera.GetProjectionMatrix( stereoEye );
			var vp			=	device.DisplayBounds;
			

			if (QualityLevel==QualityLevel.None) {
				device.Clear( occlusionMap.Surface, Color4.White );
				return;
			}

			using (new PixEvent("HDAO Render")) {

				using ( new PixEvent( "Interleave" ) ) {

					device.ResetStates();

					device.ComputeShaderResources[0]    =   depthBuffer;
					
					device.SetCSRWTexture( 0, depthSliceMap0.Surface );
					device.SetCSRWTexture( 1, depthSliceMap1.Surface );
					device.SetCSRWTexture( 2, depthSliceMap2.Surface );
					device.SetCSRWTexture( 3, depthSliceMap3.Surface );

					device.PipelineState = factory[(int)Flags.INTERLEAVE];
					
					int tgx = MathUtil.IntDivRoundUp( vp.Width/2,  InterleaveBlockSizeX );
					int tgy = MathUtil.IntDivRoundUp( vp.Height/2, InterleaveBlockSizeY );
					int tgz = 1;
					device.Dispatch( tgx, tgy, tgz );
				}


				using ( new PixEvent( "HDAO Pass" ) ) {

					device.ResetStates();
		
					//
					//	Setup parameters :
					//
					var paramsData              =   new HdaoParams();

					paramsData.InputSize        =   depthSliceMap1.SizeRcpSize;

					paramsData.CameraTangentX   =   camera.CameraTangentX;
					paramsData.CameraTangentY   =   camera.CameraTangentY;

					paramsData.LinDepthBias     =   camera.LinearizeDepthBias;
					paramsData.LinDepthScale    =   camera.LinearizeDepthScale;

					paramsData.PowerIntensity   =   PowerIntensity;
					paramsData.LinearIntensity  =   LinearIntensity;
					paramsData.FadeoutDistance  =   FadeoutDistance;
					paramsData.DiscardDistance  =   DiscardDistance;
					paramsData.AcceptRadius     =   AcceptRadius;
					paramsData.RejectRadius     =   RejectRadius;
					paramsData.RejectRadiusRcp  =   1 / RejectRadius;

					var slices = new[] { depthSliceMap0, depthSliceMap1, depthSliceMap2, depthSliceMap3 };

					Flags flag = Flags.HDAO;

					switch (QualityLevel) {
						case QualityLevel.Low	: flag = Flags.HDAO | Flags.LOW		;  break;
						case QualityLevel.Medium: flag = Flags.HDAO | Flags.MEDIUM	;  break;
						case QualityLevel.High	: flag = Flags.HDAO | Flags.HIGH	;  break;
						case QualityLevel.Ultra	: flag = Flags.HDAO | Flags.ULTRA	;  break;
					}

					for (int i=0; i<4; i++) {

						paramsData.WriteOffset.X		=	i % 2;
						paramsData.WriteOffset.Y		=	i / 2;

						paramsCB.SetData( paramsData );

						device.ComputeShaderConstants[0]    =   paramsCB;
						device.ComputeShaderResources[0]    =   slices[i];

						device.SetCSRWTexture( 0, occlusionMap.Surface );

						device.PipelineState = factory[(int)flag];

						int tgx = MathUtil.IntDivRoundUp( vp.Width/2,  BlockSizeX );
						int tgy = MathUtil.IntDivRoundUp( vp.Height/2, BlockSizeY );
						int tgz = 1;

						device.Dispatch( tgx, tgy, tgz );
					}
				}



				using ( new PixEvent( "Bilateral Filter" ) ) {

					if (!SkipBilateralFilter) {

						var filterData              =   new FilterParams();
						filterData.LinDepthBias     =   camera.LinearizeDepthBias;
						filterData.LinDepthScale    =   camera.LinearizeDepthScale;
						filterData.DepthFactor		=   BilateralDepthFactor;
						filterData.NormalFactor		=   BilateralNormalFactor;
						filterCB.SetData( filterData );
					
						int tgx = MathUtil.IntDivRoundUp( vp.Width,  BilateralBlockSizeX );
						int tgy = MathUtil.IntDivRoundUp( vp.Height, BilateralBlockSizeY );
						int tgz = 1;

						//	HORIZONTAL pass :
						device.ResetStates();

						device.ComputeShaderResources[0]    =   occlusionMap;
						device.ComputeShaderResources[1]    =   depthBuffer;
						device.ComputeShaderConstants[0]    =   filterCB;
						device.SetCSRWTexture( 0, temporaryMap.Surface );

						device.PipelineState = factory[(int)(Flags.BILATERAL|Flags.HORIZONTAL)];
						device.Dispatch( tgx, tgy, tgz );

						//	VERTICAL pass :
						device.ResetStates();

						device.ComputeShaderResources[0]    =   temporaryMap;
						device.ComputeShaderResources[1]    =   depthBuffer;
						device.ComputeShaderConstants[0]    =   filterCB;
						device.SetCSRWTexture( 0, occlusionMap.Surface );

						device.PipelineState = factory[(int)(Flags.BILATERAL|Flags.VERTICAL)];
						device.Dispatch( tgx, tgy, tgz );

					}
				}

			}
		}

	}
}
