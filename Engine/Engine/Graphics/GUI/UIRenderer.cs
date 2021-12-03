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

namespace Fusion.Engine.Graphics.GUI
{
	[RequireShader("gui", true)]
	public class UIRenderer : RenderComponent
	{
		static FXConstantBuffer<GpuData.CAMERA>		regCamera			=	new CRegister( 0, "Camera"		);
		static FXConstantBuffer<GUI_DATA>			regGUIData			=	new CRegister( 1, "GUIData"		);
		static FXSamplerState						regSamplerLinear	=	new SRegister( 0, "LinearSampler"	);
		static FXTexture2D<Vector4>					regTexture			=	new TRegister( 0, "Texture"		);

		[StructLayout(LayoutKind.Sequential, Pack=4, Size=128)]
		struct GUI_DATA
		{
			public Matrix	WorldTransform;
			public Vector4	Size;
			public float	Intensity;
		}


		const int MaxGUIs = 16;
		const int SpriteCapasity = 1024;

		RenderTarget2D[] guiTargets;
		SpriteLayer spriteLayer;


		public IList<UIScreen> UIScreens { get { return uiScreens; } }
		List<UIScreen> uiScreens = new List<UIScreen>();

		Ubershader shader;
		StateFactory factory;
		ConstantBuffer cbGUIData;

		enum Flags
		{
			DEFAULT,
		}
		

		public UIRenderer( RenderSystem rs ) : base( rs )
		{
		}


		public override void Initialize()
		{
			guiTargets	=	new RenderTarget2D[ MaxGUIs ];
			spriteLayer	=	new SpriteLayer( rs, SpriteCapasity );
			cbGUIData	=	new ConstantBuffer( rs.Device, typeof(GUI_DATA) );

			for (int i=0; i<MaxGUIs; i++)
			{
				guiTargets[i]	=	new RenderTarget2D(rs.Device, ColorFormat.Rgba8_sRGB, 640, 480 );
			}

			LoadContent();

			Game.Reloading += (e,s) => LoadContent();
		}


		void LoadContent()
		{
			shader	=	Game.Content.Load<Ubershader>("gui");
			factory	=	shader.CreateFactory(typeof(Flags), this);
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
				SafeDispose( guiTargets );
				SafeDispose( ref cbGUIData );
			}

			base.Dispose( disposing );
		}


		internal void DrawGUIs( GameTime gameTime, Camera camera, HdrFrame hdrFrame )
		{
			int count	=	Math.Min( MaxGUIs, uiScreens.Count );
			var ui		=	Game.GetService<FrameProcessor>();
			var device	=	rs.Device;

			using ( new PixEvent( "InGame GUI" ) )
			{
				using ( new PixEvent( "Off-Screen Pass" ) )
				{
					for ( int i = 0; i<count; i++ )
					{
						var gui	=	uiScreens[i].Root;
						var w	=	gui.Width;
						var h	=	gui.Height;

						spriteLayer.Clear();
						spriteLayer.Projection	=	Matrix.OrthoOffCenterRH( 0, w, h, 0, -1, 1 );
						spriteLayer.BlendMode	=	SpriteBlendMode.AlphaBlend;

						var dstRect = new Rectangle( 0, 0, w, h );

						Frame.DrawNonRecursive( gui, gameTime, spriteLayer );

						rs.SpriteEngine.DrawSpriteLayer( spriteLayer, spriteLayer.Projection, guiTargets[i].Surface, dstRect );
					}
				}

				device.ResetStates();

				using ( new PixEvent( "View Pass" ) )
				{
					for ( int i = 0; i<count; i++ )
					{
						var guiData	=	new GUI_DATA();

						guiData.WorldTransform	=	uiScreens[i].Transform;
						guiData.Intensity		=	1.0f;

						cbGUIData.SetData( guiData );

						device.PipelineState					=	factory[ (int)Flags.DEFAULT ];
						device.GfxConstants[ regCamera ]		=	camera.CameraData;
						device.GfxConstants[ regGUIData ]		=	cbGUIData;
						device.GfxSamplers[ regSamplerLinear ]	=	SamplerState.LinearWrap;
						device.GfxResources[ regTexture ]		=	guiTargets[i];

						device.SetTargets( hdrFrame.DepthBuffer, hdrFrame.HdrTarget );
						device.SetViewport( hdrFrame.HdrTarget.Bounds );
						device.SetScissorRect( hdrFrame.HdrTarget.Bounds );

						device.Draw(6,0);
					}
				}
			}
		}
	}
}
