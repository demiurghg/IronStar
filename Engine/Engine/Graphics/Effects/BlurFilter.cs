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
	[RequireShader("blur", true)]
	internal class BlurFilter : RenderComponent {
		const int MaxBlurTaps	=	33;


		[ShaderDefine]
		const int BlockSize = 16;

		[Flags]
		enum Flags : int
		{
			GAUSSIAN		=	0x00000001,
			PASS1			=	0x00000002,
			PASS2			=	0x00000004,
		}



		Ubershader		shaders;
		StateFactory	factory;
		
		public BlurFilter( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shaders = Game.Content.Load<Ubershader>( "blur" );
			factory	= shaders.CreateFactory( typeof(Flags) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
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
		public void GaussBlur( RenderTarget2D target, RenderTarget2D temp, int mipLevel )
		{

			int width	=	target.Width  >> mipLevel;
			int height	=	target.Height >> mipLevel;

			using( new PixEvent("GaussBlur") ) {

				int gx	=	MathUtil.IntDivRoundUp( width,  BlockSize );
				int gy	=	MathUtil.IntDivRoundUp( height, BlockSize );
				int gz	=	1;


				device.ResetStates();

				device.ComputeShaderSamplers[0]		=	SamplerState.LinearClamp;
				device.ComputeShaderResources[0]	=	target.GetShaderResource( mipLevel );
				device.SetCSRWTexture( 0, temp.GetSurface( mipLevel ) );

				device.PipelineState	=	factory[ (int)(Flags.GAUSSIAN|Flags.PASS1) ];
				device.Dispatch( gx, gy, gz );


				device.ResetStates();

				device.ComputeShaderSamplers[0]		=	SamplerState.LinearClamp;
				device.ComputeShaderResources[0]	=	temp.GetShaderResource( mipLevel );
				device.SetCSRWTexture( 0, target.GetSurface( mipLevel ) );

				device.PipelineState	=	factory[ (int)(Flags.GAUSSIAN|Flags.PASS2) ];
				device.Dispatch( gx, gy, gz );
			}
		}
	}
}
