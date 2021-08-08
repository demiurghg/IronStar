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
using Fusion.Widgets.Advanced;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	[RequireShader("fog", true)]
	[ShaderSharedStructure(typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	internal partial class Fog : RenderComponent 
	{
		[Config]	
		[AECategory("Fog")]
		[AESlider(0, 0.1f, 0.01f, 0.001f)]
		public float FogDensity { get; set; } = 0;

		[Config]	
		[AECategory("Fog")]
		[AESlider(0, 1000, 50, 1)]
		public float FogHeight { get; set; } = 50;

		[Config]	
		[AECategory("Fog")]
		[AESlider(0, 0.98f, 0.1f, 0.01f)]
		public float HistoryFactor 
		{ 
			get { return historyFactor; }
			set { historyFactor = MathUtil.Clamp( value, 0, 0.99f ); }
		}
		float historyFactor = 0.8f;

		[Config]
		[AECategory("Quality")]
		public QualityLevel FogQuality 
		{ 
			get { return fogQuality; }
			set
			{
				if (fogQuality!=value)
				{
					fogQuality = value;
					fogQualityDirty = true;
				}
			}
		}
		QualityLevel fogQuality	=	QualityLevel.Ultra;
		bool fogQualityDirty	=	true;

		//[Config]
		//[AECategory("Fog Grid")]
		//public int FogGridSizeX	{ get { return fogSizeX; } set { fogSizeX = MathUtil.Clamp(value, 8, 256 ); } }
		//[Config]
		//[AECategory("Fog Grid")]
		//public int FogGridSizeY	{ get { return fogSizeY; } set { fogSizeY = MathUtil.Clamp(value, 8, 256 ); } }
		//[Config]
		//[AECategory("Fog Grid")]
		//public int FogGridSizeZ { get { return fogSizeZ; } set { fogSizeZ = MathUtil.Clamp(value, 8, 256 ); } }
		Size3 fogGridSize;

		[Config]
		[AECategory("Fog Grid")]
		public bool ShowSlices { get; set; }

		[Config]
		[AECategory("Fog Grid")]
		[AESlider(100, 5000, 100f, 1f)]
		public float FogGridHalfDepth { get { return fogGridHalfDepth; } set { fogGridHalfDepth = MathUtil.Clamp( value, 100, 5000 ); } }
		float fogGridHalfDepth = 300.0f;

		[Config]
		[AECategory("Fog Grid")]
		[AESlider(100, 5000, 100f, 1f)]
		public float FogFadeoutDistance { get { return fogFadeoutDistance; } set { fogFadeoutDistance = MathUtil.Clamp( value, 100, 5000 ); } }
		float fogFadeoutDistance = 600.0f;

		[Config]
		[AECategory("Ground Fog")]
		public bool GroundFogEnabled { get; set; } = true;

		[Config]
		[AECategory("Ground Fog")]
		[AESlider(10, 5000, 100f, 10f)]
		public float GroundFogHeight { get; set; } = 100;

		[Config]
		[AECategory("Ground Fog")]
		[AESlider(1, 5000, 100, 1f)]
		public float GroundFogDistance { get; set; } = 500;

		[Config]
		[AECategory("Ground Fog")]
		[AESlider(-200, 200, 10, 1f)]
		public float GroundFogLevel { get; set; } = 0;

		[Config]
		[AECategory("Ground Fog")]
		public Color GroundFogColor { get; set; } = Color.Gray;


		float FogGridExpK { get { return (float)Math.Log(0.5f) / fogGridHalfDepth; } }

		[ShaderDefine]
		const int BlockSizeX	=	4;

		[ShaderDefine]
		const int BlockSizeY	=	4;

		[ShaderDefine]
		const int BlockSizeZ	=	4;

		static FXConstantBuffer<FOG_DATA>					regFog						=	new CRegister( 0, "Fog"						);
		static FXConstantBuffer<Sky2.SKY_DATA>				regSky						=	new CRegister( 1, "Sky"						);
		static FXConstantBuffer<GpuData.CAMERA>				regCamera					=	new CRegister( 2, "Camera"					);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight				=	new CRegister( 3, "DirectLight"				);
		static FXConstantBuffer<ShadowSystem.CASCADE_SHADOW>regCascadeShadow			=	new CRegister( 4, "CascadeShadow"			);

		static FXSamplerState								regLinearClamp				=	new SRegister( 0, "LinearClamp"				);
		static FXSamplerComparisonState						regShadowSampler			=	new SRegister( 1, "ShadowSampler"			);
																										
		static FXTexture3D<Vector4>							regFogSource				=	new TRegister( 0, "FogSource"				);
		static FXTexture3D<Vector4>							regFogHistory				=	new TRegister( 1, "FogHistory"				);
		static FXTexture3D<UInt2>							regClusterTable				=	new TRegister( 2, "ClusterArray"			);
		static FXBuffer<uint>								regLightIndexTable			=	new TRegister( 3, "ClusterIndexBuffer"		);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLightDataTable			=	new TRegister( 4, "ClusterLightBuffer"		);
		static FXTexture2D<Vector4>							regShadowMap				=	new TRegister( 5, "ShadowMap"				);
		static FXTexture2D<Vector4>							regShadowMask				=	new TRegister( 6, "ShadowMask"				);
		static FXTexture3D<Vector4>							regLutAP					=	new TRegister( 7, "LutAP"					);
		static FXTexture3D<Vector4>							regShadow					=	new TRegister( 9, "FogShadowSource"			);
		static FXTexture3D<Vector4>							regShadowHistory			=	new TRegister(10, "FogShadowHistory"		);

		static FXTexture3D<Vector4>							regIrradianceVolumeL0		=	new TRegister(11, "IrradianceVolumeL0"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL1		=	new TRegister(12, "IrradianceVolumeL1"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL2		=	new TRegister(13, "IrradianceVolumeL2"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL3		=	new TRegister(14, "IrradianceVolumeL3"		);

		[ShaderIfDef("INTEGRATE,COMPUTE")]
		static FXRWTexture3D<Vector4>						regFogTarget				=	new URegister(0, "FogTarget"				);

		[ShaderIfDef("COMPUTE")]
		static FXRWTexture3D<Vector4>						regFogShadowTarget			=	new URegister(1, "FogShadowTarget"			);

		[ShaderIfDef("INTEGRATE")]
		static FXRWTexture2D<Vector4>						regSkyFogLut				=	new URegister(1, "SkyFogLut"				);

		[Flags]
		enum FogFlags : int
		{
			COMPUTE		= 0x0001,
			INTEGRATE	= 0x0002,
			SHOW_SLICE	= 0x0004,
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=256)]
		public struct FOG_DATA 
		{
			public Matrix	WorldToVolume;
			public Vector4	SampleOffset;

			public Vector4	FogSizeInv;
			public Color4	FogColor;

			public Color4	GroundFogColor;
			public float	GroundFogDensity;
			public float	GroundFogHeight;
			public float	GroundFogLevel;

			public uint		FogSizeX;
			public uint		FogSizeY;
			public uint		FogSizeZ;
			public float	FogGridExpK;
			public float	DirectLightFactor;
			public float	IndirectLightFactor;
			public float	HistoryFactor;
			public uint		FrameCount;
			public float	FogDensity;
			public float	FogHeight;
			public float	FogScale;
			public float	FadeoutDistanceInvSqr;
		}

		Ubershader			shader;
		StateFactory		factory;
		ConstantBuffer		cbFog;
		Texture3DCompute	fogDensity;
		Texture3DCompute	scatteredLight0;
		Texture3DCompute	scatteredLight1;
		Texture3DCompute	volumeShadow;
		Texture3DCompute	shadowHistory;
		Texture3DCompute	integratedLight;
		RenderTarget2D		skyFogLut;
		uint				frameCounter;
		Random				random = new Random();

		public ShaderResource FogGrid	{ get { return integratedLight; } }
		public ShaderResource SkyFogLut	{ get { return skyFogLut; } }
		public ConstantBuffer FogData	{ get { return cbFog; } }


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public Fog ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			CreateVolumeResources();

			cbFog	=	new ConstantBuffer( device, typeof(FOG_DATA) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
			Game.GraphicsDevice.DisplayBoundsChanged +=GraphicsDevice_DisplayBoundsChanged;
		}

		private void GraphicsDevice_DisplayBoundsChanged( object sender, EventArgs e )
		{
			fogDensity		.Clear(Vector4.Zero);
			scatteredLight0	.Clear(Vector4.Zero);
			scatteredLight1	.Clear(Vector4.Zero);
			volumeShadow	.Clear(Vector4.Zero);
			shadowHistory	.Clear(Vector4.Zero);
			integratedLight	.Clear(Vector4.Zero);
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("fog");
			factory		=	shader.CreateFactory( typeof(FogFlags) );

			CreateVolumeResources();
		}


		void CreateVolumeResources()
		{
			SafeDispose( ref fogDensity );
			SafeDispose( ref skyFogLut );
			SafeDispose( ref scatteredLight0 );
			SafeDispose( ref scatteredLight1 );
			SafeDispose( ref integratedLight );
			SafeDispose( ref volumeShadow );

			switch (fogQuality)
			{
				case QualityLevel.None:	   fogGridSize	=	new Size3(  16,   8,  64 );	break;
				case QualityLevel.Low:	   fogGridSize	=	new Size3(  64,  48,  64 );	break;
				case QualityLevel.Medium:  fogGridSize	=	new Size3( 160,  96,  64 );	break;
				case QualityLevel.High:	   fogGridSize	=	new Size3( 256, 192,  64 );	break;
				case QualityLevel.Ultra:   fogGridSize	=	new Size3( 256, 192, 128 );	break;
			}

			fogDensity		=	new Texture3DCompute( device, ColorFormat.Rgba8,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );
			scatteredLight0	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );
			scatteredLight1	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );
			integratedLight	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );
			skyFogLut		=	new RenderTarget2D  ( device, ColorFormat.Rgba16F,	fogGridSize.Width, fogGridSize.Height, true );

			volumeShadow	=	new Texture3DCompute( device, ColorFormat.Rg8,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );
			shadowHistory	=	new Texture3DCompute( device, ColorFormat.Rg8,	fogGridSize.Width, fogGridSize.Height, fogGridSize.Depth );

			int floatOne	=	0;//BitConverter.ToInt32( BitConverter.GetBytes(1.0f), 0 );

			device.Clear( scatteredLight0.UnorderedAccess, new Int4(0,0,0,floatOne) );
			device.Clear( scatteredLight1.UnorderedAccess, new Int4(0,0,0,floatOne) );
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) 
			{
				SafeDispose( ref fogDensity );
				SafeDispose( ref skyFogLut );
				SafeDispose( ref scatteredLight0 );
				SafeDispose( ref scatteredLight1 );
				SafeDispose( ref integratedLight );
				SafeDispose( ref volumeShadow );
				SafeDispose( ref shadowHistory );

				SafeDispose( ref cbFog );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="settings"></param>
		void SetupParameters ( Camera camera, LightSet lightSet, ShaderResource fogHistory )
		{
			var fogData		=	new FOG_DATA();
			var rw			=	rs.RenderWorld;

			if (fogQualityDirty)
			{
				CreateVolumeResources();
				fogQualityDirty = false;
			}

			fogData.WorldToVolume			=	rw.LightMap.WorldToVolume;
			fogData.IndirectLightFactor		=	rs.Radiosity.MasterIntensity;
			fogData.DirectLightFactor		=	rs.SkipDirectLighting ? 0 : 1;

			fogData.FogSizeInv				=	new Vector4( 1.0f / fogGridSize.Width, 1.0f / fogGridSize.Height, 1.0f / fogGridSize.Depth, 0 );
			fogData.FogGridExpK				=	FogGridExpK;
			fogData.FogColor				=	rs.Sky.MieColor;

			fogData.GroundFogColor			=	GroundFogColor;
			fogData.GroundFogHeight			=	GroundFogHeight;
			fogData.GroundFogDensity		=	GroundFogEnabled ? 0.693147f / GroundFogDistance : 0;
			fogData.GroundFogLevel			=	GroundFogLevel;

			fogData.FogSizeX				=	(uint)fogGridSize.Width;
			fogData.FogSizeY				=	(uint)fogGridSize.Height;
			fogData.FogSizeZ				=	(uint)fogGridSize.Depth;

			fogData.FogDensity				=	MathUtil.Exp2( rs.Sky.MieScale ) * Sky2.BetaMie.Red;
			fogData.FogHeight				=	rs.Sky.MieHeight;
			fogData.FogScale				=	MathUtil.Exp2( rs.Sky.APScale );

			fogData.SampleOffset			=	random.NextVector4( Vector4.Zero, Vector4.One );
			fogData.HistoryFactor			=	HistoryFactor;
			fogData.FadeoutDistanceInvSqr	=	(float)Math.Pow( 1.0f / FogFadeoutDistance, 2 );
			fogData.FrameCount				=	frameCounter;


			cbFog.SetData( fogData );

			device.ComputeConstants	[ regFog				]	=	cbFog;
			device.ComputeConstants	[ regSky				]	=	rs.Sky.SkyData;
			device.ComputeConstants	[ regCamera				]	=	camera.CameraData;
			device.ComputeConstants	[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants	[ regCascadeShadow		]	=	rs.ShadowSystem.GetCascadeShadowConstantBuffer();

			device.ComputeSamplers	[ regLinearClamp		]	=	SamplerState.LinearClamp;
			device.ComputeSamplers	[ regShadowSampler		]	=	SamplerState.ShadowSampler;
		
			device.ComputeResources	[ regFogHistory			]	=	fogHistory;
			device.ComputeResources	[ regClusterTable		]	=	rs.LightManager.LightGrid.GridTexture		;
			device.ComputeResources	[ regLightIndexTable	]	=	rs.LightManager.LightGrid.IndexDataGpu		;
			device.ComputeResources	[ regLightDataTable		]	=	rs.LightManager.LightGrid.LightDataGpu		;
			device.ComputeResources	[ regShadowMap			]	=	rs.ShadowSystem.ShadowMap.ShadowTextureLowRes		;
			device.ComputeResources	[ regShadowMask			]	=	rs.ShadowSystem.ShadowMap.ParticleShadowTextureLowRes	;
			device.ComputeResources	[ regLutAP				]	=	rs.Sky.LutAP;
			device.ComputeResources	[ regShadowHistory		]	=	shadowHistory;
		
			device.ComputeResources	[ regIrradianceVolumeL0	]	= 	rw.LightMap?.GetVolume(0);
			device.ComputeResources	[ regIrradianceVolumeL1	]	= 	rw.LightMap?.GetVolume(1);
			device.ComputeResources	[ regIrradianceVolumeL2	]	= 	rw.LightMap?.GetVolume(2);
			device.ComputeResources	[ regIrradianceVolumeL3	]	= 	rw.LightMap?.GetVolume(3);
		}

		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderFogVolume( Camera camera, LightSet lightSet )
		{
			using ( new PixEvent("Fog Volume") ) {
				
				using ( new PixEvent("Lighting") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, scatteredLight1 );

					device.PipelineState	=	factory[ (int)FogFlags.COMPUTE ];

					device.SetComputeUnorderedAccess( regFogTarget,			scatteredLight0.UnorderedAccess );
					device.SetComputeUnorderedAccess( regFogShadowTarget,	volumeShadow.UnorderedAccess );
					
					//var gx	=	MathUtil.IntDivUp( fogGridSize.Width,  BlockSizeX );
					//var gy	=	MathUtil.IntDivUp( fogGridSize.Height, BlockSizeY );
					//var gz	=	MathUtil.IntDivUp( fogGridSize.Depth,  BlockSizeZ );
					var gx	=	MathUtil.IntDivUp( fogGridSize.Width,  4 );
					var gy	=	MathUtil.IntDivUp( fogGridSize.Height, 4 );
					var gz	=	MathUtil.IntDivUp( fogGridSize.Depth,  4 );

					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Integrate") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, null );

					FogFlags flags = FogFlags.INTEGRATE;

					if (ShowSlices) flags |= FogFlags.SHOW_SLICE;

					device.PipelineState	=	factory[ (int)flags ];

					device.SetComputeUnorderedAccess( regFogTarget,		integratedLight.UnorderedAccess );
					device.SetComputeUnorderedAccess( regSkyFogLut,		skyFogLut.Surface.UnorderedAccess );
					device.ComputeResources			[ regFogSource ]	=	scatteredLight0;
					device.ComputeResources			[ regShadow ]		=	volumeShadow;
					
					var gx	=	MathUtil.IntDivUp( fogGridSize.Width,  8 );
					var gy	=	MathUtil.IntDivUp( fogGridSize.Height, 8 );
					var gz	=	1;

					device.Dispatch( gx, gy, gz );
				}

				Misc.Swap( ref scatteredLight0, ref scatteredLight1 );
				Misc.Swap( ref shadowHistory, ref volumeShadow );
				frameCounter++;
			}
		}
	}
}
