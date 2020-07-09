﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics;
using Fusion.Engine.Imaging;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core.Shell;


namespace Fusion.Engine.Graphics 
{
	public enum TonemappingOperator 
	{
		Linear,
		Reinhard,
		Filmic,
	}


	[RequireShader("hdr", true)]
	internal class HdrFilter : RenderComponent 
	{
		static FXConstantBuffer<PARAMS> regParams = new CRegister( 0, "Params" );

		readonly Random rand = new Random();
		

		/// <summary>
		/// Tonemapping operator.
		/// </summary>
		[Config]
		public TonemappingOperator TonemappingOperator { get; set; }

		/// <summary>
		/// Tonemapping operator.
		/// </summary>
		[Config]
		public bool ShowHistogram { get; set; }
		
		/// <summary>
		/// Time to adapt. Default value is 0.5 seconds.
		/// </summary>
		[Config]
		[AEValueRange(0.125f, 4f, 0.125f, 0.125f)]
		public float AdaptationHalfTime { get; set; } = 0.5f;

		/// <summary>
		/// Luminance key value. Default value is 0.18.
		/// </summary>
		[Config]
		[AEValueRange(0.045f, 1, 0.05f, 0.01f)]
		public float KeyValue { get; set; } = 0.18f;
		
		/// <summary>
		/// Bloom gaussian blur sigma. Default is 3.
		/// </summary>
		[Config]
		public float GaussBlurSigma { 
			get { return gaussBlurSigma; }
			set { gaussBlurSigma = MathUtil.Clamp( value, 1, 5 ); }
		}

		float gaussBlurSigma = 3;

		/// <summary>
		/// Amount of bloom. Zero means no bloom.
		/// One means fully bloomed image.
		/// </summary>
		[Config]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float BloomAmount { get; set; } = 0.1f;

		/// <summary>
		/// Amount of dirt. Zero means no bloom.
		/// One means fully bloomed image.
		/// </summary>
		[Config]
		[AEValueRange(0, 1, 1f/32f, 1f/256f)]
		public float DirtAmount { get; set; } = 0.9f;

		/// <summary>
		/// Gets and sets overall image saturation
		/// Default value is 1.
		/// </summary>
		[Config]
		[AEValueRange(0, 1, 1f/32f, 1f/256f)]
		public float Saturation { get; set; } = 1.0f;

		static readonly int		MinLogLuminance = -16;
		static readonly int		MaxLogLuminance =  16;
		static readonly float	MinLinearLuminance	=	(float)Math.Pow( 2, MinLogLuminance );
		static readonly float	MaxLinearLuminance	=	(float)Math.Pow( 2, MaxLogLuminance );
		float minEv = MinLogLuminance;
		float maxEv = MaxLogLuminance;
		float adaptMinEv = MinLogLuminance;
		float adaptMaxEv = MaxLogLuminance;

		[Config]
		public float EVMin { 
			get { return minEv; }
			set { minEv = MathUtil.Clamp( value, MinLogLuminance, MaxLogLuminance ); }
		}

		[Config]
		public float EVMax {
			get { return maxEv; }
			set { maxEv = MathUtil.Clamp( value, MinLogLuminance, MaxLogLuminance ); }
		}


		[Config]
		public float AdaptEVMin { 
			get { return adaptMinEv; }
			set { adaptMinEv = MathUtil.Clamp( value, MinLogLuminance, MaxLogLuminance ); }
		}

		[Config]
		public float AdaptEVMax {
			get { return adaptMaxEv; }
			set { adaptMaxEv = MathUtil.Clamp( value, MinLogLuminance, MaxLogLuminance ); }
		}

		static float ComputeLogLuminance ( float linear )
		{
			return (float)Math.Log( linear, 2 );
		}

		/// <summary>
		/// Dither pattern amount
		/// </summary>
		[Config]
		[AEValueRange(0, 16, 1f, 1f/16f)]
		public float Dithering { get; set; } = 4;

		/// <summary>
		/// Dither pattern amount
		/// </summary>
		[Config]
		[AEValueRange(0, 1, 1f/16f, 1f/256f)]
		public float Vignette { get; set; } = 1;


		Ubershader	shader;
		ConstantBuffer	paramsCB;
		RenderTarget2D	averageLum;
		StateFactory	factory;
		DynamicTexture	whiteTex;
		DiscTexture[]	noiseTex;
		DiscTexture		vignetteTex;
		ByteAddressBuffer histogramBuffer;


		//	float AdaptationRate;          // Offset:    0
		//	float LuminanceLowBound;       // Offset:    4
		//	float LuminanceHighBound;      // Offset:    8
		//	float KeyValue;                // Offset:   12
		//	float BloomAmount;             // Offset:   16
		[StructLayout(LayoutKind.Sequential, Size=128)]
		[ShaderStructure]
		struct PARAMS {
			public	float	AdaptationRate;
			public	float 	LuminanceLowBound;
			public	float	LuminanceHighBound;
			public	float	KeyValue;
			public	float	BloomAmount;
			public	float	DirtMaskLerpFactor;
			public	float	DirtAmount;
			public	float	Saturation;
			public	float	DitherAmount;
			public	uint	Width;
			public	uint	Height;
			public	float	EVMin;
			public	float	EVMax;
			public	float	EVRange;
			public	float	EVRangeInverse;
			public	float	AdaptEVMin;
			public	float	AdaptEVMax;
			public	uint	NoiseX;
			public	uint	NoiseY;
			public	float	VignetteAmount;
		}


		[ShaderDefine]
		const int NoiseSizeX		=	64;
		[ShaderDefine]
		const int NoiseSizeY		=	64;

		[ShaderDefine]
		const int BlockSizeX		=	16;
		[ShaderDefine]
		const int BlockSizeY		=	16;
		[ShaderDefine]
		const int NumHistogramBins	=	256;
		[ShaderDefine]
		const float Epsilon			=	1f / 4096f;


		enum Flags {	
			TONEMAPPING			=	0x001,
			MEASURE_ADAPT		=	0x002,
			LINEAR				=	0x004, 
			REINHARD			=	0x008,
			FILMIC				=	0x010,
			SHOW_HISTOGRAM		=	0x020,

			COMPOSITION			=	0x100,
			COMPUTE_HISTOGRAM	=	0x200,
			AVERAGE_HISTOGRAM	=	0x400,
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public HdrFilter ( RenderSystem rs ) : base(rs)
		{
		}



		/// <summary>
		/// /
		/// </summary>
		public override void Initialize ()
		{
			averageLum	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F, 256,256, true, true );
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(PARAMS) );
			whiteTex	=	new DynamicTexture( Game.RenderSystem, 4,4, typeof(Color), false, false);
			whiteTex.SetData( Enumerable.Range(0,16).Select( i=> Color.White ).ToArray() );

			histogramBuffer	=	new ByteAddressBuffer( Game.GraphicsDevice, 256 );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("hdr");
			factory		=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty, BlendState.Opaque, RasterizerState.CullNone, DepthStencilState.None );

			vignetteTex	=	Game.Content.Load<DiscTexture>(@"noise\vignette");

			noiseTex	=	new DiscTexture[8];
			for (int i=0; i<8; i++) {
				noiseTex[i]	=	Game.Content.Load<DiscTexture>(@"noise\anim\LDR_LLL1_" + i.ToString());
			}
		}



		int[,] GenerateBayerMatrixRecursive ( int[,] matrix )
		{
			int[,] newMatrix;

			Random rand = new Random();

			if (matrix==null) {
				return new int [2,2] { {0,2}, {3,1} };
			}

			var w = matrix.GetLength(0);
			var h = matrix.GetLength(0);

			newMatrix	=	new int [w*2,h*2];

			for ( int i=0; i<w; i++) {
				for ( int j=0; j<h; j++) {
					newMatrix[ i+0,j+0 ] = matrix[i,j] * 4 + 0;
					newMatrix[ i+w,j+0 ] = matrix[i,j] * 4 + 2;
					newMatrix[ i+0,j+h ] = matrix[i,j] * 4 + 3;
					newMatrix[ i+w,j+h ] = matrix[i,j] * 4 + 1;
				}
			}

			return newMatrix;
		}



		DynamicTexture GenerateBayerMatrix ( int order )
		{
			int[,] matrix = null;

			Random rand = new Random();

			for ( int i=0; i<6; i++ ) {
				matrix = GenerateBayerMatrixRecursive( matrix );
			}

			var w = matrix.GetLength(0);
			var h = matrix.GetLength(0);

			var colors = new Color[w*h];

			for ( int i=0; i<w; i++) {
				for ( int j=0; j<h; j++) {
					colors[i+j*w] = new Color( (byte)(matrix[i,j]/16) );
				}
			}

			var tex = new DynamicTexture( rs, w, h, typeof(Color) );

			tex.SetData( colors );

			return tex;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref averageLum	 );
				SafeDispose( ref paramsCB	 );
				SafeDispose( ref whiteTex );
				SafeDispose( ref noiseTex );
				SafeDispose( ref histogramBuffer );
			}

			base.Dispose( disposing );
		}


							
		
	 
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hdrFrame"></param>
		public void ComposeHdrImage ( HdrFrame hdrFrame )
		{
			var device	=	Game.GraphicsDevice;
			var filter	=	Game.RenderSystem.Filter;

			device.ResetStates();

			using ( new PixEvent("HDR Composition") ) {
				
				hdrFrame.SwapHdrTargets();

				//
				//	Tonemap and compose :
				//
				device.SetTargets( null, hdrFrame.HdrTarget );
				device.SetViewport( hdrFrame.HdrTarget.Bounds );
				device.SetScissorRect( hdrFrame.HdrTarget.Bounds );

				device.GfxSamplers[0]	=	SamplerState.LinearClamp;
				device.GfxSamplers[1]	=	SamplerState.AnisotropicClamp;

				device.GfxResources[0]	=	hdrFrame.HdrSource;
				device.GfxResources[1]	=	hdrFrame.HdrBufferGlass;
				device.GfxResources[2]	=	hdrFrame.DistortionGlass;
				device.GfxResources[3]	=	hdrFrame.DistortionBuffer;
				device.GfxResources[4]	=	hdrFrame.SoftParticlesFront;
				device.GfxResources[5]	=	hdrFrame.SoftParticlesBack;
				device.GfxResources[6]	=	hdrFrame.Bloom0;
				device.GfxResources[7]	=	hdrFrame.ParticleVelocity;

				device.PipelineState			=	factory[ (int)(Flags.COMPOSITION) ];
				
				device.Draw( 3, 0 );
			
				device.ResetStates();
			}
		}



		uint frameCounter	=	0;


		/// <summary>
		/// Performs luminance measurement, tonemapping, applies bloom.
		/// </summary>
		/// <param name="target">LDR target.</param>
		/// <param name="hdrImage">HDR source image.</param>
		public void TonemapHdrImage ( GameTime gameTime, HdrFrame hdrFrame, Camera camera )
		{
			frameCounter++;

			var device	=	Game.GraphicsDevice;
			var filter	=	Game.RenderSystem.Filter;
			var blur	=	Game.RenderSystem.Blur;

			int imageWidth	=	hdrFrame.HdrTarget.Width;
			int imageHeight	=	hdrFrame.HdrTarget.Height;

			using ( new PixEvent("HDR Postprocessing") ) {

				//
				//	Rough downsampling of source HDR-image :
				//
				filter.StretchRect( averageLum.Surface, hdrFrame.HdrTarget, SamplerState.PointClamp );
				averageLum.BuildMipmaps();

				//
				//	Make bloom :
				//
				filter.StretchRect( hdrFrame.Bloom0.Surface, hdrFrame.HdrTarget, SamplerState.LinearClamp );
				hdrFrame.Bloom0.BuildMipmaps();

				#if false
				#warning BLUR SCALING ERROR
				blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 0 );
				blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 1 );
				blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 2 );
				blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 3 );
				blur.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, 4 );
				device.ResetStates();
				#else
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, GaussBlurSigma, 0 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, GaussBlurSigma, 1 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, GaussBlurSigma, 2 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, GaussBlurSigma, 3 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, GaussBlurSigma, 4 );
				#endif

				//
				//	Setup parameters :
				//
				var paramsData	=	new PARAMS();
				paramsData.AdaptationRate		=	1 - (float)Math.Pow( 0.5f, gameTime.ElapsedSec / AdaptationHalfTime );
				paramsData.KeyValue				=	KeyValue;
				paramsData.BloomAmount			=	BloomAmount;
				paramsData.DirtMaskLerpFactor	=	0;
				paramsData.DirtAmount			=	0;
				paramsData.Saturation			=	Saturation;
				paramsData.DitherAmount			=	Dithering;
				paramsData.Width				=	(uint)imageWidth;
				paramsData.Height				=	(uint)imageHeight;
				paramsData.EVMin				=	EVMin;
				paramsData.EVMax				=	EVMax;
				paramsData.EVRange				=	paramsData.EVMax - paramsData.EVMin;
				paramsData.EVRangeInverse		=	1.0f / paramsData.EVRange;
				paramsData.AdaptEVMin			=	AdaptEVMin;
				paramsData.AdaptEVMax			=	AdaptEVMax;
				paramsData.NoiseX				=	0;
				paramsData.NoiseY				=	0;
				paramsData.VignetteAmount		=	Vignette;

				paramsCB.SetData( ref paramsData );
				device.GfxConstants[0]		=	paramsCB;
				device.ComputeConstants[0]	=	paramsCB;

				//
				//	Measure and adapt :
				//
				device.Clear( histogramBuffer, Int4.Zero );
				device.ComputeResources[0]	=	hdrFrame.HdrTarget;
				device.SetComputeUnorderedAccess( 0, histogramBuffer.UnorderedAccess );

				device.PipelineState		=	factory[ (int)(Flags.COMPUTE_HISTOGRAM) ];
				device.Dispatch( new Int2(imageWidth, imageHeight), new Int2(BlockSizeX, BlockSizeY) ); 

				device.SetComputeUnorderedAccess( 0, histogramBuffer.UnorderedAccess );
				device.SetComputeUnorderedAccess( 1, hdrFrame.MeasuredNew.Surface.UnorderedAccess );

				//--------------------

				device.PipelineState		=	factory[ (int)(Flags.AVERAGE_HISTOGRAM) ];
				device.Dispatch( 1,1,1 ); 

				device.ComputeResources[0]	=	null;
				device.SetComputeUnorderedAccess( 0, null );
				device.SetComputeUnorderedAccess( 1, null );

				//
				//	Tonemap and compose :
				//
				device.SetTargets( null, hdrFrame.FinalColor );
				device.SetViewport( hdrFrame.FinalColor.Bounds );
				device.SetScissorRect( hdrFrame.FinalColor.Bounds );

				device.GfxResources[0]	=	hdrFrame.HdrTarget;// averageLum;
				device.GfxResources[1]	=	hdrFrame.MeasuredNew;// averageLum;
				device.GfxResources[2]	=	hdrFrame.Bloom0;// averageLum;
				device.GfxResources[3]	=	null;
				device.GfxResources[4]	=	null;
				device.GfxResources[5]	=	noiseTex[frameCounter % 8].Srv;
				device.GfxResources[6]	=	vignetteTex.Srv;
				device.GfxResources[9]	=	histogramBuffer;
				device.GfxResources[10]	=	hdrFrame.DepthBuffer;
				device.GfxSamplers[0]	=	SamplerState.LinearClamp;

				Flags op = Flags.LINEAR;
				if (TonemappingOperator==TonemappingOperator.Filmic)   { op = Flags.FILMIC;   }
				if (TonemappingOperator==TonemappingOperator.Linear)   { op = Flags.LINEAR;	 }
				if (TonemappingOperator==TonemappingOperator.Reinhard) { op = Flags.REINHARD; }

				if (ShowHistogram) {
					op |= Flags.SHOW_HISTOGRAM;
				}

				device.PipelineState		=	factory[ (int)(Flags.TONEMAPPING|op) ];
				
				device.Draw( 3, 0 );
			
				device.ResetStates();


				//	swap luminanice buffers :
				///Misc.Swap( ref hdrFrame.MeasuredNew, ref hdrFrame.MeasuredOld );
			}
		}
	}
}
