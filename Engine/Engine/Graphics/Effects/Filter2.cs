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
	/// Class for base image processing such as copying, blurring, enhancement, anti-aliasing etc.
	/// </summary>
	[RequireShader("filter2")]
	internal class Filter2 : RenderComponent {

		[Flags]
		enum ShaderFlags : int {
			RENDER_QUAD		= 0x0001,
			RENDER_BORDER	= 0x0002,
			RENDER_SPOT		= 0x0004,
		}

		Ubershader		shaders;
		StateFactory	factory;
		ConstantBuffer	cbuffer;
		
		public Filter2( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			LoadContent();

			cbuffer	=	new ConstantBuffer( device, typeof(Vector4) );

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shaders = Game.Content.Load<Ubershader>( "filter2" );
			factory	= shaders.CreateFactory( typeof(ShaderFlags), (ps,i) => Enum(ps, (ShaderFlags)i) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void Enum ( PipelineState ps, ShaderFlags flags )
		{
			ps.Primitive			=	Primitive.TriangleStrip;
			ps.VertexInputElements	=	VertexInputElement.Empty;
			ps.BlendState			=	BlendState.Opaque;
			ps.RasterizerState		=	RasterizerState.CullNone;
			ps.DepthStencilState	=	DepthStencilState.None;
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



		/// <summary>
		/// Sets default render state
		/// </summary>
		void SetDefaultRenderStates()
		{
			device.ResetStates();
		}

		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Base filters
		 * 
		-----------------------------------------------------------------------------------------------*/

		void SetViewport ( RenderTargetSurface dst )
		{
			device.SetViewport( 0,0, dst.Width, dst.Height );
		}



		Vector4 GetScaleOffset ( Rectangle rect, int width, int height )
		{
			float ax = rect.Width  / (float)width;
			float ay = rect.Height / (float)height;
			float bx = rect.Left   / (float)width;
			float by = rect.Top    / (float)height;

			float x		=	0.5f * ax;
			float y		=  -0.5f * ay;
			float z		=   0.5f * ax + bx;
			float w		=	0.5f * ay + by;

			return new Vector4(x,y,z,w);
		}





		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void RenderQuad ( RenderTargetSurface dst, ShaderResource src, Rectangle dstRegion, Rectangle srcRegion )
		{
			SetDefaultRenderStates();

			using( new PixEvent("StretchRect") ) {

				device.SetTargets( null, dst );
				device.SetViewport( dstRegion.X, dstRegion.Y, dstRegion.Width, dstRegion.Height );

				device.PipelineState	=	factory[ (int)(ShaderFlags.RENDER_QUAD) ];

				Vector4 scaleOffset	=	 GetScaleOffset( srcRegion, src.Width, src.Height );
				cbuffer.SetData( ref scaleOffset );

				device.GfxResources[0]	=	src;
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;
				device.GfxConstants[0]	=	cbuffer;

				device.Draw( 4, 0 );
			}
			device.ResetStates();
		}


		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void RenderBorder ( RenderTargetSurface dst, Rectangle dstRegion, float width )
		{
			SetDefaultRenderStates();

			using( new PixEvent("StretchRect") ) {

				device.SetTargets( null, dst );
				device.SetScissorRect( dstRegion );
				device.SetViewport( dstRegion );

				device.PipelineState			=	factory[ (int)(ShaderFlags.RENDER_BORDER) ];

				Vector4 targetSize;
				targetSize.X = dst.Width;
				targetSize.Y = dst.Height;
				targetSize.Z = 1.0f / dst.Width;
				targetSize.W = 1.0f / dst.Height;

				cbuffer.SetData( targetSize );

				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;
				device.GfxConstants[0]	=	cbuffer;

				device.Draw( 4, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void RenderSpot ( RenderTargetSurface dst, Rectangle dstRegion, float width )
		{
			SetDefaultRenderStates();

			using( new PixEvent("StretchRect") ) {

				device.SetTargets( null, dst );
				device.SetViewport( dstRegion.X, dstRegion.Y, dstRegion.Width, dstRegion.Height );

				device.PipelineState			=	factory[ (int)(ShaderFlags.RENDER_SPOT) ];

				Vector4 targetSize;
				targetSize.X = dst.Width;
				targetSize.Y = dst.Height;
				targetSize.Z = 1.0f / dst.Width;
				targetSize.W = 1.0f / dst.Height;

				cbuffer.SetData( ref targetSize );

				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;
				device.GfxConstants[0]	=	cbuffer;

				device.Draw( 4, 0 );
			}
			device.ResetStates();
		}



	}
}
