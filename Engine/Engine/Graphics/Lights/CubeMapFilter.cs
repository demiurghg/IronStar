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
		static FXRWTexture2DArray<Vector4>	regTarget			= 	new URegister(0, "Target"		); 

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
			PREFILTER	=	0x0001,

			MIP1		=	0x0010,
			MIP2		=	0x0020,
			MIP3		=	0x0040,
			MIP4		=	0x0080,
			MIP5		=	0x0100,
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
		public void PrefilterLightProbe ( ShaderResource source, UnorderedAccess target, int size, int mip, float roughness )
		{
			device.ResetStates();

			if (source==null) throw new ArgumentNullException("source");
			if (target==null) throw new ArgumentNullException("target");

			Flags flag = Flags.PREFILTER|Flags.MIP1;

			switch (mip)
			{
				case 1: flag = Flags.PREFILTER | Flags.MIP1; break;
				case 2: flag = Flags.PREFILTER | Flags.MIP2; break;
				case 3: flag = Flags.PREFILTER | Flags.MIP3; break;
				case 4: flag = Flags.PREFILTER | Flags.MIP4; break;
				case 5: flag = Flags.PREFILTER | Flags.MIP5; break;
			}

			device.PipelineState = factory[(int)flag];

			var data = new PARAMS();
			data.Roughness	=	roughness;
			data.MipLevel	=	mip;
			data.SourceSize	=	size * 2;
			data.TargetSize	=	size;
			cbuffer.SetData( ref data ); 

			device.ComputeSamplers	[ regLinearSampler	]	=	SamplerState.LinearWrap;
			device.ComputeResources	[ regSource			]	=	source;
			device.ComputeConstants	[ regTarget			]	=	cbuffer;
				
			device.SetComputeUnorderedAccess( 0, target );

			int tgx		=	MathUtil.IntDivRoundUp( size, BlockSize );
			int tgy		=	MathUtil.IntDivRoundUp( size, BlockSize );
			int tgz		=	1;

			device.Dispatch( tgx, tgy, tgz );
		}
	}
}
