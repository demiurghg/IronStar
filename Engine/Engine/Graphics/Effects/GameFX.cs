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
using Fusion.Engine.Graphics;
using Fusion.Engine.Imaging;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core.Shell;


namespace Fusion.Engine.Graphics 
{
	[RequireShader("gamefx", true)]
	public class GameFX : RenderComponent 
	{
		const float PainDecayFactor	=	0.2f;

		static FXConstantBuffer<PARAMS> regParams		=	new CRegister( 0, "Params" );

		static FXTexture2D<Vector4>		regSource		=	new TRegister( 0, "Source" );
		static FXTexture2D<Vector4>		regCloudTex		=	new TRegister( 1, "CloudTex" );
		static FXTexture2D<Vector4>		regPainTex		=	new TRegister( 2, "PainTex" );

		static FXSamplerState			regLinearClamp	=	new SRegister( 0, "LinearClamp" );
		static FXSamplerState			regLinearWrap	=	new SRegister( 1, "LinearWrap" );

		readonly Random rand = new Random();
		
		Ubershader	shader;
		ConstantBuffer	paramsCB;
		StateFactory	factory;
		DiscTexture		cloudTex;
		DiscTexture		painTex;

		//	float AdaptationRate;          // Offset:    0
		//	float LuminanceLowBound;       // Offset:    4
		//	float LuminanceHighBound;      // Offset:    8
		//	float KeyValue;                // Offset:   12
		//	float BloomAmount;             // Offset:   16
		[StructLayout(LayoutKind.Sequential, Size=128)]
		[ShaderStructure]
		struct PARAMS {
			public	float	Time;
			public	float	Random;
			public	float	PainAmount;
			public	float 	DeathFactor;
		}


		[ShaderDefine]
		const int NoiseSizeX		=	64;


		enum Flags {	
			PAIN		=	0x001,
			DEATH		=	0x002,
		}


		float painFXFactor	=	0;
		bool deathFXRunning = false;
		float deathFXFactor;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public GameFX ( RenderSystem rs ) : base(rs)
		{
		}



		/// <summary>
		/// /
		/// </summary>
		public override void Initialize ()
		{
			paramsCB	=	new ConstantBuffer( Game.GraphicsDevice, typeof(PARAMS) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		public void ClearFX()
		{
			painFXFactor		=	0;
			deathFXRunning	=	false;
			deathFXFactor	=	0;
		}

		public void RunPainFX( float factor )
		{
			painFXFactor	=	MathUtil.Clamp( painFXFactor + factor, 0, 1 );
		}


		public void RunDeathFX()
		{
			deathFXRunning	=	true;
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("gamefx");
			factory		=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexInputElement.Empty, BlendState.Opaque, RasterizerState.CullNone, DepthStencilState.None );

			cloudTex	=	Game.Content.Load<DiscTexture>(@"noise\cloud");
			painTex		=	Game.Content.Load<DiscTexture>(@"noise\pain");
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref paramsCB	 );
			}

			base.Dispose( disposing );
		}


		uint frameCounter	=	0;


		/// <summary>
		/// Performs luminance measurement, tonemapping, applies bloom.
		/// </summary>
		/// <param name="target">LDR target.</param>
		/// <param name="hdrImage">HDR source image.</param>
		public bool Apply ( GameTime gameTime, RenderTarget2D target, RenderTarget2D source )
		{
			frameCounter++;

			var device	=	Game.GraphicsDevice;
			var filter	=	Game.RenderSystem.Filter;
			var blur	=	Game.RenderSystem.Blur;

			int imageWidth	=	target.Width;
			int imageHeight	=	target.Height;

			//
			//	Update state :
			//
			painFXFactor *= (float)Math.Pow( PainDecayFactor, gameTime.ElapsedSec );

			if (deathFXRunning)
			{
				deathFXFactor += gameTime.ElapsedSec;
			}

			using ( new PixEvent("GameFX") ) 
			{
				//
				//	Setup parameters :
				//
				var paramsData	=	new PARAMS();
				paramsData.Time				=	(float)gameTime.Current.TotalSeconds;
				paramsData.Random			=	MathUtil.Random.NextFloat(0,1);
				paramsData.PainAmount		=	painFXFactor;
				paramsData.DeathFactor		=	deathFXFactor;

				paramsCB.SetData( ref paramsData );
				device.GfxConstants[0]		=	paramsCB;

				//
				//	Tonemap and compose :
				//
				device.SetTargets( null, target );
				device.SetViewport( target.Bounds );
				device.SetScissorRect( target.Bounds );

				device.GfxSamplers[ regLinearClamp	]	=	SamplerState.LinearClamp;
				device.GfxSamplers[ regLinearWrap	]	=	SamplerState.LinearWrap;

				device.GfxResources[ regSource		]	=	source;
				device.GfxResources[ regPainTex		]	=	painTex.Srv;
				device.GfxResources[ regCloudTex	]	=	cloudTex.Srv;

				Flags flags = Flags.PAIN;

				device.PipelineState		=	factory[ (int)(flags) ];
				
				device.Draw( 3, 0 );
			
				device.ResetStates();
			}

			return true;
		}
	}
}
