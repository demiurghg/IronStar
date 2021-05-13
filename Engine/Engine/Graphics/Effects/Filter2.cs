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
	internal class Filter2 : RenderComponent 
	{
		[Flags]
		enum ShaderFlags : int 
		{
			COPY			=	1 << 0,
			CLEAR			=	1 << 1,
			COLOR			=	1 << 2,
			DEPTH			=	1 << 3,
			RENDER_BORDER	=	1 << 4,
			RENDER_SPOT		=	1 << 5,
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

			if (flags.HasFlag(ShaderFlags.COLOR))
			{
			}

			if (flags.HasFlag(ShaderFlags.DEPTH))
			{
			}
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

		void SetupPass ( ShaderFlags flags, RenderTargetSurface colorTarget, DepthStencilSurface depthTarget, Rectangle? dstRegion, ShaderResource src, Rectangle? srcRegion, Color color, bool fast = false )
		{
			if (!fast)
			{
				device.ResetStates();
				device.PipelineState	=	factory[ (int)flags ];
			}

			CDATA cData = new CDATA();

			Rectangle rect = new Rectangle(0,0,1,1);

			if (colorTarget!=null) rect = colorTarget.Bounds;
			if (depthTarget!=null) rect = depthTarget.Bounds;

			cData.TargetSize.X = rect.Width;
			cData.TargetSize.Y = rect.Height;
			cData.TargetSize.Z = 1.0f / rect.Width;
			cData.TargetSize.W = 1.0f / rect.Height;

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
				device.SetViewport( rect );
				device.SetScissorRect( rect );
			}

			cbuffer.SetData( ref cData );

			if (!fast)
			{
				device.SetTargets( depthTarget, colorTarget );

				device.GfxResources[regSource]				=	src;
				device.GfxSamplers[regSamplerPointClamp]	=	SamplerState.PointClamp;
				device.GfxSamplers[regSamplerLinearClamp]	=	SamplerState.LinearClamp;
				device.GfxConstants[regCData]				=	cbuffer;
			}
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




		public void CopyColor ( RenderTargetSurface dst, ShaderResource src, Rectangle dstRegion, Rectangle srcRegion, Color color )
		{
			using( new PixEvent("CopyColor") ) 
			{
				SetupPass( ShaderFlags.COPY|ShaderFlags.COLOR, dst, null, dstRegion, src, srcRegion, color );
				device.Draw( 4, 0 );
			}
		}


		public void CopyColorBatched ( RenderTargetSurface dst, ShaderResource src, Rectangle[] dstRegions, Rectangle[] srcRegions, Color color )
		{
			using( new PixEvent("CopyColorBatched") ) 
			{
				int count = Math.Min( dstRegions.Length, srcRegions.Length );

				for (int i=0; i<count; i++)
				{
					SetupPass( ShaderFlags.COPY|ShaderFlags.COLOR, dst, null, dstRegions[i], src, srcRegions[i], color, i!=0 );
					device.Draw( 4, 0 );
				}
			}
		}


		public void ClearColor ( RenderTargetSurface dst, Rectangle dstRegion, Color color )
		{
			using( new PixEvent("ClearColor") ) 
			{
				SetupPass( ShaderFlags.CLEAR|ShaderFlags.COLOR, dst, null, dstRegion, null, null, color );
				device.Draw( 4, 0 );
			}
		}


		public void ClearColorBatched ( RenderTargetSurface dst, Rectangle[] dstRegions, Color color )
		{
			using( new PixEvent("ClearColorBatched") ) 
			{
				int count = dstRegions.Length;

				for (int i=0; i<count; i++)
				{
					SetupPass( ShaderFlags.CLEAR|ShaderFlags.COLOR, dst, null, dstRegions[i], null, null, color, i!=0 );
					device.Draw( 4, 0 );
				}
			}
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
				SetupPass( ShaderFlags.RENDER_BORDER, dst, null, dstRegion, null, null, color );

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
				SetupPass( ShaderFlags.RENDER_SPOT, dst, null, dstRegion, null, null, color );

				device.Draw( 4, 0 );
			}
		}
	}
}
