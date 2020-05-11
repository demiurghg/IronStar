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
	[RequireShader("dilate", true)]
	internal class DilateFilter : RenderComponent {

		static FXConstantBuffer<DILATE>		regDilate	=	new CRegister( 0, "Dilate" );

		static FXTexture2D<Vector4>			regSource	=	new TRegister( 0, "Source" );
		static FXTexture2D<Vector4>			regMask		=	new TRegister( 1, "Mask" );

		static FXRWTexture2D<Vector4>		regTarget	=	new URegister( 0, "Target" );

		[Flags]
		enum Flags : int {
			MASK_ALPHA		=	0x0001,
		}


		[ShaderDefine]
		const int BilateralBlockSizeX = 16;

		[ShaderDefine]
		const int BilateralBlockSizeY = 16;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct DILATE 
		{
			public	UInt2	SourceXY;
			public	UInt2	TargetXY;
			public	UInt2	MaskXY;
			public	UInt2	Dummy;
			public	float	Threshold;
			public	float	GainMask;
			public	float	reserved2;
			public	float	reserved3;
		}

		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbuffer;
		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		public DilateFilter( RenderSystem device ) : base( device )
		{
		}



		/// <summary>
		/// Initializes Filter service
		/// </summary>
		public override void Initialize() 
		{
			LoadContent();

			cbuffer	=	new ConstantBuffer( device, typeof(DILATE) );

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>( "dilate" );
			factory	=	shader.CreateFactory( typeof(Flags) ); 
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

		void DilateGeneric ( Flags flags, RenderTarget2D target, Rectangle targetRect, ShaderResource source, Rectangle sourceRect, ShaderResource mask, Rectangle maskRect, float threshold, float maskGain )
		{ 
			device.ResetStates();

			if (target==null) {
				throw new ArgumentNullException("target");
			}
			if (source==null) {
				throw new ArgumentNullException("source");
			}
			if (mask==null) {
				throw new ArgumentNullException("mask");
			}

			targetRect	=	Rectangle.Intersect( targetRect, new Rectangle(0,0, target.Width, target.Height) );
			sourceRect	=	Rectangle.Intersect( sourceRect, new Rectangle(0,0, source.Width, source.Height) );

			int width	=	Math.Min( targetRect.Width, sourceRect.Width );
			int height	=	Math.Min( targetRect.Height, sourceRect.Height );

			var filterData			=	new DILATE();
			filterData.TargetXY		=	new UInt2( (uint)targetRect.X, (uint)targetRect.Y );
			filterData.SourceXY		=	new UInt2( (uint)sourceRect.X, (uint)sourceRect.Y );
			filterData.MaskXY		=	new UInt2( (uint)maskRect.X,   (uint)maskRect.Y );
			filterData.Threshold	=	threshold;
			filterData.GainMask		=	maskGain;
			filterData.reserved2	=	0;
			filterData.reserved3	=	0;
			cbuffer.SetData( ref filterData );
					
			int tgx = MathUtil.IntDivRoundUp( width,  BilateralBlockSizeX );
			int tgy = MathUtil.IntDivRoundUp( height, BilateralBlockSizeY );
			int tgz = 1;

			device.ComputeResources[ regSource	]	=	source;
			device.ComputeResources[ regMask	]	=	mask;

			device.ComputeConstants[ regDilate	]	=	cbuffer;

			device.SetComputeUnorderedAccess( regTarget, target.Surface.UnorderedAccess );

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
		public void DilateByMaskAlpha ( RenderTarget2D target, Rectangle targetRect, ShaderResource source, Rectangle sourceRect, ShaderResource mask, Rectangle maskRect, float threshold, float maskGain )
		{
			DilateGeneric( Flags.MASK_ALPHA, target, targetRect, source, sourceRect, mask, maskRect, threshold, maskGain );
		}



	}
}
