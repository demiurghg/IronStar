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
using Fusion.Widgets.Advanced;

namespace Fusion.Engine.Graphics 
{
	[RequireShader("dof", true)]
	public class DofFilter : RenderComponent 
	{
		[ShaderDefine]
		const uint BokehShapeSize = 19;

		[Config]
		public bool Enabled { get; set; } = false;

		[Config]
		[AESlider(35, 70, 5f, 1f)]
		public float FilmFormat { get; set; } = 35;

		[Config]
		[AESlider(1.4f, 22, 0.1f, 0.01f)]
		public float FNumber { get; set; } = 1;

		[Config]
		[AESlider(0.5f, 100, 0.5f, 0.01f)]
		public float FocalDistance { get; set; } = 10;

		[Config]
		[AESlider(0, 360, 10f, 1f)]
		public float DiaphragmAngle { get; set; } = 15;


		[ShaderDefine]
		const uint BlockSize = 8;

		static FXConstantBuffer<GpuData.CAMERA>			regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<DOF_DATA>				regDOF				=	new CRegister( 1, "Dof"			);
		static FXConstantBuffer<Vector4>				regBokehShape		=	new CRegister( 2, (int)BokehShapeSize, "BokehShape"	);

		static FXSamplerState							regLinearClamp		=	new SRegister( 0, "LinearClamp"	);

		[ShaderIfDef("COMPUTE_COC")]	static FXTexture2D<float>				regDepthBuffer		=	new TRegister( 0, "DepthBuffer"		);
		[ShaderIfDef("COMPUTE_COC")]	static FXRWTexture2D<Vector4>			regCocTarget		=	new URegister( 0, "CocTarget"		);

		[ShaderIfDef("EXTRACT,BOKEH,COMPOSE")]	static FXTexture2D<Vector4>		regCocTexture		=	new TRegister( 0, "CocTexture"		);

		[ShaderIfDef("EXTRACT,COMPOSE")]		static FXTexture2D<Vector4>		regHdrSource		=	new TRegister( 1, "HdrSource"		);
		[ShaderIfDef("EXTRACT")]				static FXRWTexture2D<Vector4>	regBackground		=	new URegister( 0, "Background"		);
		[ShaderIfDef("EXTRACT")]				static FXRWTexture2D<Vector4>	regForeground		=	new URegister( 1, "Foreground"		);

		[ShaderIfDef("BOKEH")]					static FXTexture2D<Vector4>		regBokehSource		=	new TRegister( 2, "BokehSource"		);
		[ShaderIfDef("BOKEH")]					static FXRWTexture2D<Vector4>	regBokehTarget		=	new URegister( 0, "BokehTarget"		);

		[ShaderIfDef("COMPOSE")]				static FXTexture2D<Vector4>		regBokehBackground	=	new TRegister( 2, "BokehBackground"		);
		[ShaderIfDef("COMPOSE")]				static FXTexture2D<Vector4>		regBokehForeground	=	new TRegister( 3, "BokehForeground"		);
		[ShaderIfDef("COMPOSE")]				static FXRWTexture2D<Vector4>	regHdrTarget		=	new URegister( 0, "HdrTarget"			);



		
		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbDof;
		ConstantBuffer	cbBokehShape;
		Vector4[]		shapeData = new Vector4[BokehShapeSize];


		[StructLayout(LayoutKind.Sequential,Size = 64)]
		struct DOF_DATA {
			public	float	ApertureDiameter;
			public	float	FocalLength;
			public	float	FocalDistance;
			public	float	PixelDensity;
		}


		enum Flags 
		{	
			COMPUTE_COC		=	0x0001,
			EXTRACT			=	0x0002,
			BACKGROUND		=	0x0010,
			FOREGROUND		=	0x0020,
			BOKEH			=	0x0100,
			PASS1			=	0x0200,
			PASS2			=	0x0400,
			COMPOSE			=	0x1000,
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
			cbDof			=	new ConstantBuffer( Game.GraphicsDevice, typeof(DOF_DATA) );
			cbBokehShape	=	new ConstantBuffer( Game.GraphicsDevice, typeof(Vector4), (int)BokehShapeSize );

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
				SafeDispose( ref cbBokehShape );
			}

			base.Dispose( disposing );
		}


		void UpdateBokehShape( float angle )
		{
			shapeData[ 0]		=	new Vector4(  0,  0, 0, 0 );

			shapeData[ 1]		=	new Vector4(  2,  0, 0, 0 );
			shapeData[ 2]		=	new Vector4(  1,  2, 0, 0 );
			shapeData[ 3]		=	new Vector4( -1,  2, 0, 0 );
			shapeData[ 4]		=	new Vector4( -2,  0, 0, 0 );
			shapeData[ 5]		=	new Vector4( -1, -2, 0, 0 );
			shapeData[ 6]		=	new Vector4(  1, -2, 0, 0 );

			shapeData[ 7]		=	new Vector4(  4,  0, 0, 0 );
			shapeData[ 8]		=	new Vector4(  3,  2, 0, 0 );
			shapeData[ 9]		=	new Vector4(  2,  4, 0, 0 );
			shapeData[10]		=	new Vector4(  0,  4, 0, 0 );
			shapeData[11]		=	new Vector4( -2,  4, 0, 0 );
			shapeData[12]		=	new Vector4( -3,  2, 0, 0 );
			shapeData[13]		=	new Vector4( -4,  0, 0, 0 );
			shapeData[14]		=	new Vector4( -3, -2, 0, 0 );
			shapeData[15]		=	new Vector4( -2, -4, 0, 0 );
			shapeData[16]		=	new Vector4(  0, -4, 0, 0 );
			shapeData[17]		=	new Vector4(  2, -4, 0, 0 );
			shapeData[18]		=	new Vector4(  3, -2, 0, 0 );

			var transform		=	Matrix.Scaling( 0.25f, 0.2165f, 1 );// * Matrix.RotationZ( angle );

			for (int i=0; i<shapeData.Length; i++)
			{
				shapeData[i] = Vector4.Transform( shapeData[i], transform );
			}

			cbBokehShape.SetData( shapeData );
		}



		/// <summary>
		/// Applies DOF effect
		/// </summary>
		internal void RenderDof ( Camera camera, HdrFrame hdrFrame )
		{
			if (!Enabled) 
			{
				return;
			}

			using ( new PixEvent( "DOF" ) )
			{
				device.ResetStates();

				var width	=	hdrFrame.HdrTarget.Width;
				var height	=	hdrFrame.HdrTarget.Height;
				var dofData	=	new DOF_DATA();

				float focalLength			=	0.5f * FilmFormat * camera.ProjectionMatrix.M11;
				float focalDistance			=	FocalDistance;
				float apertureDiameter		=	focalLength / FNumber;

				dofData.FocalLength			=	focalLength;
				dofData.ApertureDiameter	=	apertureDiameter;
				dofData.FocalDistance		=	focalDistance;
				dofData.PixelDensity		=	width / FilmFormat;

				cbDof.SetData( ref dofData );

				UpdateBokehShape( MathUtil.DegreesToRadians( DiaphragmAngle ) );

				device.ComputeConstants	[ regCamera		]	=	camera.CameraData;
				device.ComputeConstants	[ regDOF		]	=	cbDof;
				device.ComputeConstants	[ regBokehShape	]	=	cbBokehShape;
				device.ComputeSamplers	[ regLinearClamp ]	=	SamplerState.LinearClamp;
			
				//	compute COC :
				device.SetComputeUnorderedAccess( regCocTarget,	hdrFrame.DofCOC.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( 1,			null );

				device.ComputeResources.Clear();
				device.ComputeResources[ regDepthBuffer ]		=	hdrFrame.DepthBuffer;

				ComputePass( Flags.COMPUTE_COC, width, height, 1 );
			
				//	extract layers :
				device.SetComputeUnorderedAccess( regBackground,	hdrFrame.DofBackground.Surface.UnorderedAccess );
				device.SetComputeUnorderedAccess( regForeground,	hdrFrame.DofForeground.Surface.UnorderedAccess );
	
				device.ComputeResources.Clear();
				device.ComputeResources[ regCocTexture ]		=	hdrFrame.DofCOC;
				device.ComputeResources[ regHdrSource ]		=	hdrFrame.HdrTarget;

				ComputePass( Flags.EXTRACT, width, height, 2 );

				using ( new PixEvent( "Bokeh" ) ) 
				{
					//	blur :
					BokehPass( Flags.BOKEH|Flags.PASS1, hdrFrame.DofBokehTemp, hdrFrame.DofBackground, hdrFrame.DofCOC );
					BokehPass( Flags.BOKEH|Flags.PASS2, hdrFrame.DofBackground, hdrFrame.DofBokehTemp, hdrFrame.DofCOC );
				}

				//	compose :
				hdrFrame.SwapHdrTargets();
			
				device.SetComputeUnorderedAccess( regHdrTarget,	hdrFrame.HdrTarget.Surface.UnorderedAccess );
	
				device.ComputeResources.Clear();
				device.ComputeResources[ regCocTexture		]	=	hdrFrame.DofCOC;
				device.ComputeResources[ regHdrSource		]	=	hdrFrame.HdrSource;
				device.ComputeResources[ regBokehBackground	]	=	hdrFrame.DofBackground;
				device.ComputeResources[ regBokehForeground	]	=	hdrFrame.DofForeground;

				ComputePass( Flags.COMPOSE, width, height, 1 );
			}
		}


		void BokehPass( Flags combination, RenderTarget2D target, ShaderResource source, ShaderResource coc )
		{
			device.ComputeResources.Clear();
			device.SetComputeUnorderedAccess( regBokehTarget,	target.Surface.UnorderedAccess );
			device.SetComputeUnorderedAccess( 1,				null );

			device.ComputeResources.Clear();
			device.ComputeResources[ regCocTexture ]	=	coc;
			device.ComputeResources[ regBokehSource ]	=	source;

			ComputePass( combination, target.Width, target.Height, 1 );
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
