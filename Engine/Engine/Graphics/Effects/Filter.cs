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
	[RequireShader("filter")]
	internal class Filter : RenderComponent 
	{
		const int MaxBlurTaps	=	33;

		[Flags]
		enum ShaderFlags : int
		{
			PASS1								= 1 << 0,
			PASS2								= 1 << 1,
			FXAA								= 1 << 2,
			COPY								= 1 << 3,
			STRETCH_RECT						= 1 << 4,
			DOWNSAMPLE_2_4x4					= 1 << 5,
			DOWNSAMPLE_4						= 1 << 6,
			GAUSS_BLUR_3x3						= 1 << 7,
			GAUSS_BLUR							= 1 << 8,
			TO_CUBE_FACE						= 1 << 9,
			LINEARIZE_DEPTH						= 1 << 10,
			RESOLVE_AND_LINEARIZE_DEPTH_MSAA	= 1 << 11,
			OVERLAY_ADDITIVE					= 1 << 12,
			PREFILTER_ENVMAP					= 1 << 13,
			POSX								= 1 << 14,
			POSY								= 1 << 15,
			POSZ								= 1 << 16,
			NEGX								= 1 << 17,
			NEGY								= 1 << 18,
			NEGZ								= 1 << 19,
			FILL_ALPHA_ONE						= 1 << 20,
			BILATERAL							= 1 << 21,
			COPY_ALPHA							= 1 << 22,
			DOWNSAMPLE_DEPTH_RED				= 1 << 23,
			DOWNSAMPLE_DEPTH_GREEN				= 1 << 24,
			CLEAR_DEPTH							= 1 << 25,
		}

		[StructLayout( LayoutKind.Explicit )]
		struct LinearDepth
		{
			[FieldOffset(0)]	public	float	linearizeDepthA;        
			[FieldOffset(4)]	public	float	linearizeDepthB;        
		}


		Ubershader		shaders;
		StateFactory	factory;
		ConstantBuffer	gaussWeightsCB;
		ConstantBuffer	sourceRectCB;
		ConstantBuffer	matrixCB;
		ConstantBuffer	vectorCB;
		ConstantBuffer	bufLinearizeDepth;
		
		public Filter( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			bufLinearizeDepth	= new ConstantBuffer( device, 128 );
			gaussWeightsCB		= new ConstantBuffer( device, typeof(Vector4), MaxBlurTaps );
			sourceRectCB		= new ConstantBuffer( device, typeof(Vector4) );
			matrixCB			= new ConstantBuffer( device, typeof(Matrix), 1 );
			vectorCB			= new ConstantBuffer( device, typeof(Vector4), 1 );

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shaders = Game.Content.Load<Ubershader>( "filter" );
			factory	= shaders.CreateFactory( typeof(ShaderFlags), (ps,i) => Enum(ps, (ShaderFlags)i) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void Enum ( PipelineState ps, ShaderFlags flags )
		{
			ps.Primitive			=	Primitive.TriangleList;
			ps.VertexInputElements	=	VertexInputElement.Empty;
			ps.BlendState			=	BlendState.Opaque;
			ps.RasterizerState		=	RasterizerState.CullNone;
			ps.DepthStencilState	=	DepthStencilState.None;

			if (flags==ShaderFlags.OVERLAY_ADDITIVE) {
				ps.BlendState = BlendState.Additive;
			}

			if (flags==ShaderFlags.FILL_ALPHA_ONE) {
				ps.BlendState = BlendState.AlphaMaskWrite;
			}

			if (flags==ShaderFlags.DOWNSAMPLE_DEPTH_RED) {
				ps.BlendState = BlendState.WriteMaskRed;
			}

			if (flags==ShaderFlags.DOWNSAMPLE_DEPTH_GREEN) {
				ps.BlendState = BlendState.WriteMaskGreen;
			}

			if (flags==ShaderFlags.CLEAR_DEPTH)
			{
				var dss = new DepthStencilState();
				dss.DepthComparison		=	ComparisonFunc.Always;
				dss.DepthWriteEnabled	=	true;
				dss.DepthEnabled		=	true;
				ps.DepthStencilState	=	dss;
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
				SafeDispose( ref gaussWeightsCB );
				SafeDispose( ref sourceRectCB );
				SafeDispose( ref bufLinearizeDepth );
				SafeDispose( ref matrixCB );
				SafeDispose( ref vectorCB );
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

		public void StretchRect( RenderTargetSurface dst, ShaderResource src, Rectangle dstRect )
		{
			StretchRect( dst, src, null, false, dstRect );
		}

		/// <summary>
		/// Performs good-old StretchRect to destination buffer with blending.
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="src"></param>
		/// <param name="filter"></param>
		/// <param name="rect"></param>
		public void StretchRect( RenderTargetSurface dst, ShaderResource src, SamplerState filter = null, bool flipToCubeFace = false, Rectangle? dstRect = null )
		{
			SetDefaultRenderStates();

			using( new PixEvent("StretchRect") ) {

				SetViewport(dst, dstRect);
				device.SetTargets( null, dst );

				if (flipToCubeFace) {
					device.PipelineState		=	factory[ (int)(ShaderFlags.STRETCH_RECT|ShaderFlags.TO_CUBE_FACE) ];
				} else {
					device.PipelineState		=	factory[ (int)ShaderFlags.STRETCH_RECT ];
				}
				device.GfxResources[0]	=	src;
				device.GfxSamplers[0]	=	filter ?? SamplerState.LinearPointClamp;
				device.GfxConstants[0]	=	sourceRectCB;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		public void StretchRect4x4( RenderTargetSurface dst, RenderTarget2D src, SamplerState filter = null, bool flipToCubeFace = false )
		{
			SetDefaultRenderStates();

			using( new PixEvent("StretchRect4x4") ) {

				device.SetTargets( null, dst );
				SetViewport(dst);
				
				if (flipToCubeFace) {
					device.PipelineState		=	factory[ (int)(ShaderFlags.DOWNSAMPLE_2_4x4|ShaderFlags.TO_CUBE_FACE) ];
				} else {
					device.PipelineState		=	factory[ (int)ShaderFlags.DOWNSAMPLE_2_4x4 ];
				}
				device.GfxResources[0]	=	src;
				device.GfxSamplers[0]	=	filter ?? SamplerState.LinearPointClamp;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		public void DownSample4( RenderTarget2D dst, RenderTarget2D src )
		{
			SetDefaultRenderStates();

			using( new PixEvent("DownSample4") ) {

				dst.SetViewport();
				device.SetTargets( null, dst );

				device.PipelineState	=	factory[ (int)ShaderFlags.DOWNSAMPLE_4 ];
				device.GfxResources[0]	=	src;
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		void SetViewport ( RenderTargetSurface dst, Rectangle? dstRect = null )
		{
			var rect = dstRect.HasValue ? dstRect.Value : new Rectangle( 0, 0, dst.Width, dst.Height );

			device.SetScissorRect( rect );
			device.SetViewport	 ( new Viewport(rect) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst">target to copy to</param>
		/// <param name="src">target to copy from</param>
		public void Copy( RenderTargetSurface dst, ShaderResource src )
		{
			SetDefaultRenderStates();

			using ( new PixEvent( "Copy" ) ) {

				if ( dst == null ) {
					device.RestoreBackbuffer();
				} else {
					SetViewport( dst );
					device.SetTargets( null, dst );
				}

				device.PipelineState	=	factory[(int)ShaderFlags.COPY];
				device.GfxResources[0]	=	src;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst">target to copy to</param>
		/// <param name="src">target to copy from</param>
		public void ClearDepth( DepthStencilSurface depthStencil, Rectangle region )
		{
			SetDefaultRenderStates();

			using ( new PixEvent( "ClearDepth" ) ) 
			{
				device.SetScissorRect( region );
				device.SetViewport( region );

				device.SetTargets( depthStencil );

				device.PipelineState	=	factory[(int)ShaderFlags.CLEAR_DEPTH];

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		public void CopyColor (	RenderTargetSurface dst, ShaderResource src )
		{
			Copy ( dst, src );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst">target to copy to</param>
		/// <param name="src">target to copy from</param>
		public void CopyAlpha( RenderTargetSurface dst, ShaderResource src )
		{
			SetDefaultRenderStates();

			using ( new PixEvent( "Copy" ) ) {

				if ( dst == null ) {
					device.RestoreBackbuffer();
				} else {
					SetViewport( dst );
					device.SetTargets( null, dst );
				}

				device.PipelineState	=	factory[(int)ShaderFlags.COPY_ALPHA];
				device.GfxResources[0]	=	src;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst">target to copy to</param>
		/// <param name="src">target to copy from</param>
		public void FillAlphaOne( RenderTargetSurface dst )
		{
			SetDefaultRenderStates();

			using( new PixEvent("FillAlphaOne") ) {

				if(dst == null) {
					device.RestoreBackbuffer();
				} else {
					SetViewport(dst);
					device.SetTargets( null, dst );
				}

				device.PipelineState = factory[ (int)ShaderFlags.FILL_ALPHA_ONE ];

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst">target to copy to</param>
		/// <param name="src">target to copy from</param>
		public void OverlayAdditive( RenderTargetSurface dst, ShaderResource src )
		{
			SetDefaultRenderStates();

			using( new PixEvent("OverlayAdditive") ) {

				if (dst == null) {
					device.RestoreBackbuffer();
				} else {
					device.SetTargets( null, dst );
				}

				device.PipelineState	=	factory[ (int)ShaderFlags.OVERLAY_ADDITIVE ];
				device.GfxResources[0]	=	src;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public void DownsampleDepthRed( RenderTargetSurface target, DepthStencil2D source )
		{
			SetDefaultRenderStates();

			using( new PixEvent("DownsampleDepthToRed") ) {
				
				SetViewport( target );
				device.SetTargets( null, target );
				
				device.PipelineState	=	factory[ (int)ShaderFlags.DOWNSAMPLE_DEPTH_RED ];
				device.GfxResources[0]	=	source;
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;

				device.Draw( 3, 0 );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		public void DownsampleDepthGreen( RenderTargetSurface target, DepthStencil2D source )
		{
			SetDefaultRenderStates();

			using( new PixEvent("DownsampleDepthToGreen") ) {

				SetViewport( target );
				device.SetTargets( null, target );
				
				device.PipelineState	=	factory[ (int)ShaderFlags.DOWNSAMPLE_DEPTH_GREEN ];
				device.GfxResources[0]	=	source;
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;

				device.Draw( 3, 0 );
			}
		}


		/// <summary>
		/// Performs FXAA antialiasing.
		/// </summary>
		/// <param name="dst">Target buffer to render FXAA to</param>
		/// <param name="src">Source image with luminance in alpha</param>
		public void Fxaa( RenderTargetSurface dst, ShaderResource src )
		{
			SetDefaultRenderStates();

			using( new PixEvent("Fxaa") ) {

				if(dst == null) {
					device.RestoreBackbuffer();
				} else {
					SetViewport( dst );
					device.SetTargets( null, dst );
				}

				device.PipelineState	=	factory[ (int)ShaderFlags.FXAA ];
				device.GfxResources[0]	=	src;
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dst"></param>
		/// <param name="cubeSrc"></param>
		/// <param name="sampleCount"></param>
		public void PrefilterEnvMap ( RenderTargetCube envMap )
		{
			SetDefaultRenderStates();

			int width  = envMap.Width / 2;
			int height = envMap.Height / 2;

			using( new PixEvent("PrefilterEnvMap") ) {

				var sides =	new[]{ ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.POSX, ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.NEGX, 
								   ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.POSY, ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.NEGY, 
								   ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.POSZ, ShaderFlags.PREFILTER_ENVMAP | ShaderFlags.NEGZ };

				//	loop through mip levels from second to last specular mip level :
				for (int mip=1; mip<RenderSystem.LightProbeMaxSpecularMip; mip++) {

					float roughness = (float)mip / (float)(RenderSystem.LightProbeMaxSpecularMip-1);
					float step		= 1.0f / width;

					var vector = new Vector4( roughness, step,0,0 );
					vectorCB.SetData( ref vector );
					
								
					for (int face=0; face<6; face++) {

						device.SetTargets( null, envMap.GetSurface( mip, (CubeFace)face ) );
					
						device.SetViewport( 0,0, width, height );

						device.GfxConstants[0]	=	vectorCB;
						device.PipelineState	=	factory[ (int)sides[face] ];
						device.GfxResources[0]	=	envMap.GetCubeShaderResource( mip-1 );
						device.GfxSamplers[0]	=	SamplerState.LinearWrap;

						device.Draw( 3, 0 );
					}

					width /= 2;
					height /= 2;
				}
			}


			device.ResetStates();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="srcDst">source and destination target</param>
		/// <param name="temporary">temporaru target for two pass filter</param>
		/*public void GaussBlur3x3( RenderTarget2D srcDst, RenderTarget2D temporary )
		{
			SetDefaultRenderStates();

			using( new PixEvent() ) {
				srcDst.SetViewport();
				device.PixelShaderSamplers[0] = SamplerState.LinearPointClamp;

				device.PipelineState			=	factory[ (int)ShaderFlags.GAUSS_BLUR_3x3 ];
                shaders.SetPixelShader( (int)(ShaderFlags.GAUSS_BLUR_3x3) );
                shaders.SetVertexShader( (int)(ShaderFlags.GAUSS_BLUR_3x3) );

                device.SetTargets( null, temporary );
				device.VertexShaderResources[0] = srcDst;
				device.PixelShaderResources[0] = srcDst;
                
				device.Draw( Primitive.TriangleList, 3, 0 );

                shaders.SetPixelShader( (int)(ShaderFlags.GAUSS_BLUR_3x3 | ShaderFlags.PASS2) );
                shaders.SetVertexShader( (int)(ShaderFlags.GAUSS_BLUR_3x3 | ShaderFlags.PASS2) );

                device.SetTargets( null, srcDst );
				device.VertexShaderResources[0] = temporary;
				device.PixelShaderResources[0] = temporary;

                device.Draw( Primitive.TriangleList, 3, 0 );
			}
			device.ResetStates();
		}	*/

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="srcDst"></param>
		/// <param name="temporary"></param>
		/// <param name="sigma"></param>
		/// <param name="mipLevel"></param>
		public void GaussBlur ( RenderTarget2D srcDst, RenderTarget2D temporary, float sigma, int mipLevel )
		{
			GaussBlurInternal( srcDst, srcDst, temporary, sigma, 0f, mipLevel, null, null );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="srcDst"></param>
		/// <param name="temporary"></param>
		/// <param name="?"></param>
		/// <param name="depthData"></param>
		/// <param name="normalData"></param>
		/// <param name="sigma"></param>
		/// <param name="mipLevel"></param>
		public void GaussBlurBilateral ( RenderTarget2D src, RenderTarget2D dst, RenderTarget2D temporary, ShaderResource depthData, ShaderResource normalData, float sigma, float sharpness, int mipLevel )
		{
			GaussBlurInternal( src, dst, temporary, sigma, sharpness, mipLevel, depthData, normalData );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="srcDst"></param>
		/// <param name="temporary"></param>
		/// <param name="sigma"></param>
		/// <param name="kernelSize"></param>
		void GaussBlurInternal ( RenderTarget2D src, RenderTarget2D dst, RenderTarget2D temporary, float sigma, float sharpness, int mipLevel, ShaderResource depthData, ShaderResource normalData )
		{
			var taps = GetGaussWeightsBuffer( sigma, mipLevel );

			SetDefaultRenderStates();

			gaussWeightsCB.SetData( taps );


			int combination	=	(int)ShaderFlags.GAUSS_BLUR;

			if (depthData!=null && normalData!=null) {
				combination |=	(int)ShaderFlags.BILATERAL;
			}



			using( new PixEvent("GaussBlur") ) {

				SetViewport(temporary.GetSurface(mipLevel));
				device.SetTargets( null, temporary.GetSurface(mipLevel) );

				device.PipelineState	=	factory[ combination|(int)ShaderFlags.PASS1 ];
				device.GfxResources[0]	=	src;
				device.GfxResources[1]	=	depthData;
				device.GfxResources[2]	=	normalData;

				device.GfxConstants[0]	=	gaussWeightsCB;
				
				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;
				device.GfxSamplers[1]	=	SamplerState.PointClamp;

				device.Draw( 3, 0 );



				device.GfxResources[0]	=	null;

				SetViewport(dst.GetSurface(mipLevel));
				device.SetTargets( null, dst.GetSurface(mipLevel) );

				device.PipelineState	=	factory[ combination|(int)ShaderFlags.PASS2 ];
				device.GfxResources[0]	=	temporary;
				device.GfxResources[1]	=	depthData;
				device.GfxResources[2]	=	normalData;

				device.GfxConstants[0]	=	gaussWeightsCB;

				device.GfxSamplers[0]	=	SamplerState.LinearPointClamp;
				device.GfxSamplers[1]	=	SamplerState.PointClamp;

				device.Draw( 3, 0 );
			}
			device.ResetStates();
		}



		float GaussDistribution ( float x, float sigma )
		{
			var k1 =  1.0 / (sigma * Math.Sqrt(2.0*Math.PI));
			var k2 = -1.0 / (2.0 * sigma * sigma);
			return (float)( k1 * Math.Exp( k2 * x * x ) );
		}


		Vector4[] GetGaussWeightsBuffer( float sigma, int mipLevel ) 
		{
			var taps = new Vector4[MaxBlurTaps];

			for ( int i=0; i<MaxBlurTaps; i++) {

				float x = i - (MaxBlurTaps/2);

				taps[i].X = GaussDistribution( x, sigma );
				taps[i].Y = mipLevel;
				taps[i].W = x * (1 << mipLevel);
			}

			return taps;
			//bufGaussWeights.UpdateCBuffer();
			//#endif
		}
	}
}
