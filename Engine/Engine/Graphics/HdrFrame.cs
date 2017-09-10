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

		public RenderTarget2D	HdrBuffer			;	
		public RenderTarget2D	LightAccumulator	;
		public DepthStencil2D	DepthBuffer			;	
		public RenderTarget2D	GBuffer0			;
		public RenderTarget2D	GBuffer1			;
		public RenderTarget2D	Normals				;	
		public RenderTarget2D	AOBuffer			;
		public RenderTarget2D	FeedbackBuffer		;

		public RenderTarget2D	Bloom0				;
		public RenderTarget2D	Bloom1				;

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

			HdrBuffer			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		false, false );
			LightAccumulator	=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	width,		height,		false, true );
			DepthBuffer			=	new DepthStencil2D( game.GraphicsDevice, DepthFormat.D24S8,		width,		height,		1 );
			GBuffer0			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8_sRGB,width,		height,		false, false );
			GBuffer1			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			Normals				=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, false );
			AOBuffer			=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba8,		width,		height,		false, true  );
			FeedbackBuffer		=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgb10A2,	width,		height,		false, false );

			Bloom0				=	new RenderTarget2D( game.GraphicsDevice, ColorFormat.Rgba16F,	bloomWidth,	bloomHeight,true, false );
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
			device.Clear( FeedbackBuffer.Surface, Color4.Black );
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

				SafeDispose( ref HdrBuffer			 );
				SafeDispose( ref LightAccumulator	 );
				SafeDispose( ref DepthBuffer		 );
				SafeDispose( ref GBuffer0			 );
				SafeDispose( ref GBuffer1			 );
				SafeDispose( ref Normals			 );
				SafeDispose( ref AOBuffer			 );
				SafeDispose( ref FeedbackBuffer		 );

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
