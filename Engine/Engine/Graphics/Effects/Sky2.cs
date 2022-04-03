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
using Fusion.Engine.Imaging;
using Fusion.Widgets.Advanced;

namespace Fusion.Engine.Graphics {

	[ConfigClass]
	[RequireShader("sky2", true)]
	public partial class Sky2 : RenderComponent 
	{
		[ShaderDefine] const uint BLOCK_SIZE	=	16;
		[ShaderDefine] const uint LUT_WIDTH		=	128;
		[ShaderDefine] const uint LUT_HEIGHT	=	128;

		[ShaderDefine] const uint AP_WIDTH		=	32;
		[ShaderDefine] const uint AP_HEIGHT		=	32;
		[ShaderDefine] const uint AP_DEPTH		=	16;

		public static Color4	BetaRayleigh		{ get { return new Color4( 3.8e-6f, 13.5e-6f, 33.1e-6f, 0 ); } }	
		public static Color4	BetaMie				{ get { return new Color4( 21e-6f ); } }

		[Config]	
		[AECategory("Sun")]
		[AESlider(-90, 90, 5, 0.1f)]
		static public float SunAltitude { get; set; } = 45;

		[Config]	
		[AECategory("Sun")]
		[AESlider(-180, 180, 5, 0.1f)]
		static public float SunAzimuth { get; set; } = 45;

		[Config]	
		[AECategory("Sun")]
		[AEDisplayName("Sun Intensity (Ev)")]
		[AESlider(0, 10, 1, 0.01f)]
		static public float SunIntensityEv { get; set; } = 8;

		[Config]	
		[AECategory("Sun")]
		[AESlider(1500, 27000, 100, 1)]
		static public float SunTemperature { get; set; } = 5900;

		[Config]	
		[AECategory("Sun")]
		[AESlider(-5, 16, 1, 1)]
		static public float SunBrightnessEv { get; set; } = 12;

		[Config]	
		[AECategory("Sun")]
		[AESlider(0, 5, 0.1f, 0.01f)]
		static public float SunAngularSize { get; set; } = 1;

		[Config]	
		[AECategory("Atmosphere")]
		[AESlider(500, 12000, 500, 10)]
		static public float PlanetRadius { get; set; } = 6360;
		
		[Config]	
		[AESlider(0, 200, 10, 1)]
		[AECategory("Atmosphere")]
		static public float AtmosphereHeight { get; set; } = 80;
		
		[Config]	
		[AESlider(1, 10000, 500, 10)]
		[AECategory("Atmosphere")]
		static public float RayleighHeight { get; set; } = 8000;
		
		[Config]	
		[AESlider(1, 5000, 200, 10)]
		[AECategory("Atmosphere")]
		static public float MieHeight { get; set; } = 1200;
		
		[Config]	
		[AESlider(0, 200000, 1000, 10)]
		static public float ViewElevation { get; set; } = 0;

		[Config]	
		[AECategory("Atmosphere")]
		[AESlider(-0.95f, 0.95f, 0.05f, 0.01f)]
		static public float MieExcentricity { get; set; } = 0.76f;
		
		[Config]	
		[AECategory("Atmosphere")]
		[AEDisplayName("Sky Exposure (Ev)")]
		[AESlider(-8, 8, 1, 0.1f)]
		static public float SkyExposure { get; set; } = 0;

		[Config]	
		[AESlider(1000,5000,500,1)]
		static public float SkySphereSize { get; set; } = 10f;

		[Config]	
		[AECategory("Tweaks")]
		[AESlider(-8, 8, 1, 0.1f)]
		static public float RayleighScale { get; set; } = 0;

		[Config]	
		[AECategory("Tweaks")]
		[AESlider(-8, 8, 1, 0.1f)]
		static public float MieScale { get; set; } = 0;
		
		[Config]	
		[AECategory("Tweaks")]
		static public Color MieColor { get; set; } = Color.White;
		
		[Config]	
		[AECategory("Tweaks")]
		[AESlider(0, 5, 1, 0.1f)]
		static public float APScale { get; set; } = 0;
		
		[Config]	
		[AECategory("Tweaks")]
		[AESlider(0, 1, 0.1f, 0.001f)]
		static public float AmbientLevel { get; set; } = 0;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AESlider(0, 1, 0.1f, 0.001f)]
		[AEDisplayName("Cirrus Coverage")]
		static public float CirrusCoverage { get; set; } = 1;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AESlider(0, 12000, 1000f, 1f)]
		[AEDisplayName("Cirrus Height (m)")]
		static public float CirrusHeight { get; set; } = 6000;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AESlider(0, 1, 0.1f, 0.01f)]
		[AEDisplayName("Cirrus Density")]
		static public float CirrusDensity { get; set; } = 1;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AESlider(1, 100, 10f, 1f)]
		[AEDisplayName("Cirrus Size (km)")]
		static public float CirrusSize { get; set; } = 24;
		
		[Config]	
		[AECategory("Wind")]
		[AEDisplayName("Wind Speed (km/h)")]
		[AESlider(0, 500, 10f, 1f)]
		static public float WindVelocity { get; set; } = 240;
		
		[Config]	
		[AECategory("Wind")]
		[AEDisplayName("Wind Direction")]
		[AESlider(-180, 180, 5f, 1f)]
		static public float WindDirection { get; set; } = 0;
		
		[AECategory("Debug")]
		static public bool ShowLut { get; set; } = false;
		
		[AECategory("Debug")]
		static public bool SkipSky { get; set; } = false;

		Vector2 currentCloudOffset = Vector2.Zero;

		[AECommand]
		static public void Earth()
		{
			//SunAltitude			= 45;
			//SunAzimuth			= 45;
			SunIntensityEv		= 8;
			SunTemperature		= 5900;
			PlanetRadius		= 6360;
			AtmosphereHeight	= 80;
			RayleighHeight		= 8000;
			MieHeight			= 1200;
			ViewElevation			= 0;
			MieExcentricity		= 0.76f;
			SkySphereSize		= 10f;
			MieScale			= 0;
			RayleighScale		= 0;
			SkyExposure			= 0;

			SunAngularSize		=	0.8f;

			CirrusHeight		=	10000;
			CirrusCoverage		=	0.5f;
			CirrusDensity		=	0.5f;
			WindVelocity		=	240;
			CirrusSize			=	50;
		}

		[AECommand]
		static public void Moon()
		{
			SunIntensityEv = 7;
			SunTemperature = 5900;
			SunBrightnessEv = 16;
			SunAngularSize = 0.7f;
			PlanetRadius = 4000;
			AtmosphereHeight = 60;
			RayleighHeight = 4000;
			MieHeight = 2600;
			ViewElevation = 60;
			MieExcentricity = 0.7f;
			SkyExposure = 0;
			SkySphereSize = 10;
			RayleighScale = 2;
			MieScale = 5;
			MieColor = new Color(150, 134, 126, 255);
			APScale = 4;
			AmbientLevel = 0.8f;
			CirrusCoverage = 1;
			CirrusHeight = 6000;
			CirrusDensity = 0.5f;
			CirrusSize = 20;
			WindVelocity = 240;
			WindDirection = -45;
		}


		[AECommand]
		static public void Alien()
		{
			SunIntensityEv = 5;
			SunTemperature = 4100;
			SunBrightnessEv = 16;
			SunAngularSize = 0.3f;
			PlanetRadius = 4000;
			AtmosphereHeight = 60;
			RayleighHeight = 1000;
			MieHeight = 2000;
			ViewElevation = 60;
			MieExcentricity = 0.6f;
			SkyExposure = 0;
			SkySphereSize = 10;
			RayleighScale = -0.5f;
			MieScale = 6;
			MieColor = new Color(150, 130, 96, 255);
			APScale = 4;
			AmbientLevel = 0.8f;
			CirrusCoverage = 1;
			CirrusHeight = 6000;
			CirrusDensity = 0.5f;
			CirrusSize = 20;
			WindVelocity = 0;
			WindDirection = -45;
		}


		[AECommand]
		static public void Titan()
		{
			SunAltitude	=	15;
			SunAzimuth	=	45;
			SunIntensityEv = 6; // 2 - real value; // ~10 AU
			SunTemperature = 5900;
			SunBrightnessEv = 10;
			SunAngularSize = 0.4f;
			PlanetRadius = 2500;
			AtmosphereHeight = 60;
			RayleighHeight = 500;
			MieHeight = 1800;
			ViewElevation = 60;
			MieExcentricity = 0.76f;
			SkyExposure = 0;
			SkySphereSize = 10;
			RayleighScale = -3f;
			MieScale = 6;
			MieColor = new Color(208,185,118, 255);	 // ref color: 208,185,121
			APScale = 4;
			AmbientLevel = 0.8f;
			CirrusCoverage = 0;
			CirrusHeight = 6000;
			CirrusDensity = 0.0f;
			CirrusSize = 20;
			WindVelocity = 0;
			WindDirection = -45;
		}

		[AECommand]
		static public void Titan2()
		{
			SunAltitude = 10				;
			SunAzimuth = 45					;
			SunIntensityEv = 5				;
			SunTemperature = 2200			;
			SunBrightnessEv = 17			;
			SunAngularSize = 0.7f			;
			PlanetRadius = 4000				;
			AtmosphereHeight = 60			;
			RayleighHeight = 1000			;
			MieHeight = 2000				;
			ViewElevation = 60				;
			MieExcentricity = 0.6f			;
			SkyExposure = 0					;
			SkySphereSize = 10				;
			RayleighScale = 0				;
			MieScale = 4					;
			MieColor = new Color(150, 130, 96, 255);
			APScale = 0						;
			AmbientLevel = 0.8f				;
			CirrusCoverage = 1				;
			CirrusHeight = 2934				;
			CirrusDensity = 1				;
			CirrusSize = 10					;
			WindVelocity = 0				;
			WindDirection = -45				;
		}

		[AECommand]
		static public void Titan3()
		{
			SunAltitude = 20				;
			SunAzimuth = 45					;
			SunIntensityEv = 7				;
			SunTemperature = 5800			;
			SunBrightnessEv = 9				;
			SunAngularSize = 0.3f			;
			PlanetRadius = 2500				;
			AtmosphereHeight = 200			;
			RayleighHeight = 100000			;
			MieHeight = 3000				;
			ViewElevation = 110				;
			MieExcentricity = 0.35f			;
			SkyExposure = 0					;
			SkySphereSize = 10				;
			RayleighScale = -2				;
			MieScale = 4					;
			MieColor = new Color(234, 182, 117, 255);
			APScale = 0						;
			AmbientLevel = 0.8f				;
			CirrusCoverage = 0				;
			CirrusHeight = 6000				;
			CirrusDensity = 0				;
			CirrusSize = 20					;
			WindVelocity = 0				;
			WindDirection = -45				;
		}


		static FXConstantBuffer<SKY_DATA>				regSky				=	new CRegister( 0, "Sky"			);
		static FXConstantBuffer<GpuData.CAMERA>			regCamera			=	new CRegister( 1, "Camera"		);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>	regDirectLight		=	new CRegister( 2, "DirectLight"	);
		static FXConstantBuffer<Fog.FOG_DATA>			regFog				=	new CRegister( 3, "Fog"	);

		static FXTexture2D<Vector4>						regLutScattering	=	new TRegister( 0, "LutScattering"	);
		static FXTexture2D<Vector4>						regLutTransmittance	=	new TRegister( 1, "LutTransmittance");
		static FXTexture2D<Vector4>						regLutCirrus		=	new TRegister( 2, "LutCirrus"		);
		static FXTextureCube<Vector4>					regSkyCube			=	new TRegister( 3, "SkyCube"			);
		static FXTexture2D<Vector4>						regFogLut			=	new TRegister( 4, "FogLut"			);

		static FXTexture2D<Vector4>						regCirrusClouds		=	new TRegister( 5, "CirrusClouds"	);

		[ShaderIfDef("LUT_AP")]	static FXRWTexture3D<Vector4>	regLutAP	=	new URegister( 0, "LutAP"			);

		static FXSamplerState		regLutSampler	=	new SRegister(0, "LutSampler" );
		static FXSamplerState		regLinearWrap	=	new SRegister(1, "LinearWrap" );
		static FXSamplerState		regLinearClamp	=	new SRegister(2, "LinearClamp" );
		
		[Flags]
		enum Flags : int
		{
			SKY_VIEW		= 1 << 0,
			SKY_CUBE		= 1 << 1,
			LUT_SKY			= 1 << 2,
			LUT_AP			= 1 << 3,
		}

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Size=256)]
		public struct SKY_DATA 
		{
			public Color4	BetaRayleigh;	
			public Color4	BetaMie;
			public Color4	MieColor;

			public Color4	SunIntensity;
			public Color4	SunBrightness; // 

			public Vector4	SunDirection;
			public Vector4	ViewOrigin;

			public Color4	AmbientLevel;
			public Vector4	ViewportSize;

			public float	SunAzimuth;
			public float	SunAltitude;
			public float	APScale;
			public float	Dummy1;

			public float 	PlanetRadius;
			public float	AtmosphereRadius;
			public float	RayleighHeight;
			public float	MieHeight;

			public float	MieExcentricity;
			public float	SkySphereSize;
			public float	ViewHeight;
			public float	SkyExposure;

			public float	CirrusHeight;
			public float	CirrusCoverage;
			public float	CirrusDensity;
			public float	CirrusScale;

			public float	CirrusScrollU;
			public float	CirrusScrollV;
			public float	Dummy2;
			public float	Dummy3;
		}


		struct SkyVertex {
			[Vertex("POSITION")]
			public Vector4 Vertex;
		}

		VertexBuffer		skyVB;
		Ubershader			sky;
		ConstantBuffer		cbSky;
		SKY_DATA			skyData;
		StateFactory		factory;
		Camera				cubeCamera;
		SamplerState		skyLutSampler;

		RenderTarget2D		lutSkyEmission;
		RenderTarget2D		lutSkyExtinction;
		RenderTarget2D		lutCirrus;

		Texture3DCompute	lutAerial0;
		Texture3DCompute	lutAerial1;

		public Vector3	SkyAmbientLevel { get; protected set; }

		internal RenderTargetCube	SkyCube { get { return skyCube; } }
		RenderTargetCube			skyCube;

		internal RenderTargetCube	SkyCubeDiffuse { get { return skyCubeDiffuse; } }
		RenderTargetCube			skyCubeDiffuse;

		internal Texture3DCompute	LutAP { get { return lutAerial0; } }

		DiscTexture	texCirrusClouds;

		public Color4 AmbientColor { get { return ambientColor; } }
		Color4 ambientColor = Color4.Zero;


		public ConstantBuffer SkyData { get { return cbSky; } }


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
			//var noiseTest	=	ImageLib.GenerateOpenSimplexNoise(256,256, 7, 1/64.0f, 0.5f);
			//ImageLib.SaveTga( noiseTest, "noiseTest.tga" );

			skyData			=	new SKY_DATA();
			skyCube			=	new RenderTargetCube( device, ColorFormat.Rgba16F, 128, 5 );
			skyCubeDiffuse	=	new RenderTargetCube( device, ColorFormat.Rgba16F,   8, 0 );
			cbSky			=	new ConstantBuffer( device, typeof(SKY_DATA) );
			cubeCamera		=	new Camera( rs, "SkyCubeCamera" );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();

			lutSkyEmission		=	new RenderTarget2D( device, ColorFormat.Rgba16F,	(int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );
			lutSkyExtinction	=	new RenderTarget2D( device, ColorFormat.Rgba16F,	(int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );
			lutCirrus			=	new RenderTarget2D( device, ColorFormat.Rgba16F,	(int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );
			lutAerial0			=	new Texture3DCompute( device, ColorFormat.Rgba16F, 	(int)AP_WIDTH, (int)AP_HEIGHT, (int)AP_DEPTH );
			lutAerial1			=	new Texture3DCompute( device, ColorFormat.Rgba16F, 	(int)AP_WIDTH, (int)AP_HEIGHT, (int)AP_DEPTH );

			var skySphere	=	SkySphere.GetVertices(3).Select( v => new SkyVertex{ Vertex = v } ).ToArray();
			skyVB			=	new VertexBuffer( Game.GraphicsDevice, typeof(SkyVertex), skySphere.Length );
			skyVB.SetData( skySphere );

			skyLutSampler	=	new SamplerState();
			skyLutSampler.Filter	=	Drivers.Graphics.Filter.MinMagMipLinear;
			skyLutSampler.AddressU	=	Drivers.Graphics.AddressMode.Mirror;
			skyLutSampler.AddressV	=	Drivers.Graphics.AddressMode.Mirror;
			skyLutSampler.AddressW	=	Drivers.Graphics.AddressMode.Wrap;
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			sky			=	Game.Content.Load<Ubershader>("sky2");
			factory		=	sky.CreateFactory( typeof(Flags), (ps,i) => EnumFunc(ps, (Flags)i) );

			texCirrusClouds	=	Game.Content.Load<DiscTexture>(@"sky\cirrus");
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void EnumFunc ( PipelineState ps, Flags flags )
		{
			if (flags!=Flags.LUT_SKY)
			{
				ps.VertexInputElements	=	VertexInputElement.FromStructure<SkyVertex>();
			}

			//	do not cull triangles for both for RH and LH coordinates 
			//	for direct view and cubemaps.
			ps.RasterizerState		=	RasterizerState.CullNone; 
			ps.BlendState			=	BlendState.Opaque;
			ps.DepthStencilState	=	flags.HasFlag(Flags.SKY_CUBE) ? DepthStencilState.None : DepthStencilState.None;
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) 
			{
				SafeDispose( ref skyVB );
				SafeDispose( ref skyCube );
				SafeDispose( ref skyCubeDiffuse );
				SafeDispose( ref cbSky );
				SafeDispose( ref cubeCamera );
				SafeDispose( ref lutSkyEmission );
				SafeDispose( ref lutSkyExtinction );
				SafeDispose( ref skyLutSampler );
				SafeDispose( ref lutAerial0 );
				SafeDispose( ref lutAerial1 );
			}
			base.Dispose( disposing );
		}



		void Setup ( Flags flags, Camera camera, Rectangle viewport )
		{
			device.SetViewport( viewport );
			device.SetScissorRect( viewport );

			var upVector		=	new Vector4( Vector3.Up, 0 );
			var apScale			=	MathUtil.Exp2( APScale );
			var eleveation		=	( PlanetRadius * 1000 + ViewElevation + 2 ) + Math.Max( 0, RenderSystem.GameUnitToMeters( camera.CameraPosition.Y ) );
			var viewOrigin		=	upVector * eleveation;

			skyData.BetaRayleigh		=	BetaRayleigh * MathUtil.Exp2( RayleighScale );	
			skyData.BetaMie				=	BetaMie		 * MathUtil.Exp2( MieScale );
			skyData.MieColor			=	MieColor;
			skyData.PlanetRadius		=	( PlanetRadius ) * 1000	;
			skyData.AtmosphereRadius	=	( PlanetRadius + AtmosphereHeight ) * 1000;
			skyData.RayleighHeight		=	RayleighHeight;
			skyData.MieHeight			=	MieHeight;
			skyData.MieExcentricity		=	MieExcentricity;
			skyData.SkySphereSize		=	SkySphereSize;
			skyData.ViewHeight			=	ViewElevation;
			skyData.SunIntensity		=	GetSunIntensity(false);
			skyData.SunBrightness		=	GetSunBrightness();
			skyData.SunDirection		=	GetSunDirection4();
			skyData.SunAltitude			=	MathUtil.DegreesToRadians( SunAltitude );
			skyData.SunAzimuth			=	MathUtil.DegreesToRadians( SunAzimuth );
			skyData.SkyExposure			=	MathUtil.Exp2( SkyExposure );
			skyData.AmbientLevel		=	AmbientColor;
			skyData.ViewOrigin			=	viewOrigin;
			skyData.APScale				=	apScale;

			skyData.CirrusCoverage		=	CirrusCoverage;
			skyData.CirrusHeight		=	CirrusHeight;
			skyData.CirrusScale			=	1.0f / CirrusSize / 1000.0f;
			skyData.CirrusDensity		=	CirrusDensity;
			skyData.CirrusScrollU		=	currentCloudOffset.X * skyData.CirrusScale;
			skyData.CirrusScrollV		=	currentCloudOffset.Y * skyData.CirrusScale;

			skyData.ViewportSize		=	new Vector4( viewport.Width, viewport.Height, 1.0f / viewport.Width, 1.0f / viewport.Height );

			cbSky.SetData( skyData );

			device.GfxConstants		[ regSky			]	=	cbSky;
			device.GfxConstants		[ regCamera			]	=	camera.CameraData;
			device.GfxConstants		[ regDirectLight	]	=	rs.LightManager.DirectLightData;
			device.GfxConstants		[ regFog			]	=	rs.Fog.FogData;

			device.ComputeConstants	[ regSky			]	=	cbSky;
			device.ComputeConstants	[ regCamera			]	=	camera.CameraData;
			device.ComputeConstants	[ regDirectLight	]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants	[ regFog			]	=	rs.Fog.FogData;

			device.GfxSamplers		[ regLutSampler		]	=	skyLutSampler;
			device.ComputeSamplers	[ regLutSampler		]	=	skyLutSampler;
			device.GfxSamplers		[ regLinearWrap		]	=	SamplerState.LinearWrap;
			device.ComputeSamplers	[ regLinearWrap		]	=	SamplerState.LinearWrap;
			device.GfxSamplers		[ regLinearClamp	]	=	SamplerState.LinearClamp;
			device.ComputeSamplers	[ regLinearClamp	]	=	SamplerState.LinearClamp;

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
		internal void RenderSky( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame frame, bool cubemap )
		{
			if (SkipSky) return;

			RenderSky( gameTime, camera, stereoEye, frame.HdrTarget.Surface, cubemap );

			if (ShowLut)
			{
				rs.Filter.StretchRect( frame.HdrTarget.Surface, lutSkyEmission );
			}
		}



		void UpdateCloudPosition( GameTime gameTime )
		{
			float	v	=	WindVelocity / 3.6f; // to m/s
			float dU	=	(float)Math.Cos( MathUtil.DegreesToRadians( WindDirection ) ) * gameTime.ElapsedSec * v;
			float dV	=	(float)Math.Sin( MathUtil.DegreesToRadians( WindDirection ) ) * gameTime.ElapsedSec * v;

			currentCloudOffset.X	+=	dU;
			currentCloudOffset.Y	+=	dV;
		}


		/// <summary>
		/// Renders sky with specified technique
		/// </summary>
		/// <param name="rendCtxt"></param>
		/// <param name="techName"></param>
		internal void RenderSkyLut( GameTime gameTime, Camera camera )
		{
			if (SkipSky) return;

			UpdateCloudPosition( gameTime );

			ambientColor = ComputeZenithColor() * AmbientLevel;

			using ( new PixEvent("Sky LUT") ) 
			{
				device.ResetStates();

				//	Sky LUT :

				device.SetTargets( null, lutSkyEmission.Surface, lutSkyExtinction.Surface, lutCirrus.Surface );

				device.GfxResources[ regSkyCube ] = skyCubeDiffuse;

				Setup( Flags.LUT_SKY, camera, new Rectangle(0,0, (int)LUT_WIDTH, (int)LUT_HEIGHT) );

				device.Draw( 3, 0 );

				//	AP LUT :

				Setup( Flags.LUT_AP, camera, new Rectangle(0,0, (int)LUT_WIDTH, (int)LUT_HEIGHT) );
				device.SetComputeUnorderedAccess( regLutAP, LutAP.UnorderedAccess );

				uint tgx = MathUtil.IntDivRoundUp( AP_WIDTH,  8 );
				uint tgy = MathUtil.IntDivRoundUp( AP_HEIGHT, 8 );
				uint tgz = MathUtil.IntDivRoundUp( AP_DEPTH,  1 );
				device.Dispatch( tgx, tgy, tgz );
			}
		}


		/// <summary>
		/// Renders sky with specified technique
		/// </summary>
		/// <param name="rendCtxt"></param>
		/// <param name="techName"></param>
		internal void RenderSky( GameTime gameTime, Camera camera, StereoEye stereoEye, RenderTargetSurface color, bool cubemap )
		{
			if (SkipSky) return;

			using ( new PixEvent( "Sky" ) )
			{
				device.ResetStates();

				device.SetTargets( null, color );
				device.GfxResources[ regLutScattering		] =	lutSkyEmission;
				device.GfxResources[ regLutTransmittance	] =	lutSkyExtinction;
				device.GfxResources[ regLutCirrus			] =	lutCirrus;
				device.GfxResources[ regCirrusClouds		] = texCirrusClouds.Srv;
				device.GfxResources[ regSkyCube				] = skyCube;
				device.GfxResources[ regFogLut				] = rs.Fog.SkyFogLut;

				Setup( cubemap ? Flags.SKY_CUBE : Flags.SKY_VIEW, camera, color.Bounds );

				device.SetupVertexInput( skyVB, null );
				device.Draw( skyVB.Capacity, 0 );
			}
		}


		/// <summary>
		/// Renders sky with specified technique
		/// </summary>
		/// <param name="rendCtxt"></param>
		/// <param name="techName"></param>
		internal void RenderSkyCube( GameTime gameTime, Camera camera )
		{
			if (SkipSky) return;

			using ( new PixEvent( "SkyCube" ) )
			{
				device.ResetStates();

				for( int i = 0; i < 6; ++i ) 
				{
					cubeCamera.SetupCameraCubeFaceLH( Vector3.Zero, (CubeFace)i, 0.125f, 10000 );

					device.SetTargets( null, SkyCube.GetSurface(0, (CubeFace)i ) );

					device.GfxResources[ regLutScattering		] =	lutSkyEmission;
					device.GfxResources[ regLutTransmittance	] =	lutSkyExtinction;
					device.GfxResources[ regLutCirrus			] =	lutCirrus;
					device.GfxResources[ regCirrusClouds		] = texCirrusClouds.Srv;
					device.GfxResources[ regSkyCube				] = skyCubeDiffuse;

					Setup( Flags.SKY_CUBE, cubeCamera, new Rectangle( 0, 0, SkyCube.Width, SkyCube.Height ) );

					device.SetupVertexInput( skyVB, null );
					device.Draw( skyVB.Capacity, 0 );
				}

				Game.GetService<CubeMapFilter>().GenerateCubeMipLevel( skyCube );
				Game.GetService<CubeMapFilter>().PrefilterDiffuse( skyCubeDiffuse, skyCube, 4 );
			}
		}
	}
}
