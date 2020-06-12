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
	public class Sky2 : RenderComponent 
	{
		[ShaderDefine] const uint BLOCK_SIZE	=	8;
		[ShaderDefine] const uint LUT_WIDTH		=	128;
		[ShaderDefine] const uint LUT_HEIGHT	=	128;

		public Color4	BetaRayleigh		{ get { return new Color4( 3.8e-6f, 13.5e-6f, 33.1e-6f, 0 ); } }	
		public Color4	BetaMie				{ get { return new Color4( 21e-6f ); } }

		[Config]	
		[AECategory("Sun")]
		[AEValueRange(-90, 90, 5, 0.1f)]
		public float SunAltitude { get; set; } = 45;

		[Config]	
		[AECategory("Sun")]
		[AEValueRange(-180, 180, 5, 0.1f)]
		public float SunAzimuth { get; set; } = 45;

		[Config]	
		[AECategory("Sun")]
		[AEDisplayName("Sun Intensity (Ev)")]
		[AEValueRange(0, 10, 1, 0.01f)]
		public float SunIntensityEv { get; set; } = 8;

		[Config]	
		[AECategory("Sun")]
		[AEValueRange(1500, 27000, 100, 1)]
		public float SunTemperature { get; set; } = 5700;

		[Config]	
		[AECategory("Atmosphere")]
		[AEValueRange(500, 12000, 500, 10)]
		public float PlanetRadius { get; set; } = 6360;
		
		[Config]	
		[AEValueRange(0, 200, 10, 1)]
		[AECategory("Atmosphere")]
		public float AtmosphereHeight { get; set; } = 80;
		
		[Config]	
		[AEValueRange(1, 10000, 500, 10)]
		[AECategory("Atmosphere")]
		public float RayleighHeight { get; set; } = 8000;
		
		[Config]	
		[AEValueRange(1, 2000, 500, 10)]
		[AECategory("Atmosphere")]
		public float MieHeight { get; set; } = 1200;
		
		[Config]	
		[AEValueRange(0, 200000, 1000, 10)]
		public float ViewHeight { get; set; } = 0;

		[Config]	
		[AECategory("Atmosphere")]
		[AEValueRange(-0.95f, 0.95f, 0.05f, 0.01f)]
		public float MieExcentricity { get; set; } = 0.76f;
		
		[Config]	
		[AECategory("Atmosphere")]
		[AEDisplayName("Sky Exposure (Ev)")]
		[AEValueRange(-8, 8, 1, 0.1f)]
		public float SkyExposure { get; set; } = 0;

		[Config]	
		[AEValueRange(1000,5000,500,1)]
		public float SkySphereSize { get; set; } = 10f;

		[Config]	
		[AECategory("Tweaks")]
		[AEValueRange(-8, 8, 1, 0.1f)]
		public float MieScale { get; set; } = 0;
		
		[Config]	
		[AECategory("Tweaks")]
		[AEValueRange(-8, 8, 1, 0.1f)]
		public float RayleighScale { get; set; } = 0;

		[AECategory("Debug")]
		public bool ShowLut { get; set; } = false;

		[AECommand]
		public void Earth()
		{
			//SunAltitude			= 45;
			//SunAzimuth			= 45;
			SunIntensityEv		= 8;
			SunTemperature		= 5700;
			PlanetRadius		= 6360;
			AtmosphereHeight	= 80;
			RayleighHeight		= 8000;
			MieHeight			= 1200;
			ViewHeight			= 0;
			MieExcentricity		= 0.76f;
			SkySphereSize		= 10f;
			MieScale			= 0;
			RayleighScale		= 0;
			SkyExposure			= 0;
		}


		static FXConstantBuffer<SKY_DATA>						regSky				=	new CRegister( 0, "Sky"			);
		static FXConstantBuffer<GpuData.CAMERA>					regCamera			=	new CRegister( 1, "Camera"		);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>			regDirectLight		=	new CRegister( 2, "DirectLight"	);

		[ShaderIfDef("LUT")] static FXRWTexture2D<Vector4>		regLutUav			=	new URegister( 0, "LutUav"		);
		static FXTexture2D<Vector4>								regLut				=	new TRegister( 0, "Lut"			);

		static FXSamplerState		regLinearClamp	 =	new SRegister(0, "LinearClamp" );
		static FXSamplerState		regPointClamp	=	new SRegister(1, "PointClamp" );
		
		[Flags]
		enum Flags : int
		{
			SKY		= 1 << 0,
			FOG		= 1 << 1,
			LUT		= 1 << 2,
		}

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Size=160)]
		struct SKY_DATA 
		{
			public Color4	BetaRayleigh;	
			public Color4	BetaMie;

			public Color4	SunIntensity;

			public float	SunAzimuth;
			public float	SunAltitude;

			public float 	PlanetRadius;
			public float	AtmosphereRadius;
			public float	RayleighHeight;
			public float	MieHeight;

			public float	MieExcentricity;
			public float	SkySphereSize;
			public float	ViewHeight;
			public float	SkyExposure;
		}


		struct SkyVertex {
			[Vertex("POSITION")]
			public Vector4 Vertex;
		}

		VertexBuffer	skyVB;
		Ubershader		sky;
		ConstantBuffer	cbSky;
		SKY_DATA		skyData;
		StateFactory	factory;
		Camera			cubeCamera;
		SamplerState	skyLutSampler;

		RenderTarget2D	lutSky;

		public Vector3	SkyAmbientLevel { get; protected set; }

		internal RenderTargetCube	SkyCube { get { return skyCube; } }
		RenderTargetCube			skyCube;



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
		/// <returns></returns>
		public Vector3 GetSunDirection()
		{
			float	cosAlt	=	(float)Math.Cos( MathUtil.DegreesToRadians( SunAltitude ) );
			float	sinAlt	=	(float)Math.Sin( MathUtil.DegreesToRadians( SunAltitude ) );

			float	cosAz	=	(float)Math.Cos( MathUtil.DegreesToRadians( SunAzimuth ) );
			float	sinAz	=	(float)Math.Sin( MathUtil.DegreesToRadians( SunAzimuth ) );

			float	x		=	 sinAz * cosAlt;
			float	y		=	 sinAlt;
			float	z		=	-cosAz * cosAlt;

			return new Vector3( x, y, z );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color4 GetSunIntensity()
		{
			float	scale	=	MathUtil.Exp2( SunIntensityEv );
			Color4	color	=	Temperature.GetColor( (int)SunTemperature );
			return 	color * scale;
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			skyData		=	new SKY_DATA();
			skyCube		=	new RenderTargetCube( device, ColorFormat.Rgba16F, 32, 0 );
			cbSky		=	new ConstantBuffer( device, typeof(SKY_DATA) );
			cubeCamera	=	new Camera( rs, "SkyCubeCamera" );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();

			lutSky			=	new RenderTarget2D( device, ColorFormat.Rgba16F, (int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );

			var skySphere	=	SkySphere.GetVertices(5).Select( v => new SkyVertex{ Vertex = v } ).ToArray();
			skyVB			=	new VertexBuffer( Game.GraphicsDevice, typeof(SkyVertex), skySphere.Length );
			skyVB.SetData( skySphere );

			skyLutSampler	=	new SamplerState();
			skyLutSampler.Filter	=	Drivers.Graphics.Filter.MinMagMipLinear;
			skyLutSampler.AddressU	=	Drivers.Graphics.AddressMode.Wrap;
			skyLutSampler.AddressV	=	Drivers.Graphics.AddressMode.Clamp;
			skyLutSampler.AddressW	=	Drivers.Graphics.AddressMode.Wrap;
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
			ps.DepthStencilState	=	flags.HasFlag(Flags.FOG) ? DepthStencilState.None : DepthStencilState.None;
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
				SafeDispose( ref lutSky );
				SafeDispose( ref skyLutSampler );
			}
			base.Dispose( disposing );
		}



		void Setup ( Flags flags, Camera camera, Rectangle viewport )
		{
			device.SetViewport( viewport );
			device.SetScissorRect( viewport );

			skyData.BetaRayleigh		=	BetaRayleigh * MathUtil.Exp2( RayleighScale );	
			skyData.BetaMie				=	BetaMie		 * MathUtil.Exp2( MieScale );
			skyData.PlanetRadius		=	( PlanetRadius ) * 1000	;
			skyData.AtmosphereRadius	=	( PlanetRadius + AtmosphereHeight ) * 1000;
			skyData.RayleighHeight		=	RayleighHeight;
			skyData.MieHeight			=	MieHeight;
			skyData.MieExcentricity		=	MieExcentricity;
			skyData.SkySphereSize		=	SkySphereSize;
			skyData.ViewHeight			=	ViewHeight;
			skyData.SunIntensity		=	new Color4(1,1,1,1) * MathUtil.Exp2( SunIntensityEv );
			skyData.SunAltitude			=	MathUtil.DegreesToRadians( SunAltitude );
			skyData.SunAzimuth			=	MathUtil.DegreesToRadians( SunAzimuth );
			skyData.SkyExposure			=	MathUtil.Exp2( SkyExposure );

			cbSky.SetData( skyData );

			device.GfxConstants		[ regSky			]	=	cbSky;
			device.GfxConstants		[ regCamera			]	=	camera.CameraData;
			device.GfxConstants		[ regDirectLight	]	=	rs.LightManager.DirectLightData;

			device.GfxSamplers		[ regLinearClamp	]	=	skyLutSampler;
			device.ComputeSamplers	[ regLinearClamp	]	=	skyLutSampler;
			device.GfxSamplers		[ regPointClamp		]	=	SamplerState.PointClamp;
			device.ComputeSamplers	[ regPointClamp		]	=	SamplerState.PointClamp;

			device.ComputeConstants	[ regSky			]	=	cbSky;
			device.ComputeConstants	[ regCamera			]	=	camera.CameraData;
			device.ComputeConstants	[ regDirectLight	]	=	rs.LightManager.DirectLightData;

			device.PipelineState	=	factory[(int)flags];
		}



		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderSkyCube()
		{
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="stereoEye"></param>
		/// <param name="frame"></param>
		/// <param name="settings"></param>
		internal void Render( Camera camera, StereoEye stereoEye, HdrFrame frame )
		{
			Render( camera, stereoEye, frame.HdrTarget.Surface );

			if (ShowLut)
			{
				rs.Filter.StretchRect( frame.HdrTarget.Surface, lutSky );
			}
		}


		/// <summary>
		/// Renders sky with specified technique
		/// </summary>
		/// <param name="rendCtxt"></param>
		/// <param name="techName"></param>
		internal void Render( Camera camera, StereoEye stereoEye, RenderTargetSurface color, bool noSun = false )
		{
			using ( new PixEvent("Sky Rendering") ) 
			{
				using ( new PixEvent( "Lut" ) )
				{
					device.ResetStates();

					Setup( Flags.LUT, camera, color.Bounds );

					device.SetComputeUnorderedAccess( regLutUav, lutSky.Surface.UnorderedAccess );

					uint tgx	=	MathUtil.IntDivRoundUp( LUT_WIDTH,	BLOCK_SIZE );
					uint tgy	=	MathUtil.IntDivRoundUp( LUT_HEIGHT, BLOCK_SIZE );
					uint tgz	=	MathUtil.IntDivRoundUp( LUT_WIDTH,	BLOCK_SIZE );

					device.Dispatch( tgx, tgy, tgz );
				}


				using ( new PixEvent( "Sky" ) )
				{
					device.ResetStates();

					device.SetTargets( null, color );
					device.GfxResources[ regLut ] = lutSky;

					Setup( Flags.SKY, camera, color.Bounds );

					device.SetupVertexInput( skyVB, null );
					device.Draw( skyVB.Capacity, 0 );
				}


				using ( new PixEvent( "SkyCube" ) )
				{
					device.ResetStates();

					for( int i = 0; i < 6; ++i ) 
					{
						cubeCamera.SetupCameraCubeFaceLH( Vector3.Zero, (CubeFace)i, 0.125f, 10000 );

						device.SetTargets( null, SkyCube.GetSurface(0, (CubeFace)i ) );
						device.GfxResources[ regLut ] = lutSky;

						Setup( Flags.FOG, cubeCamera, new Rectangle( 0, 0, SkyCube.Width, SkyCube.Height ) );

						device.SetupVertexInput( skyVB, null );
						device.Draw( skyVB.Capacity, 0 );
					}
				}
			}
		}
	}
}
