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
	public enum BilateralType {
		Depth,
		Normals,
	}


	/// <summary>
	/// Bilateral filter
	/// </summary>
	[RequireShader("bilateral", true)]
	internal class BilateralFilter : RenderComponent {

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
		}


		[ShaderDefine]
		const int BilateralBlockSizeX = 16;

		[ShaderDefine]
		const int BilateralBlockSizeY = 16;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct FilterParams 
		{
			public	Color4	LumaVector;
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

		void BilateralPass ( Flags flags, RenderTarget2D target, Camera camera, ShaderResource source, ShaderResource mask, float colorFactor, float maskFactor, float falloff, Color4 lumaVector )
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
			//if (camera==null) {
			//	throw new ArgumentNullException("camera");
			//}

			if (target.Width!=source.Width || target.Width!=source.Width) {
				throw new ArgumentException("target and source size are not the same");
			}
			if (target.Width!=mask.Width || target.Width!=mask.Width) {
				throw new ArgumentException("target and mask size are not the same");
			}

			var filterData              =   new FilterParams();
			filterData.LumaVector		=	lumaVector;
			filterData.MaskFactor		=   maskFactor;
			filterData.GaussFalloff		=	falloff;
			filterData.ColorFactor		=   colorFactor;
			cbuffer.SetData( ref filterData );
					
			int tgx = MathUtil.IntDivRoundUp( target.Width,  BilateralBlockSizeX );
			int tgy = MathUtil.IntDivRoundUp( target.Height, BilateralBlockSizeY );
			int tgz = 1;

			//	HORIZONTAL pass :
			device.ResetStates();

			device.ComputeResources[0]    =   source;
			device.ComputeResources[1]    =   mask;

			device.ComputeConstants[ regParams	]	=	cbuffer;
			device.ComputeConstants[ regCamera	]	=	camera?.CameraData;

			device.SetComputeUnorderedAccess( 0, target.Surface.UnorderedAccess );

			device.PipelineState = factory[(int)flags];
			device.Dispatch( tgx, tgy, tgz );
		}


		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void FilterSSAOByDepth ( Camera camera, RenderTarget2D target, RenderTarget2D temp, ShaderResource depth, float depthFactor, float colorFactor, float falloff )
		{
			Color4 luma = new Color4(1,0,0,0);
			BilateralPass( Flags.DOUBLE_PASS | Flags.HORIZONTAL | Flags.MASK_DEPTH, temp  , camera, target, depth, colorFactor, depthFactor, falloff, luma );
			BilateralPass( Flags.DOUBLE_PASS | Flags.VERTICAL   | Flags.MASK_DEPTH, target, camera, temp  , depth, colorFactor, depthFactor, falloff, luma );
		}



		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void FilterSHL1ByAlpha ( RenderTarget2D radiance, RenderTarget2D temp, ShaderResource albedo, float intensityFactor, float alphaFactor, float falloff )
		{
			Color4 luma = new Color4(0.4f, 0.2f, 0.2f, 0.2f);
			BilateralPass( Flags.DOUBLE_PASS | Flags.HORIZONTAL | Flags.MASK_ALPHA, temp  , null, radiance, albedo, intensityFactor, alphaFactor, falloff, luma );
			BilateralPass( Flags.DOUBLE_PASS | Flags.VERTICAL   | Flags.MASK_ALPHA, radiance, null, temp  , albedo, intensityFactor, alphaFactor, falloff, luma );
		}


	}
}
