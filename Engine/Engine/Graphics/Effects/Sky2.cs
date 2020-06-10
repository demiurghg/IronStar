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
using System.Diagnostics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	[RequireShader("sky2", true)]
	internal class Sky2 : RenderComponent 
	{
		public Color4	BetaRayleigh		{ get { return new Color4( 3.8e-6f, 13.5e-6f, 33.1e-6f, 0 ); } }	
		public Color4	BetaMie				{ get { return new Color4( 21e-6f ); } }

		[Config]	
		[AEValueRange(500, 12000, 500, 10)]
		public float PlanetRadius { get; set; } = 6360;
		
		[Config]	
		[AEValueRange(500, 12000, 500, 10)]
		public float AtmosphereRadius { get; set; } = 6420;
		
		[Config]	
		[AEValueRange(5000, 10000, 500, 10)]
		public float RayleighHeight { get; set; } = 7994;
		
		[Config]	
		[AEValueRange(100, 2000, 500, 10)]
		public float MieHeight { get; set; } = 1200;

		[Config]	
		[AEValueRange(-0.95f, 0.95f, 0.05f, 0.01f)]
		public float MieExcentricity { get; set; } = 0.76f;

		[Config]	
		[AEValueRange(1000,5000,500,1)]
		public float SkySphereSize { get; set; } = 3500f;

		[AECommand]
		public void Earth()
		{
			PlanetRadius		= 6360;
			AtmosphereRadius	= 6420;
			RayleighHeight		= 7994;
			MieHeight			= 1200;
			MieExcentricity		= 0.76f;
		}


		static FXConstantBuffer<SKY_DATA>					regSky						=	new CRegister( 0, "Sky"						);
		static FXConstantBuffer<GpuData.CAMERA>				regCamera					=	new CRegister( 1, "Camera"					);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight				=	new CRegister( 2, "DirectLight"				);
		
		[Flags]
		enum Flags : int
		{
			SKY		= 1 << 0,
			FOG		= 1 << 1,
		}

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Size=160)]
		struct SKY_DATA {
			public Color4	BetaRayleigh;	
			public Color4	BetaMie;

			public float 	PlanetRadius;
			public float	AtmosphereRadius;
			public float	RayleighHeight;
			public float	MieHeight;

			public float	MieExcentricity;
			public float	SkySphereSize;
		}


		struct SkyVertex {
			[Vertex("POSITION")]
			public Vector4 Vertex;
		}

		VertexBuffer	skyVB;
		Ubershader		sky;
		ConstantBuffer	cbSky;
		SKY_DATA		skyConstsData;
		StateFactory	factory;
		Camera			cubeCamera;

		public Vector3	SkyAmbientLevel { get; protected set; }

		public RenderTargetCube	SkyCube { get { return skyCube; } }
		RenderTargetCube		skyCube;



		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public Sky2 ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			skyCube		=	new RenderTargetCube( device, ColorFormat.Rgba16F, 32, 0 );
			cbSky		=	new ConstantBuffer( device, typeof(SKY_DATA) );
			cubeCamera	=	new Camera( rs, "SkyCubeCamera" );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();

			var skySphere	=	SkySphere.GetVertices(7).Select( v => new SkyVertex{ Vertex = v } ).ToArray();
			skyVB			=	new VertexBuffer( Game.GraphicsDevice, typeof(SkyVertex), skySphere.Length );
			skyVB.SetData( skySphere );
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			sky			=	Game.Content.Load<Ubershader>("sky2");
			factory		=	sky.CreateFactory( typeof(Flags), (ps,i) => EnumFunc(ps, (Flags)i) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void EnumFunc ( PipelineState ps, Flags flags )
		{
			ps.VertexInputElements	=	VertexInputElement.FromStructure<SkyVertex>();

			//	do not cull triangles for both for RH and LH coordinates 
			//	for direct view and cubemaps.
			ps.RasterizerState		=	RasterizerState.CullNone; 
			ps.BlendState			=	BlendState.Opaque;
			ps.DepthStencilState	=	flags.HasFlag(Flags.FOG) ? DepthStencilState.None : DepthStencilState.Readonly;
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) {
				SafeDispose( ref skyVB );
				SafeDispose( ref skyCube );
				SafeDispose( ref cbSky );
				SafeDispose( ref cubeCamera );
			}
			base.Dispose( disposing );
		}



		void Setup ( Flags flags, Camera camera, Rectangle viewport )
		{
			var skyData	=	new SKY_DATA();

			device.SetViewport( viewport );
			device.SetScissorRect( viewport );

			skyData.BetaRayleigh		=	BetaRayleigh			;	
			skyData.BetaMie				=	BetaMie					;
			skyData.PlanetRadius		=	PlanetRadius	 * 1000	;
			skyData.AtmosphereRadius	=	AtmosphereRadius * 1000	;
			skyData.RayleighHeight		=	RayleighHeight			;
			skyData.MieHeight			=	MieHeight				;
			skyData.MieExcentricity		=	MieExcentricity			;
			skyData.SkySphereSize		=	SkySphereSize			;

			cbSky.SetData( skyData );

			device.GfxConstants	[ regSky			]	=	cbSky;
			device.GfxConstants	[ regCamera			]	=	camera.CameraData;
			device.GfxConstants	[ regDirectLight	]	=	rs.LightManager.DirectLightData;

			device.PipelineState	=	factory[(int)flags];
		}



		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderSkyCube( SkySettings settings )
		{
			using ( new PixEvent("Fog Table") ) 
			{
				device.ResetStates();

				for( int i = 0; i < 6; ++i ) 
				{
					cubeCamera.SetupCameraCubeFaceLH( Vector3.Zero, (CubeFace)i, 0.125f, 10000 );

					device.SetTargets( null, SkyCube.GetSurface(0, (CubeFace)i ) );

					Setup( Flags.FOG, cubeCamera, new Rectangle( 0, 0, SkyCube.Width, SkyCube.Height ) );

					device.SetupVertexInput( skyVB, null );
					device.Draw( skyVB.Capacity, 0 );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="stereoEye"></param>
		/// <param name="frame"></param>
		/// <param name="settings"></param>
		internal void Render( Camera camera, StereoEye stereoEye, HdrFrame frame, SkySettings settings )
		{
			Render( camera, stereoEye, frame.DepthBuffer.Surface, frame.HdrTarget.Surface, settings );
		}


		/// <summary>
		/// Renders sky with specified technique
		/// </summary>
		/// <param name="rendCtxt"></param>
		/// <param name="techName"></param>
		internal void Render( Camera camera, StereoEye stereoEye, DepthStencilSurface depth, RenderTargetSurface color, SkySettings settings, bool noSun = false )
		{
			using ( new PixEvent("Sky Rendering") ) 
			{
				device.ResetStates();

				device.SetTargets( depth, color );

				Setup( Flags.SKY, camera, color.Bounds );

				device.SetupVertexInput( skyVB, null );
				device.Draw( skyVB.Capacity, 0 );
			}
		}
	}
}
