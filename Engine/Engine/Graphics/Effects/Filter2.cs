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
	[RequireShader("filter2", true)]
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

		static FXConstantBuffer<CDATA>	regCData				=	new CRegister( 0, "CData"	);
		static FXTexture2D<Vector4>		regSource				=	new TRegister( 0, "Source"	);
		static FXSamplerState			regSamplerPointClamp	=	new SRegister( 0, "SamplerPointClamp"	);
		static FXSamplerState			regSamplerLinearClamp	=	new SRegister( 1, "SamplerLinearClamp"	);

		[StructLayout(LayoutKind.Sequential, Pack=4)]
		public struct CDATA
		{														  
			public Vector4	ScaleOffset;
			public Vector4	TargetSize;
			public Vector4	Color;
			public Vector4	Dummy;
		}

		
		public Filter2( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			LoadContent();

			cbuffer	=	new ConstantBuffer( device, typeof(CDATA) );

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
			if (disposing) 
			{
				SafeDispose( ref cbuffer );
			}

			base.Dispose( disposing );
		}

		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Base filters
		 * 
		-----------------------------------------------------------------------------------------------*/

		void SetupPass ( RenderTargetSurface dst, Rectangle? dstRegion, ShaderResource src, Rectangle? srcRegion, Color color )
		{
			device.ResetStates();

			CDATA cData = new CDATA();

			cData.TargetSize.X = dst.Width;
			cData.TargetSize.Y = dst.Height;
			cData.TargetSize.Z = 1.0f / dst.Width;
			cData.TargetSize.W = 1.0f / dst.Height;

			if (srcRegion.HasValue && src!=null)
			{
				cData.ScaleOffset	=	GetScaleOffset( srcRegion.Value, src.Width, src.Height );
			}
			else
			{	
				cData.ScaleOffset	=	new Vector4(1,1,0,0);
			}

			cData.Color	=	color.ToVector4();

			if (dstRegion.HasValue)
			{
				device.SetViewport( dstRegion.Value );
				device.SetScissorRect( dstRegion.Value );
			}
			else
			{
				device.SetViewport( dst.Bounds );
				device.SetScissorRect( dst.Bounds );
			}

			cbuffer.SetData( ref cData );

			device.SetTargets( null, dst );

			device.GfxResources[regSource]				=	src;
			device.GfxSamplers[regSamplerPointClamp]	=	SamplerState.PointClamp;
			device.GfxSamplers[regSamplerLinearClamp]	=	SamplerState.LinearClamp;
			device.GfxConstants[regCData]				=	cbuffer;
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
		public void RenderQuad ( RenderTargetSurface dst, ShaderResource src, Rectangle dstRegion, Rectangle srcRegion, Color color )
		{
			using( new PixEvent("RenderQuad") ) 
			{
				SetupPass( dst, dstRegion, src, srcRegion, color );

				device.PipelineState	=	factory[ (int)(ShaderFlags.RENDER_QUAD) ];

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
		public void RenderBorder ( RenderTargetSurface dst, Rectangle dstRegion, Color color )
		{
			using( new PixEvent("RenderBorder") ) 
			{
				SetupPass( dst, dstRegion, null, null, color );

				device.PipelineState	=	factory[ (int)(ShaderFlags.RENDER_BORDER) ];

				device.Draw( 4, 0 );
			}
		}



		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void RenderSpot ( RenderTargetSurface dst, Rectangle dstRegion, Color color )
		{
			using( new PixEvent("RenderSpot") ) 
			{
				SetupPass( dst, dstRegion, null, null, color );

				device.PipelineState	=	factory[ (int)(ShaderFlags.RENDER_SPOT) ];

				device.Draw( 4, 0 );
			}
			device.ResetStates();
		}
	}
}
