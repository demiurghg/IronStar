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
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics 
{
	[RequireShader("dof", true)]
	internal class DofFilter : RenderComponent 
	{
		[Config]
		public bool Enabled { get; set; } = false;

		[Config]
		[AEValueRange(0, 8, 0.5f, 0.01f)]
		public float Aperture { get; set; } = 1;

		[Config]
		[AEValueRange(0.5f, 100, 0.5f, 0.01f)]
		public float FocalDistance { get; set; } = 10;


		[ShaderDefine]
		const uint BlockSize = 8;

		static FXConstantBuffer<GpuData.CAMERA>			regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<DOF_DATA>				regDOF				=	new CRegister( 1, "Dof"			);

		static FXSamplerState							regLinearClamp		=	new SRegister( 0, "LinearClamp"	);

		[ShaderIfDef("COMPUTE_COC")]	static FXTexture2D<float>				regDepthBuffer		=	new TRegister( 0, "DepthBuffer"		);
		[ShaderIfDef("COMPUTE_COC")]	static FXRWTexture2D<Vector4>			regCocTarget		=	new URegister( 0, "CocTarget"		);

		[ShaderIfDef("EXTRACT,BLUR,COMPOSE")]	static FXTexture2D<Vector4>		regCocTexture		=	new TRegister( 0, "CocTexture"		);

		[ShaderIfDef("EXTRACT,COMPOSE")]		static FXTexture2D<Vector4>		regHdrSource		=	new TRegister( 1, "HdrSource"		);
		[ShaderIfDef("EXTRACT")]				static FXRWTexture2D<Vector4>	regBackground		=	new URegister( 0, "Background"		);
		[ShaderIfDef("EXTRACT")]				static FXRWTexture2D<Vector4>	regForeground		=	new URegister( 1, "Foreground"		);

		[ShaderIfDef("BLUR")]					static FXTexture2D<Vector4>		regBokehSource		=	new TRegister( 2, "BokehSource"		);
		[ShaderIfDef("BLUR")]					static FXRWTexture2D<Vector4>	regBokehTarget		=	new URegister( 1, "BokehTarget"		);

		[ShaderIfDef("COMPOSE")]				static FXTexture2D<Vector4>		regBokehBackground	=	new TRegister( 2, "BokehBackground"		);
		[ShaderIfDef("COMPOSE")]				static FXTexture2D<Vector4>		regBokehForeground	=	new TRegister( 3, "BokehForeground"		);
		[ShaderIfDef("COMPOSE")]				static FXRWTexture2D<Vector4>	regHdrTarget		=	new URegister( 0, "HdrTarget"			);



		
		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbDof;


		[StructLayout(LayoutKind.Sequential,Size = 64)]
		struct DOF_DATA {
			public	float	Aperture;
			public	float	FocalDistance;
		}


		enum Flags 
		{	
			COMPUTE_COC		=	0x0001,
			BLUR			=	0x0002,
			EXTRACT			=	0x0004,
			BACKGROUND		=	0x0008,
			FOREGROUND		=	0x0010,
			APPLY_DOF		=	0x0020,
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public DofFilter ( RenderSystem rs ) : base(rs)
		{
		}



		/// <summary>
		/// /
		/// </summary>
		public override void Initialize ()
		{
			cbDof	=	new ConstantBuffer( Game.GraphicsDevice, typeof(DOF_DATA) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader	=	Game.Content.Load<Ubershader>("dof");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref cbDof );
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// Applies DOF effect
		/// </summary>
		public void RenderDof ( HdrFrame hdrFrame )
		{
			if (!Enabled) 
			{
				return;
			}

			using ( new PixEvent( "DOF" ) )
			{
				device.ResetStates();

				var width	=	hdrFrame.HdrBuffer.Width;
				var height	=	hdrFrame.HdrBuffer.Height;
				var dofData	=	new DOF_DATA();

				dofData.Aperture		=	Aperture;
				dofData.FocalDistance	=	FocalDistance;

				cbDof.SetData( ref dofData );

				device.ComputeSamplers[ regLinearClamp ]	=	SamplerState.LinearClamp;
			
				//	compute COC :
				device.SetComputeUnorderedAccess( regCocTarget,			hdrFrame.DofCOC.Surface.UnorderedAccess );
				device.ComputeResources			[ regDepthBuffer ] =	hdrFrame.DepthBuffer;

				ComputePass( Flags.COMPUTE_COC, width, height, 1 );
			}
		}


		void ComputePass( Flags combination, int width, int height, int divider )
		{
			int tgx		=	MathUtil.IntDivRoundUp( MathUtil.IntDivRoundUp( width , divider ), (int)BlockSize );	
			int tgy		=	MathUtil.IntDivRoundUp( MathUtil.IntDivRoundUp( height, divider ), (int)BlockSize );	
			int tgz		=	1;

			device.PipelineState	=	factory[ (int)combination ];

			device.Dispatch( tgx, tgy, tgz );
		}
	}
}
