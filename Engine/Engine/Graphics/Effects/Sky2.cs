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

namespace Fusion.Engine.Graphics {

	[RequireShader("sky2", true)]
	public class Sky2 : RenderComponent 
	{
		[ShaderDefine] const uint BLOCK_SIZE	=	16;
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
		[AECategory("Sun")]
		[AEValueRange(-5, 16, 1, 1)]
		public float SunBrightnessEv { get; set; } = 12;

		[Config]	
		[AECategory("Sun")]
		[AEValueRange(0, 5, 0.1f, 0.01f)]
		public float SunAngularSize { get; set; } = 1;

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
		[AEValueRange(1, 5000, 200, 10)]
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
		public float RayleighScale { get; set; } = 0;

		[Config]	
		[AECategory("Tweaks")]
		[AEValueRange(-8, 8, 1, 0.1f)]
		public float MieScale { get; set; } = 0;
		
		[Config]	
		[AECategory("Tweaks")]
		public Color MieColor { get; set; } = Color.White;
		
		[Config]	
		[AECategory("Tweaks")]
		[AEValueRange(0, 1, 0.1f, 0.001f)]
		public float AmbientLevel { get; set; } = 0;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AEValueRange(0, 1, 0.1f, 0.001f)]
		[AEDisplayName("Cirrus Coverage")]
		public float CirrusCoverage { get; set; } = 1;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AEValueRange(0, 12000, 1000f, 1f)]
		[AEDisplayName("Cirrus Height (m)")]
		public float CirrusHeight { get; set; } = 6000;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		[AEDisplayName("Cirrus Density")]
		public float CirrusDensity { get; set; } = 1;
		
		[Config]	
		[AECategory("Cirrus Clouds")]
		[AEValueRange(1, 100, 10f, 1f)]
		[AEDisplayName("Cirrus Size (km)")]
		public float CirrusSize { get; set; } = 24;
		
		[Config]	
		[AECategory("Wind")]
		[AEDisplayName("Wind Speed (km/h)")]
		[AEValueRange(0, 500, 10f, 1f)]
		public float WindVelocity { get; set; } = 240;
		
		[Config]	
		[AECategory("Wind")]
		[AEDisplayName("Wind Direction")]
		[AEValueRange(-180, 180, 5f, 1f)]
		public float WindDirection { get; set; } = 0;
		
		[AECategory("Debug")]
		public bool ShowLut { get; set; } = false;

		Vector2 currentCloudOffset = Vector2.Zero;

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

			SunAngularSize		=	0.8f;

			CirrusHeight		=	10000;
			CirrusCoverage		=	0.5f;
			CirrusDensity		=	0.5f;
			WindVelocity		=	240;
			CirrusSize			=	50;
		}


		static FXConstantBuffer<SKY_DATA>				regSky				=	new CRegister( 0, "Sky"			);
		static FXConstantBuffer<GpuData.CAMERA>			regCamera			=	new CRegister( 1, "Camera"		);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>	regDirectLight		=	new CRegister( 2, "DirectLight"	);

		static FXTexture2D<Vector4>						regLutScattering	=	new TRegister( 0, "LutScattering"	);
		static FXTexture2D<Vector4>						regLutTransmittance	=	new TRegister( 1, "LutTransmittance");
		static FXTexture2D<Vector4>						regLutCirrus		=	new TRegister( 2, "LutCirrus"		);
		static FXTextureCube<Vector4>					regSkyCube			=	new TRegister( 3, "SkyCube"			);

		static FXTexture2D<Vector4>						regCirrusClouds		=	new TRegister( 5, "CirrusClouds"	);

		static FXSamplerState		regLutSampler	 =	new SRegister(0, "LutSampler" );
		static FXSamplerState		regLinearWrap	=	new SRegister(1, "LinearWrap" );
		
		[Flags]
		enum Flags : int
		{
			SKY		= 1 << 0,
			FOG		= 1 << 1,
			LUT		= 1 << 2,
		}

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Size=256)]
		struct SKY_DATA 
		{
			public Color4	BetaRayleigh;	
			public Color4	BetaMie;

			public Color4	SunIntensity;
			public Color4	SunBrightness; // 

			public Vector4	SunDirection;
			public Vector4	ViewOrigin;

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

			public float	AmbientLevel;

			public float	CirrusHeight;
			public float	CirrusCoverage;
			public float	CirrusDensity;
			public float	CirrusScale;
			public float	CirrusScrollU;
			public float	CirrusScrollV;
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

		RenderTarget2D	lutSkyEmission;
		RenderTarget2D	lutSkyExtinction;
		RenderTarget2D	lutCirrus;

		public Vector3	SkyAmbientLevel { get; protected set; }

		internal RenderTargetCube	SkyCube { get { return skyCube; } }
		RenderTargetCube			skyCube;

		internal RenderTargetCube	SkyCubeDiffuse { get { return skyCubeDiffuse; } }
		RenderTargetCube			skyCubeDiffuse;

		DiscTexture	texCirrusClouds;



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

			return new Vector3( x, y, z ).Normalized();
		}


		public Vector4 GetSunDirection4()
		{
			return new Vector4( GetSunDirection(), 0 );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color4 GetSunIntensity( bool horizonDarken = false )
		{
			float	scale	=	MathUtil.Exp2( SunIntensityEv );
			Color4	color	=	Temperature.GetColor( (int)SunTemperature );
			color *= scale;

			if (horizonDarken)
			{
				var origin	=	Vector3.Up * ( PlanetRadius * 1000 + ViewHeight );
				var dir		=	GetSunDirection();
				color		*=	ComputeAtmosphereAbsorption( origin, dir );
			}

			return 	color;
		}


		Color4 ComputeAtmosphereAbsorption( Vector3 origin, Vector3 direction )
		{
			var	distance		=	0f;
			var	numSamples		=	64;
			var	Hr				=	RayleighHeight;
			var	Hm				=	MieHeight;

			var	opticalDepthR 	=	0f;
			var	opticalDepthM 	=	0f; 

			var	betaR			=	BetaRayleigh * MathUtil.Exp2( RayleighScale );
			var	betaM			=	BetaMie		 * MathUtil.Exp2( MieScale ) * MieColor;

			if ( RayAtmosphereIntersection( origin, direction, out distance) )
			{
				for (int i=0; i<numSamples; i++)
				{
					var segmentLength	=	distance / numSamples;	
					var localDistance	=	segmentLength * ( i + 0.5f );
					var samplePosition	=	origin + direction * localDistance;
					var height			=	samplePosition.Length() - PlanetRadius * 1000;
					float hr			=	(float)Math.Exp(-height / Hr) * segmentLength; 
					float hm			=	(float)Math.Exp(-height / Hm) * segmentLength; 
					opticalDepthR		+=	hr; 
					opticalDepthM		+=	hm; 
				}

				var totalOpticalDepth	=	new Color4();

				totalOpticalDepth.Red	=	opticalDepthR * betaR.Red	 + opticalDepthM * 1.1f * betaM.Red		;
				totalOpticalDepth.Green	=	opticalDepthR * betaR.Green	 + opticalDepthM * 1.1f * betaM.Green	;
				totalOpticalDepth.Blue	=	opticalDepthR * betaR.Blue	 + opticalDepthM * 1.1f * betaM.Blue	;

				return new Color4( 
					(float)Math.Exp( -totalOpticalDepth.Red ),
					(float)Math.Exp( -totalOpticalDepth.Green ),
					(float)Math.Exp( -totalOpticalDepth.Blue ),
					1
				);
			}
			else
			{
				return new Color4(1,1,1,1);
			}
		}


		bool RayAtmosphereIntersection( Vector3 origin, Vector3 dir, out float dist )
		{
			float t0, t1;

			if (!RaySphereIntersect( origin, dir, (PlanetRadius + AtmosphereHeight) * 1000, out t0, out t1 ) && t1<0)
			{
				dist = 0;
				return false;
			}
			else
			{
				dist = t1;
				return true;
			}
		}


		bool RaySphereIntersect(Vector3 origin, Vector3 dir, float radius, out float t0, out float t1 )
		{
			t0 = t1 = 0;
	
			var	r0	=	origin;			// - r0: ray origin
			var	rd	=	dir;			// - rd: normalized ray direction
			var	s0	=	Vector3.Zero;	// - s0: sphere center
			var	sr	=	radius;			// - sr: sphere radius

			float 	a 		= Vector3.Dot(rd, rd);
			Vector3	s0_r0 	= r0 - s0;
			float 	b 		= 2.0f * Vector3.Dot(rd, s0_r0);
			float 	c 		= Vector3.Dot(s0_r0, s0_r0) - (sr * sr);
	
			float	D		=	b*b - 4.0f*a*c;
	
			if (D<0)
			{
				return false;
			}
	
			t0	=	(-b - (float)Math.Sqrt(D))/(2.0f*a);
			t1	=	(-b + (float)Math.Sqrt(D))/(2.0f*a);
			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Color4 GetSunBrightness()
		{
			float	scale	=	MathUtil.Exp2( SunBrightnessEv );
			Color4	color	=	Temperature.GetColor( (int)SunTemperature );
			color *= scale;

			color.Alpha		=	MathUtil.DegreesToRadians( SunAngularSize );

			return 	color;
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

			lutSkyEmission		=	new RenderTarget2D( device, ColorFormat.Rgba16F, (int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );
			lutSkyExtinction	=	new RenderTarget2D( device, ColorFormat.Rgba16F, (int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );
			lutCirrus			=	new RenderTarget2D( device, ColorFormat.Rgba16F, (int)LUT_WIDTH, (int)LUT_HEIGHT, false, true );

			var skySphere	=	SkySphere.GetVertices(5).Select( v => new SkyVertex{ Vertex = v } ).ToArray();
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
			if (flags!=Flags.LUT)
			{
				ps.VertexInputElements	=	VertexInputElement.FromStructure<SkyVertex>();
			}

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
				SafeDispose( ref skyCubeDiffuse );
				SafeDispose( ref cbSky );
				SafeDispose( ref cubeCamera );
				SafeDispose( ref lutSkyEmission );
				SafeDispose( ref lutSkyExtinction );
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
			skyData.SunIntensity		=	GetSunIntensity(false);
			skyData.SunBrightness		=	GetSunBrightness();
			skyData.SunDirection		=	GetSunDirection4();
			skyData.SunAltitude			=	MathUtil.DegreesToRadians( SunAltitude );
			skyData.SunAzimuth			=	MathUtil.DegreesToRadians( SunAzimuth );
			skyData.SkyExposure			=	MathUtil.Exp2( SkyExposure );
			skyData.AmbientLevel		=	AmbientLevel;
			skyData.ViewOrigin			=	new Vector4( Vector3.Up, 0 ) * (skyData.PlanetRadius + skyData.ViewHeight + 2); // 2 meters to prevent self occlusion

			skyData.CirrusCoverage		=	CirrusCoverage;
			skyData.CirrusHeight		=	CirrusHeight;
			skyData.CirrusScale			=	1.0f / CirrusSize / 1000.0f;
			skyData.CirrusDensity		=	CirrusDensity;
			skyData.CirrusScrollU		=	currentCloudOffset.X * skyData.CirrusScale;
			skyData.CirrusScrollV		=	currentCloudOffset.Y * skyData.CirrusScale;

			cbSky.SetData( skyData );

			device.GfxConstants		[ regSky			]	=	cbSky;
			device.GfxConstants		[ regCamera			]	=	camera.CameraData;
			device.GfxConstants		[ regDirectLight	]	=	rs.LightManager.DirectLightData;

			device.GfxSamplers		[ regLutSampler		]	=	skyLutSampler;
			device.ComputeSamplers	[ regLutSampler		]	=	skyLutSampler;
			device.GfxSamplers		[ regLinearWrap		]	=	SamplerState.LinearWrap;
			device.ComputeSamplers	[ regLinearWrap		]	=	SamplerState.LinearWrap;

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
		internal void Render( GameTime gameTime, Camera camera, StereoEye stereoEye, HdrFrame frame )
		{
			Render( gameTime, camera, stereoEye, frame.HdrTarget.Surface );

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
		internal void Render( GameTime gameTime, Camera camera, StereoEye stereoEye, RenderTargetSurface color, bool noSun = false )
		{
			UpdateCloudPosition( gameTime );

			using ( new PixEvent("Sky Rendering") ) 
			{
				using ( new PixEvent( "Lut" ) )
				{
					device.ResetStates();

					device.SetTargets( null, lutSkyEmission.Surface, lutSkyExtinction.Surface, lutCirrus.Surface );

					device.GfxResources[ regSkyCube ] = skyCubeDiffuse;

					Setup( Flags.LUT, camera, color.Bounds );

					device.Draw( 3, 0 );
				}


				using ( new PixEvent( "Sky" ) )
				{
					device.ResetStates();

					device.SetTargets( null, color );
					device.GfxResources[ regLutScattering		] =	lutSkyEmission;
					device.GfxResources[ regLutTransmittance	] =	lutSkyExtinction;
					device.GfxResources[ regLutCirrus			] =	lutCirrus;
					device.GfxResources[ regCirrusClouds		] = texCirrusClouds.Srv;
					device.GfxResources[ regSkyCube				] = skyCubeDiffuse;

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

						device.GfxResources[ regLutScattering		] =	lutSkyEmission;
						device.GfxResources[ regLutTransmittance	] =	lutSkyExtinction;
						device.GfxResources[ regLutCirrus			] =	lutCirrus;
						device.GfxResources[ regCirrusClouds		] = texCirrusClouds.Srv;
						device.GfxResources[ regSkyCube				] = skyCubeDiffuse;

						Setup( Flags.FOG, cubeCamera, new Rectangle( 0, 0, SkyCube.Width, SkyCube.Height ) );

						device.SetupVertexInput( skyVB, null );
						device.Draw( skyVB.Capacity, 0 );
					}

					Game.GetService<CubeMapFilter>().GenerateCubeMipLevel( skyCube );
					Game.GetService<CubeMapFilter>().PrefilterDiffuse( skyCubeDiffuse, skyCube, 4 );
				}
			}
		}
	}
}
