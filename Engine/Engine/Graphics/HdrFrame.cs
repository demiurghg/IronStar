using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Graphics {
	
	/// <summary>
	/// Represents 
	/// </summary>
	internal class HdrFrame : DisposableBase {

		public const int FeedbackBufferWidth	=	80;
		public const int FeedbackBufferHeight	=	60;

		public FeedbackBuffer	FeedbackBufferRB	;

		public RenderTarget2D	FinalColor			;	
		public RenderTarget2D	TempColor			;

		public RenderTarget2D	FinalHdrImage		;

		public RenderTarget2D	HdrBuffer			;	
		public DepthStencil2D	DepthBuffer			;	
		public RenderTarget2D	HdrBufferGlass		;	
		public RenderTarget2D	DistortionGlass		;	
		public DepthStencil2D	DepthBufferGlass	;

		#warning Remove GBuffer0, GBuffer1	
		public RenderTarget2D	GBuffer0			;
		public RenderTarget2D	GBuffer1			;
		public RenderTarget2D	Normals				;	
		public RenderTarget2D	AOBuffer			;
		public RenderTarget2D	FeedbackBuffer		;

		public RenderTarget2D	SoftParticlesFront	;
		public RenderTarget2D	SoftParticlesBack	;
		public RenderTarget2D	ParticleVelocity	;
		public RenderTarget2D	DistortionBuffer	;

		public RenderTarget2D	Bloom0				;
		public RenderTarget2D	Bloom1				;

		public RenderTarget2D	HalfDepthBuffer		;

		public RenderTarget2D	TempColorFull0		;
		public RenderTarget2D	TempColorFull1		;

		public RenderTarget2D	TempColorHalf0		;
		public RenderTarget2D	TempColorHalf1		;

		public RenderTarget2D	DepthSliceMap0		;
		public RenderTarget2D	DepthSliceMap1		;
		public RenderTarget2D	DepthSliceMap2		;
		public RenderTarget2D	DepthSliceMap3		;

		public RenderTarget2D	MeasuredNew			;
		public RenderTarget2D	MeasuredOld			;


		public HdrFrame ( Game game, int width, int height )
		{
			int fbbw = FeedbackBufferWidth;
			int fbbh = FeedbackBufferHeight;

			int halfWidth		=	width  / 2;
			int halfHeight		=	height / 2;

			int bloomWidth		=	( width/2  ) & 0xFFF0;
			int bloomHeight		=	( height/2 ) & 0xFFF0;


			FeedbackBufferRB	=	new FeedbackBuffer( game.GraphicsDevice,						fbbw,		fbbh		  );

			FinalColor			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			TempColor			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );

			FinalHdrImage		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		false, false );

			HdrBuffer			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		true,  false );
			DepthBuffer			=	new DepthStencil2D( game.GraphicsDevice, DepthFormat.D24S8,		width,		height,		1 );
			HdrBufferGlass		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		true,  false );
			DistortionGlass		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			DepthBufferGlass	=	new DepthStencil2D( game.GraphicsDevice, DepthFormat.D24S8,		width,		height,		1 );
			GBuffer0			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8_sRGB,width,		height,		false, false );
			GBuffer1			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			Normals				=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			AOBuffer			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, true  );
			FeedbackBuffer		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgb10A2,	width,		height,		false, false );

			DistortionBuffer	=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			SoftParticlesFront	=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		false, false );
			SoftParticlesBack	=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		false, false );
			ParticleVelocity	=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );

			HalfDepthBuffer		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rg32F,		width/2,	height/2,	false, false );

			Bloom0				=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	bloomWidth,	bloomHeight,true, true );
			Bloom1				=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	bloomWidth,	bloomHeight,true, true );

			TempColorFull0		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height, 	true, true );
			TempColorFull1		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height, 	true, true );
			
			TempColorHalf0		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		halfWidth,	halfHeight,	true, false );
			TempColorHalf1		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		halfWidth,	halfHeight,	true, true );

			DepthSliceMap0		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.R32F,		halfWidth,	halfHeight, false, true );
			DepthSliceMap1		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.R32F,		halfWidth,	halfHeight, false, true );
			DepthSliceMap2		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.R32F,		halfWidth,	halfHeight, false, true );
			DepthSliceMap3		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.R32F,		halfWidth,	halfHeight, false, true );

			MeasuredOld			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba32F,   1,  1 );
			MeasuredNew			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba32F,   1,  1 );

			Clear();
		}


		public void Clear ()
		{
			var device = HdrBuffer.GraphicsDevice;

			device.Clear( DepthBuffer.Surface,		1, 0 );
			device.Clear( HdrBuffer.Surface,		Color4.Black );

			device.Clear( FeedbackBuffer.Surface,	Color4.Black );

			device.Clear( DistortionBuffer.Surface, Color4.Zero );
			device.Clear( HdrBufferGlass.Surface,	Color4.Zero );

			float half	=	0.5f;
			device.Clear( DistortionGlass.Surface,	new Color4(half, half, 0, 0) );

			device.Clear( FeedbackBufferRB.Surface,	Color4.Zero );
			device.Clear( FeedbackBuffer.Surface,	Color4.Zero );

			device.Clear( SoftParticlesBack.Surface,  Color4.Zero );
			device.Clear( SoftParticlesFront.Surface, Color4.Zero );
			device.Clear( ParticleVelocity.Surface,   new Color4(half, half, 0, 0) );
		}



		public void CopySolidDepthToTransparent ()
		{
			var device = HdrBuffer.GraphicsDevice;

			//device.Gr
		}



		public void SwapFinalColor ()
		{
			Misc.Swap( ref FinalColor, ref TempColor );
		}



		protected override void Dispose(bool disposing)
		{
			if (disposing) {

				SafeDispose( ref FeedbackBufferRB	 );

				SafeDispose( ref FinalColor			 );
				SafeDispose( ref TempColor			 );

				SafeDispose( ref FinalHdrImage		 );

				SafeDispose( ref HdrBuffer			 );
				SafeDispose( ref DepthBuffer		 );
				SafeDispose( ref HdrBufferGlass		 );
				SafeDispose( ref DepthBufferGlass	 );
				SafeDispose( ref GBuffer0			 );
				SafeDispose( ref GBuffer1			 );
				SafeDispose( ref Normals			 );
				SafeDispose( ref AOBuffer			 );
				SafeDispose( ref FeedbackBuffer		 );

				SafeDispose( ref SoftParticlesFront	 );
				SafeDispose( ref SoftParticlesBack	 );
				SafeDispose( ref DistortionBuffer	 );

				SafeDispose( ref Bloom0				 );
				SafeDispose( ref Bloom1				 );

				SafeDispose( ref TempColorFull0		 );
				SafeDispose( ref TempColorFull1		 );

				SafeDispose( ref TempColorHalf0		 );
				SafeDispose( ref TempColorHalf1		 );

				SafeDispose( ref DepthSliceMap0		 );
				SafeDispose( ref DepthSliceMap1		 );
				SafeDispose( ref DepthSliceMap2		 );
				SafeDispose( ref DepthSliceMap3		 );

				SafeDispose( ref MeasuredOld		 );
				SafeDispose( ref MeasuredNew		 );
			} 

			base.Dispose(disposing);
		}
	}
}
