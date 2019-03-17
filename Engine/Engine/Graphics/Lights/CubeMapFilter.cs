using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.IO;
using Fusion.Engine.Graphics.Ubershaders;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using Native.Embree;
using Fusion.Engine.Graphics.Lights;

namespace Fusion.Engine.Graphics {

	[RequireShader("cubegen", true)]
	internal partial class CubeMapFilter : RenderComponent {


		[ShaderDefine]
		const int BlockSizeX = 16;

		[ShaderDefine]
		const int BlockSizeY = 16;

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct PARAMS {
			public	float	Roughness;
			public	float	MipLevel;
			public	float	Dummy1;
			public	float	Dummy2;
		}



		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbuffer;
		

		enum Flags {
			PREFILTER			=	0x0001,
		}
		


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		public CubeMapFilter( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			cbuffer	=	new ConstantBuffer( rs.Device, typeof(PARAMS) );

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			SafeDispose( ref factory );

			shader	=	Game.Content.Load<Ubershader>("cubegen");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref factory );
				SafeDispose( ref cbuffer );
			}

			base.Dispose( disposing );
		}




		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Light probe relighting :
		 * 
		-----------------------------------------------------------------------------------------*/


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		/// <param name="target"></param>
		public void PrefilterLightProbe ( RenderTargetCube source, RenderTargetCube target )
		{
			device.ResetStates();

			int mipCount	=	RenderSystem.LightProbeMaxMips;
			int maxMip		=	RenderSystem.LightProbeMaxSpecularMip;

			if (source==null) throw new ArgumentNullException("source");
			if (target==null) throw new ArgumentNullException("target");
			if (source.Width!=target.Width) {
				throw new ArgumentException("source.Width != target.Width");
			}
			if (source.Height!=target.Height) {
				throw new ArgumentException("source.Height != target.Height");
			}
			if (source.MipCount!=target.MipCount) {
				throw new ArgumentException("source.Height != target.Height");
			}

			int initialSize	=	source.Width;
			//int mipCount	=	Math.Min( source.MipCount, maxMip );

			source.BuildMipmaps();
			
			using ( new PixEvent( "PrefilterLightProbes" ) ) {


				//
				//	prefilter specular :
				//
				for (int mip=0; mip<mipCount; mip++) {

					var flag		=	Flags.PREFILTER;
					var roughness	=	MathUtil.Clamp( mip / (float)maxMip, 0, 1 );
					
					device.PipelineState = factory[(int)flag];

					cbuffer.SetData( new Vector4(roughness,mip,0,0) ); 

					device.ComputeShaderSamplers[0]		=	SamplerState.LinearWrap;
					device.ComputeShaderResources[0]	=	source;
					device.ComputeShaderConstants[0]	=	cbuffer;
				
					device.SetCSRWTexture( 0, target.GetCubeSurface( mip ) );

					int size	=	initialSize >> mip;
					int tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
					int tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
					int tgz		=	1;

					device.Dispatch( tgx, tgy, tgz );
				}
			}
		}
	}
}
