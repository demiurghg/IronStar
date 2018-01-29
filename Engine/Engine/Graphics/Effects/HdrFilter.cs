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


namespace Fusion.Engine.Graphics {
	[RequireShader("hdr")]
	internal class HdrFilter : RenderComponent {


		Ubershader	shader;
		ConstantBuffer	paramsCB;
		RenderTarget2D	averageLum;
		StateFactory	factory;
		DynamicTexture	whiteTex;
		DynamicTexture	noiseTex;


		//	float AdaptationRate;          // Offset:    0
		//	float LuminanceLowBound;       // Offset:    4
		//	float LuminanceHighBound;      // Offset:    8
		//	float KeyValue;                // Offset:   12
		//	float BloomAmount;             // Offset:   16
		[StructLayout(LayoutKind.Explicit, Size=64)]
		struct Params {
			[FieldOffset( 0)]	public	float	AdaptationRate;
			[FieldOffset( 4)]	public	float 	LuminanceLowBound;
			[FieldOffset( 8)]	public	float	LuminanceHighBound;
			[FieldOffset(12)]	public	float	KeyValue;
			[FieldOffset(16)]	public	float	BloomAmount;
			[FieldOffset(20)]	public	float	DirtMaskLerpFactor;
			[FieldOffset(24)]	public	float	DirtAmount;
			[FieldOffset(28)]	public	float	Saturation;
			[FieldOffset(32)]	public	float	MaximumOutputValue;
			[FieldOffset(36)]	public	float	MinimumOutputValue;
			[FieldOffset(40)]	public	float	DitherAmount;
		}


		enum Flags {	
			TONEMAPPING		=	0x001,
			MEASURE_ADAPT	=	0x002,
			LINEAR			=	0x004, 
			REINHARD		=	0x008,
			FILMIC			=	0x010,
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
			averageLum	=	new RenderTarget2D( Game.GraphicsDevice, ColorFormat.Rgba16F, 256,256, true, false );
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(Params) );
			whiteTex	=	new DynamicTexture( Game.RenderSystem, 4,4, typeof(Color), false, false);
			whiteTex.SetData( Enumerable.Range(0,16).Select( i=> Color.White ).ToArray() );

			LoadContent();

			noiseTex	=	GenerateBayerMatrix(8);

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("hdr");
			//noiseTex	=	Game.Content.Load<DiscTexture>(@"noise\hdrDitherNoise");
			//noiseTex	=	Game.Content.Load<DiscTexture>(@"noise\bayerMatrix8x8");

			factory		=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty, BlendState.Opaque, RasterizerState.CullNone, DepthStencilState.None );
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
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// Performs luminance measurement, tonemapping, applies bloom.
		/// </summary>
		/// <param name="target">LDR target.</param>
		/// <param name="hdrImage">HDR source image.</param>
		public void Render ( GameTime gameTime, HdrSettings settings, HdrFrame hdrFrame )
		{
			var device	=	Game.GraphicsDevice;
			var filter	=	Game.RenderSystem.Filter;

			using ( new PixEvent("HDR Postprocessing") ) {

				//
				//	Rough downsampling of source HDR-image :
				//
				filter.StretchRect( averageLum.Surface, hdrFrame.HdrBuffer, SamplerState.PointClamp );
				averageLum.BuildMipmaps();

				//
				//	Make bloom :
				//
				filter.StretchRect( hdrFrame.Bloom0.Surface, hdrFrame.HdrBuffer, SamplerState.LinearClamp );
				hdrFrame.Bloom0.BuildMipmaps();

				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, settings.GaussBlurSigma, 0 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, settings.GaussBlurSigma, 1 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, settings.GaussBlurSigma, 2 );
				filter.GaussBlur( hdrFrame.Bloom0, hdrFrame.Bloom1, settings.GaussBlurSigma, 3 );

				//
				//	Setup parameters :
				//
				var paramsData	=	new Params();
				paramsData.AdaptationRate		=	1 - (float)Math.Pow( 0.5f, gameTime.ElapsedSec / settings.AdaptationHalfLife );
				paramsData.LuminanceLowBound	=	settings.LuminanceLowBound;
				paramsData.LuminanceHighBound	=	settings.LuminanceHighBound;
				paramsData.KeyValue				=	settings.KeyValue;
				paramsData.BloomAmount			=	settings.BloomAmount;
				paramsData.DirtMaskLerpFactor	=	settings.DirtMaskLerpFactor;
				paramsData.DirtAmount			=	settings.DirtAmount;
				paramsData.Saturation			=	settings.Saturation;
				paramsData.MaximumOutputValue	=	settings.MaximumOutputValue;
				paramsData.MinimumOutputValue	=	settings.MinimumOutputValue;
				paramsData.DitherAmount			=	settings.DitherAmount;

				paramsCB.SetData( paramsData );
				device.PixelShaderConstants[0]	=	paramsCB;

				//
				//	Measure and adapt :
				//
				device.SetTargets( null, hdrFrame.MeasuredNew );

				device.PixelShaderResources[0]	=	averageLum;
				device.PixelShaderResources[1]	=	hdrFrame.MeasuredOld;

				device.PipelineState		=	factory[ (int)(Flags.MEASURE_ADAPT) ];
				
				device.Draw( 3, 0 );


				//
				//	Tonemap and compose :
				//
				device.SetTargets( null, hdrFrame.FinalColor );

				device.PixelShaderResources[0]	=	hdrFrame.HdrBuffer;// averageLum;
				device.PixelShaderResources[1]	=	hdrFrame.MeasuredNew;// averageLum;
				device.PixelShaderResources[2]	=	hdrFrame.Bloom0;// averageLum;
				device.PixelShaderResources[3]	=	settings.DirtMask1==null ? whiteTex.Srv : settings.DirtMask1.Srv;
				device.PixelShaderResources[4]	=	settings.DirtMask2==null ? whiteTex.Srv : settings.DirtMask2.Srv;
				device.PixelShaderResources[5]	=	noiseTex.Srv;
				device.PixelShaderResources[6]	=	hdrFrame.DistortionBuffer;
				device.PixelShaderSamplers[0]	=	SamplerState.LinearClamp;

				Flags op = Flags.LINEAR;
				if (settings.TonemappingOperator==TonemappingOperator.Filmic)   { op = Flags.FILMIC;   }
				if (settings.TonemappingOperator==TonemappingOperator.Linear)   { op = Flags.LINEAR;	 }
				if (settings.TonemappingOperator==TonemappingOperator.Reinhard) { op = Flags.REINHARD; }

				device.PipelineState		=	factory[ (int)(Flags.TONEMAPPING|op) ];
				
				device.Draw( 3, 0 );
			
				device.ResetStates();


				//	swap luminanice buffers :
				Misc.Swap( ref hdrFrame.MeasuredNew, ref hdrFrame.MeasuredOld );
			}
		}
	}
}
