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
	internal partial class CubeMapFilter : RenderComponent 
	{
		static FXConstantBuffer<PARAMS> regParams = new CRegister( 0, "Params" );

		static FXSamplerState				regLinearSampler	= 	new SRegister(0, "LinearSampler");
		static FXTextureCube<Vector4>		regSource			=	new TRegister(0, "Source"		);

		[ShaderIfDef("DOWNSAMPLE")]	static FXRWTexture2DArray<Vector4>	regTarget	= 	new URegister(0, "Target"	); 
									 		
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget0	= 	new URegister(0, "Target0"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget1	= 	new URegister(1, "Target1"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget2	= 	new URegister(2, "Target2"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget3	= 	new URegister(3, "Target3"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget4	= 	new URegister(4, "Target4"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget5	= 	new URegister(5, "Target5"	); 
		[ShaderIfDef("PREFILTER")]	static FXRWTexture2DArray<Vector4>	regTarget6	= 	new URegister(6, "Target6"	); 

		[ShaderDefine]
		const int BlockSize = 8;

		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct PARAMS {
			public	float	Roughness;
			public	float	MipLevel;
			public	float	TargetSize;
			public	float	SourceSize;
		}



		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbuffer;
		

		enum Flags {
			DOWNSAMPLE	=	0x0001,
			PREFILTER	=	0x0002,
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

		public void GenerateCubeMipLevel( RenderTargetCube cubemap )
		{
			device.ResetStates();

			if (cubemap==null) throw new ArgumentNullException("cubemap");

			using ( new PixEvent( "GenerateCubeMipLevel" ) )
			{
				for ( int mip = 0; mip<cubemap.MipCount-1; mip++ )
				{
					device.PipelineState = factory[(int)Flags.DOWNSAMPLE];

					var source	=	cubemap.GetCubeShaderResource(mip);
					var target	=	cubemap.GetCubeSurface( mip + 1 ).UnorderedAccess;
					var size	=	cubemap.Width >> ( mip + 1 );

					device.SetComputeUnorderedAccess( regTarget, target );

					device.ComputeSamplers	[ regLinearSampler	]	=	SamplerState.LinearWrap;
					device.ComputeResources	[ regSource			]	=	source;


					int tgx		=	MathUtil.IntDivRoundUp( size, 8 );
					int tgy		=	MathUtil.IntDivRoundUp( size, 8 );
					int tgz		=	6; // for each face

					device.Dispatch( tgx, tgy, tgz );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		/// <param name="target"></param>
		public void PrefilterLightProbe ( RenderTargetCube source, TextureCubeArrayRW targetArray, int targetIndex )
		{
			if (targetIndex<0) return;
			
			if (source		==null) throw new ArgumentNullException("source");
			if (targetArray	==null) throw new ArgumentNullException("targetArray");

			device.PipelineState = factory[(int)Flags.PREFILTER];

			var target0	=	targetArray.GetSingleCubeSurface( targetIndex, 0 );
			var target1	=	targetArray.GetSingleCubeSurface( targetIndex, 1 );
			var target2	=	targetArray.GetSingleCubeSurface( targetIndex, 2 );
			var target3	=	targetArray.GetSingleCubeSurface( targetIndex, 3 );
			var target4	=	targetArray.GetSingleCubeSurface( targetIndex, 4 );
			var target5	=	targetArray.GetSingleCubeSurface( targetIndex, 5 );
			var target6	=	targetArray.GetSingleCubeSurface( targetIndex, 6 );
				
			device.SetComputeUnorderedAccess( 0, target0.UnorderedAccess );
			device.SetComputeUnorderedAccess( 1, target1.UnorderedAccess );
			device.SetComputeUnorderedAccess( 2, target2.UnorderedAccess );
			device.SetComputeUnorderedAccess( 3, target3.UnorderedAccess );
			device.SetComputeUnorderedAccess( 4, target4.UnorderedAccess );
			device.SetComputeUnorderedAccess( 5, target5.UnorderedAccess );
			device.SetComputeUnorderedAccess( 6, target6.UnorderedAccess );

			device.ComputeSamplers	[ regLinearSampler	]	=	SamplerState.LinearWrap;
			device.ComputeResources	[ regSource			]	=	source;
			device.ComputeConstants	[ regTarget			]	=	cbuffer;

			int size	=	128*128 + 64*64 + 32*32 + 16*16 + 8*8 + 4*4 + 2*2;
			int tgx		=	MathUtil.IntDivRoundUp( size, 64 );
			int tgy		=	6; // num faces
			int tgz		=	1;

			device.Dispatch( tgx, tgy, tgz );
		}
	}
}
