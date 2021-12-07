using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Engine.Graphics.Scenes;
using System.Runtime.InteropServices;
using Fusion.Build.Mapping;
using Fusion.Core.Collection;

namespace Fusion.Engine.Graphics.GUI
{
	[RequireShader("gui", true)]
	public partial class GuiRenderer : RenderComponent
	{
		[ShaderDefine]	const int MaxGuiWidth	=	1280;
		[ShaderDefine]	const int MaxGuiHeight	=	720;

		static FXConstantBuffer<GpuData.CAMERA>		regCamera			=	new CRegister( 0, "Camera"			);
		static FXConstantBuffer<GUI_DATA>			regGUIData			=	new CRegister( 1, "GUIData"			);
		static FXSamplerState						regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXSamplerState						regSamplerPoint		=	new SRegister( 1, "PointSampler"	);
		static FXTexture2D<Vector4>					regGuiTexture		=	new TRegister( 0, "GuiTexture"		);
		static FXTexture2D<Vector4>					regNoiseTexture		=	new TRegister( 1, "NoiseTexture"	);
		static FXTexture2D<Vector4>					regRgbTexture		=	new TRegister( 2, "RgbTexture"		);
		static FXTexture2D<Vector4>					regInterference		=	new TRegister( 3, "Interference"	);
		static FXTexture2D<Vector4>					regInterlace		=	new TRegister( 4, "Interlace"		);
		static FXTexture2D<Vector4>					regGlitchTexture	=	new TRegister( 5, "GlitchTexture"	);

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=128)]
		struct GUI_DATA
		{
			public Matrix	WorldTransform;
			public Vector4	Size;
			public float	DotsPerUnit;
			public float	Intensity;
			public uint		FrameCounter;
			public uint		GlitchSeed;
		}


		const int MaxGUIs = 16;
		const int SpriteCapasity = 1024;

		RenderTarget2D guiTarget;
		SpriteLayer spriteLayer;

		public IList<Gui> Guis { get { return guis; } }
		List<Gui> guis = new List<Gui>();

		Ubershader shader;
		StateFactory factory;
		ConstantBuffer cbGUIData;

		DiscTexture		rgbTexture;
		DiscTexture[]	noiseTexture;
		DiscTexture		interlaceTexture;
		DiscTexture		glitchTexture;
		DiscTexture		cursorTexture;

		uint frameCounter = 0;

		enum Flags
		{
			DEFAULT,
		}
		

		public GuiRenderer( RenderSystem rs ) : base( rs )
		{
		}


		public override void Initialize()
		{
			guiTarget	=	new RenderTarget2D(rs.Device, ColorFormat.Rgba8_sRGB, MaxGuiWidth, MaxGuiHeight, true, false );
			spriteLayer	=	new SpriteLayer( rs, SpriteCapasity );
			cbGUIData	=	new ConstantBuffer( rs.Device, typeof(GUI_DATA) );

			LoadContent();

			Game.Reloading += (e,s) => LoadContent();
		}


		void LoadContent()
		{
			shader	=	Game.Content.Load<Ubershader>("gui");
			factory	=	shader.CreateFactory(typeof(Flags), this);

			Game.Content.TryLoad(@"misc\rgb", out rgbTexture);
			Game.Content.TryLoad(@"misc\interlace", out interlaceTexture);
			Game.Content.TryLoad(@"misc\glitch", out glitchTexture );
			Game.Content.TryLoad(@"misc\cursor", out cursorTexture );


			noiseTexture	=	new DiscTexture[8];
			for (int i=0; i<8; i++) {
				noiseTexture[i]	=	Game.Content.Load<DiscTexture>(@"noise\anim\LDR_LLL1_" + i.ToString());
			}

		}

		
		public override bool ProvideState( PipelineState ps, int flags )
		{
			ps.BlendState			=	BlendState.AlphaBlend;
			ps.RasterizerState		=	RasterizerState.CullNone;
			ps.DepthStencilState	=	DepthStencilState.Readonly;
			ps.Primitive			=	Primitive.TriangleList;
			ps.VertexInputElements	=	VertexColorTexture.Elements;

			return true;
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref spriteLayer );
				SafeDispose( ref guiTarget );
				SafeDispose( ref cbGUIData );
			}

			base.Dispose( disposing );
		}



		internal void DrawGUIs( GameTime gameTime, Camera camera, HdrFrame hdrFrame )
		{
			frameCounter++;

			var ui		=	Game.GetService<FrameProcessor>();
			var device	=	rs.Device;
			var guiData	=	new GUI_DATA();

			UpdateGuiLodAndVisibility(camera);

			using ( new PixEvent( "InGame GUI" ) )
			{
				foreach ( var gui in Guis )
				{
					if (gui.Root==null || !gui.Visible) 
					{
						continue;
					}

					gui.UpdateGlitch(gameTime);

					var w	=	gui.Root.Width;
					var h	=	gui.Root.Height;

					using ( new PixEvent( "UI Pass" ) )
					{
						spriteLayer.Clear();
						spriteLayer.Projection	=	Matrix.OrthoOffCenterRH( 0, w, h, 0, -1, 1 );
						spriteLayer.BlendMode	=	SpriteBlendMode.AlphaBlend;

						var dstRect = new Rectangle( 0, 0, w, h );

						Frame.DrawNonRecursive( gui.Root, gameTime, spriteLayer );

						DrawCursor( spriteLayer, gui.UI );

						rs.SpriteEngine.DrawSpriteLayer( spriteLayer, spriteLayer.Projection, guiTarget.Surface, dstRect );
					}

					device.ResetStates();

					guiTarget.BuildMipmaps();

					guiData.WorldTransform	=	gui.Transform;
					guiData.Intensity		=	1.0f;
					guiData.Size			=	new Vector4( w,h, 1.0f/w, 1.0f/h );
					guiData.DotsPerUnit		=	gui.DotsPerUnit;
					guiData.FrameCounter	=	frameCounter;
					guiData.GlitchSeed		=	gui.GlitchSeed;

					cbGUIData.SetData( guiData );

					device.PipelineState					=	factory[ (int)Flags.DEFAULT ];
					device.GfxConstants[ regCamera ]		=	camera.CameraData;
					device.GfxConstants[ regGUIData ]		=	cbGUIData;
					device.GfxSamplers[ regSamplerLinear ]	=	SamplerState.LinearWrap;
					device.GfxSamplers[ regSamplerPoint ]	=	SamplerState.PointWrap;

					device.GfxResources[ regGuiTexture ]	=	guiTarget;
					device.GfxResources[ regRgbTexture ]	=	rgbTexture.Srv;
					device.GfxResources[ regNoiseTexture ]	=	noiseTexture[ frameCounter%8 ].Srv;
					device.GfxResources[ regInterlace ]		=	interlaceTexture.Srv;
					device.GfxResources[ regGlitchTexture ] =	glitchTexture.Srv;

					device.SetTargets( hdrFrame.DepthBuffer, hdrFrame.HdrTarget );
					device.SetViewport( hdrFrame.HdrTarget.Bounds );
					device.SetScissorRect( hdrFrame.HdrTarget.Bounds );

					device.Draw(6,0);
				}
			}
		}
	

		void DrawCursor( SpriteLayer spriteLayer, UIState ui )
		{
			if (ui.ShowCursor)
			{
				spriteLayer.Draw( cursorTexture, ui.MousePosition.X, ui.MousePosition.Y, cursorTexture.Width, cursorTexture.Height, Color.White );
			}
		}

		
		void UpdateGuiLodAndVisibility( Camera camera )
		{
			var frustum = camera.Frustum;

			foreach ( var gui in guis )
			{
				var bbox		=	gui.ComputeBounds();
				gui.Visible		=	frustum.Contains( bbox ) != ContainmentType.Disjoint;

				var size		=	bbox.Size().Length() + 0.01f;
				var distance	=	Math.Max( 0, Vector3.Distance( camera.CameraPosition, gui.Transform.TranslationVector ) - size );

				//gui.Lod			=	(int)Math.Log( 1 + distance / size, 2 );
				//gui.Root.Text	=	string.Format("LOD: {0}", gui.Lod );
			}
		}
	}
}
