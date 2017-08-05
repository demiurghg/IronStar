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

		[Flags]
		enum Flags : int {
			DEPTH			=	0x0001,
			NORMAL			=	0x0002,
			VERTICAL		=	0x0004,
			HORIZONTAL		=	0x0008,
		}


		[ShaderDefine]
		const int BilateralBlockSizeX = 16;

		[ShaderDefine]
		const int BilateralBlockSizeY = 16;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=16)]
		struct FilterParams {

			public	float   LinDepthScale;
			public	float   LinDepthBias;

			public	float	DepthFactor;
			public	float	ColorFactor;
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
		 *	Base filters
		 * 
		-----------------------------------------------------------------------------------------------*/



		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void RenderBilateralFilter ( Camera camera, RenderTarget2D target, RenderTarget2D temp, ShaderResource depth, float depthFactor, float colorFactor )
		{
			device.ResetStates();

			if (target==null) {
				throw new ArgumentNullException("target");
			}
			if (temp==null) {
				throw new ArgumentNullException("temp");
			}
			if (depth==null) {
				throw new ArgumentNullException("discontinuityMap");
			}
			if (camera==null) {
				throw new ArgumentNullException("camera");
			}

			if (target.Width!=temp.Width || target.Width!=depth.Width) {
				throw new ArgumentException("target buffer, temp buffer and depth buffer must have the same width");
			}

			if (target.Height!=temp.Height || target.Height!=depth.Height) {
				throw new ArgumentException("target buffer, temp buffer and depth buffer must have the same width");
			}


			using( new PixEvent("BilateralFilter") ) {

				var filterData              =   new FilterParams();
				filterData.LinDepthBias     =   camera.LinearizeDepthBias;
				filterData.LinDepthScale    =   camera.LinearizeDepthScale;
				filterData.DepthFactor		=   depthFactor;
				filterData.ColorFactor		=   colorFactor;
				cbuffer.SetData( filterData );
					
				int tgx = MathUtil.IntDivRoundUp( target.Width,  BilateralBlockSizeX );
				int tgy = MathUtil.IntDivRoundUp( target.Height, BilateralBlockSizeY );
				int tgz = 1;

				//	HORIZONTAL pass :
				device.ResetStates();

				device.ComputeShaderResources[0]    =   target;
				device.ComputeShaderResources[1]    =   depth;
				device.ComputeShaderConstants[0]    =   cbuffer;
				device.SetCSRWTexture( 0, temp.Surface );

				device.PipelineState = factory[(int)(Flags.DEPTH|Flags.HORIZONTAL)];
				device.Dispatch( tgx, tgy, tgz );

				//	VERTICAL pass :
				device.ResetStates();

				device.ComputeShaderResources[0]    =   temp;
				device.ComputeShaderResources[1]    =   depth;
				device.ComputeShaderConstants[0]    =   cbuffer;
				device.SetCSRWTexture( 0, target.Surface );

				device.PipelineState = factory[(int)(Flags.DEPTH|Flags.VERTICAL)];
				device.Dispatch( tgx, tgy, tgz );

			}
		}



	}
}
