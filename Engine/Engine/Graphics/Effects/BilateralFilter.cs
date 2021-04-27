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
using Fusion.Engine.Graphics.Ubershaders;


namespace Fusion.Engine.Graphics
{
	/// <summary>
	/// Bilateral filter
	/// </summary>
	[RequireShader("bilateral", true)]
	internal class BilateralFilter : RenderComponent 
	{
		static FXConstantBuffer<FilterParams>		regParams	=	new CRegister( 0, "filterParams" );
		static FXConstantBuffer<GpuData.CAMERA>		regCamera	=	new CRegister( 1, "Camera" );

		[Flags]
		enum Flags : int {
			DOUBLE_PASS		=	0x0001,
			SINGLE_PASS		=	0x0002,
			VERTICAL		=	0x0004,
			HORIZONTAL		=	0x0008,
			MASK_DEPTH		=	0x0010,
			MASK_ALPHA		=	0x0020,
			LUMA_ONLY		=	0x0040,
		}


		[ShaderDefine]
		const int BlockSize16 = 16;

		[ShaderDefine]
		const int BlockSize8 = 8;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct FilterParams 
		{
			public	Color4	LumaVector;
			public	UInt2	SourceXY;
			public	UInt2	TargetXY;
			public	float	MaskFactor;
			public	float	ColorFactor;
			public	float	GaussFalloff;
		}

		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbuffer;
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		public BilateralFilter( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			LoadContent();

			cbuffer	=	new ConstantBuffer( device, typeof(FilterParams) );

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>( "bilateral" );
			factory	=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty, BlendState.Opaque, RasterizerState.CullNone, DepthStencilState.None ); 
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				SafeDispose( ref cbuffer );
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Bilateral filter
		 * 
		-----------------------------------------------------------------------------------------------*/

		void BilateralPass ( Flags flags, RenderTarget2D target, Rectangle targetRect, Camera camera, ShaderResource source, Rectangle sourceRect, ShaderResource mask, Rectangle maskRect, float colorFactor, float maskFactor, float falloff, Color4 lumaVector )
		{
			device.ResetStates();

			if (target==null) {
				throw new ArgumentNullException("target");
			}
			if (source==null) {
				throw new ArgumentNullException("temp");
			}
			if (mask==null) {
				throw new ArgumentNullException("mask");
			}

			targetRect	=	Rectangle.Intersect( targetRect, new Rectangle(0,0, target.Width, target.Height) );
			sourceRect	=	Rectangle.Intersect( sourceRect, new Rectangle(0,0, source.Width, source.Height) );

			int width	=	Math.Min( targetRect.Width, sourceRect.Width );
			int height	=	Math.Min( targetRect.Height, sourceRect.Height );

			var filterData          =   new FilterParams();
			filterData.TargetXY		=	new UInt2( (uint)targetRect.X, (uint)targetRect.Y );
			filterData.SourceXY		=	new UInt2( (uint)sourceRect.X, (uint)sourceRect.Y );
			filterData.LumaVector	=	lumaVector;
			filterData.MaskFactor	=   maskFactor;
			filterData.GaussFalloff	=	falloff;
			filterData.ColorFactor	=   colorFactor;
			cbuffer.SetData( ref filterData );

			int blockSize	=	flags.HasFlag( Flags.DOUBLE_PASS ) ? BlockSize16 : BlockSize8;
					
			int tgx = MathUtil.IntDivRoundUp( width,  blockSize );
			int tgy = MathUtil.IntDivRoundUp( height, blockSize );
			int tgz = 1;

			//	HORIZONTAL pass :
			device.ComputeResources[0]    =   source;
			device.ComputeResources[1]    =   mask;

			device.ComputeConstants[ regParams	]	=	cbuffer;
			device.ComputeConstants[ regCamera	]	=	camera?.CameraData;

			device.SetComputeUnorderedAccess( 0, target.Surface.UnorderedAccess );

			device.PipelineState = factory[(int)flags];
			device.Dispatch( tgx, tgy, tgz );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void FilterSSAOByDepth ( Camera camera, RenderTarget2D target, RenderTarget2D temp, ShaderResource depth, float depthFactor, float colorFactor, float falloff )
		{
			Color4 luma = new Color4(1,0,0,0);
			var region = new Rectangle(0,0,16384,16384);
			var flagsH = Flags.DOUBLE_PASS | Flags.HORIZONTAL | Flags.MASK_DEPTH | Flags.LUMA_ONLY;
			var flagsV = Flags.DOUBLE_PASS | Flags.VERTICAL   | Flags.MASK_DEPTH | Flags.LUMA_ONLY;
			
			BilateralPass( flagsH, temp  , region, camera, target, region, depth, region, colorFactor, depthFactor, falloff, luma );
			BilateralPass( flagsV, target, region, camera, temp  , region, depth, region, colorFactor, depthFactor, falloff, luma );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void FilterSHL1ByAlphaDoublePass ( RenderTarget2D radiance, RenderTarget2D temp, ShaderResource albedo, float intensityFactor, float alphaFactor, float falloff )
		{
			Color4 luma = new Color4(0.4f, 0.2f, 0.2f, 0.2f);
			var region = new Rectangle(0,0,16384,16384);
			BilateralPass( Flags.DOUBLE_PASS | Flags.HORIZONTAL | Flags.MASK_ALPHA, temp    , region, null, radiance, region, albedo, region, intensityFactor, alphaFactor, falloff, luma );
			BilateralPass( Flags.DOUBLE_PASS | Flags.VERTICAL   | Flags.MASK_ALPHA, radiance, region, null, temp    , region, albedo, region, intensityFactor, alphaFactor, falloff, luma );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void FilterSHL1ByAlphaSinglePass ( RenderTarget2D target, Rectangle targetRect, RenderTarget2D source, ShaderResource albedo, Rectangle sourceRect, float intensityFactor, float alphaFactor, float falloff )
		{
			Color4 luma = new Color4(0.33f, 0.33f, 0.33f, 0.33f);
			BilateralPass( Flags.SINGLE_PASS | Flags.MASK_ALPHA, target, targetRect, null, source, sourceRect, albedo, sourceRect, intensityFactor, alphaFactor, falloff, luma );
		}
	}
}
