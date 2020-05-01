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
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=16)]
		struct DILATE 
		{
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

		void DilateGeneric ( Flags flags, RenderTarget2D target, ShaderResource source, ShaderResource mask, float threshold, float maskGain )
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
			if (target.Width!=source.Width || target.Height!=source.Height) {
				throw new ArgumentException("target and source buffer msut be the same size");
			}
			if (target.Width!=mask.Width || target.Height!=mask.Height) {
				throw new ArgumentException("target and mask buffer msut be the same size");
			}

			var filterData			=	new DILATE();
			filterData.Threshold	=	threshold;
			filterData.GainMask		=	maskGain;
			filterData.reserved2	=	0;
			filterData.reserved3	=	0;
			cbuffer.SetData( ref filterData );
					
			int tgx = MathUtil.IntDivRoundUp( target.Width,  BilateralBlockSizeX );
			int tgy = MathUtil.IntDivRoundUp( target.Height, BilateralBlockSizeY );
			int tgz = 1;

			device.ResetStates();

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
		public void DilateByMaskAlpha ( RenderTarget2D target, ShaderResource source, ShaderResource mask, float threshold, float maskGain )
		{
			DilateGeneric( Flags.MASK_ALPHA, target, source, mask, threshold, maskGain );
		}



	}
}
