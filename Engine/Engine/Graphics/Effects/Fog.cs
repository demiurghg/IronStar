﻿using System;
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

	[RequireShader("fog", true)]
	[ShaderSharedStructure(typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	internal partial class Fog : RenderComponent 
	{
		[Config]	
		[AECategory("Fog")]
		[AEValueRange(0, 0.1f, 0.01f, 0.001f)]
		public float FogDensity { get; set; } = 0;

		[Config]	
		[AECategory("Fog")]
		[AEValueRange(0, 1000, 50, 1)]
		public float FogHeight { get; set; } = 50;

		[Config]	
		[AECategory("Fog")]
		[AEValueRange(0, 1, 0.1f, 0.01f)]
		public float HistoryFactor { get; set; } = 0.8f;

		[Config]
		[AECategory("Fog Grid")]
		public int FogGridSizeX	{ get { return fogSizeX; } set { fogSizeX = MathUtil.Clamp(value, 8, 256 ); } }
		[Config]
		[AECategory("Fog Grid")]
		public int FogGridSizeY	{ get { return fogSizeY; } set { fogSizeY = MathUtil.Clamp(value, 8, 256 ); } }
		[Config]
		[AECategory("Fog Grid")]
		public int FogGridSizeZ { get { return fogSizeZ; } set { fogSizeZ = MathUtil.Clamp(value, 8, 256 ); } }
		int fogSizeX = 128;
		int fogSizeY = 96;
		int fogSizeZ = 192;

		[Config]
		[AECategory("Fog Grid")]
		public bool ShowSlices { get; set; }

		[Config]
		[AECategory("Fog Grid")]
		[AEValueRange(1, 5000, 100f, 1f)]
		public float FogGridHalfDepth { get { return fogGridHalfDepth; } set { fogGridHalfDepth = MathUtil.Clamp( value, 1, 5000 ); } }
		float fogGridHalfDepth = 300.0f;

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
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow			=	new CRegister( 4, "CascadeShadow"			);

		static FXSamplerState								regLinearClamp				=	new SRegister( 0, "LinearClamp"				);
		static FXSamplerComparisonState						regShadowSampler			=	new SRegister( 1, "ShadowSampler"			);
																										
		static FXTexture3D<Vector4>							regFogSource				=	new TRegister( 0, "FogSource"				);
		static FXTexture3D<Vector4>							regFogHistory				=	new TRegister( 1, "FogHistory"				);
		static FXTexture3D<UInt2>							regClusterTable				=	new TRegister( 2, "ClusterArray"			);
		static FXBuffer<uint>								regLightIndexTable			=	new TRegister( 3, "ClusterIndexBuffer"		);
		static FXStructuredBuffer<SceneRenderer.LIGHT>		regLightDataTable			=	new TRegister( 4, "ClusterLightBuffer"		);
		static FXTexture2D<Vector4>							regShadowMap				=	new TRegister( 5, "ShadowMap"				);
		static FXTexture2D<Vector4>							regShadowMask				=	new TRegister( 6, "ShadowMask"				);

		static FXTexture3D<Vector4>							regIrradianceVolumeL0		=	new TRegister(10, "IrradianceVolumeL0"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL1		=	new TRegister(11, "IrradianceVolumeL1"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL2		=	new TRegister(12, "IrradianceVolumeL2"		);
		static FXTexture3D<Vector4>							regIrradianceVolumeL3		=	new TRegister(13, "IrradianceVolumeL3"		);

		static FXRWTexture3D<Vector4>						regFogTarget				=	new URegister(0, "FogTarget"				);

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
			public Vector4	WorldToVoxelScale;
			public Vector4	WorldToVoxelOffset;
			public Vector4	SampleOffset;

			public Vector4	FogSizeInv;

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
		}

		Ubershader			shader;
		StateFactory		factory;
		ConstantBuffer		cbFog;
		Texture3DCompute	fogDensity;
		Texture3DCompute	scatteredLight0;
		Texture3DCompute	scatteredLight1;
		Texture3DCompute	integratedLight;
		uint				frameCounter;
		Random				random = new Random();

		public ShaderResource FogGrid { get { return integratedLight; } }
		public ConstantBuffer FogData { get { return cbFog; } }


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
			SafeDispose( ref fogDensity		 );
			SafeDispose( ref scatteredLight0 );
			SafeDispose( ref scatteredLight1 );
			SafeDispose( ref integratedLight );

			fogDensity		=	new Texture3DCompute( device, ColorFormat.Rgba8,	fogSizeX, fogSizeY, fogSizeZ );
			scatteredLight0	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogSizeX, fogSizeY, fogSizeZ );
			scatteredLight1	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogSizeX, fogSizeY, fogSizeZ );
			integratedLight	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	fogSizeX, fogSizeY, fogSizeZ );

			device.Clear( scatteredLight0.UnorderedAccess, Int4.Zero );
			device.Clear( scatteredLight1.UnorderedAccess, Int4.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing ) 
			{
				SafeDispose( ref scatteredLight0 );
				SafeDispose( ref scatteredLight1 );
				SafeDispose( ref integratedLight );
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

			fogData.WorldToVoxelOffset	=	rs.Radiosity.GetWorldToVoxelOffset();
			fogData.WorldToVoxelScale	=	rs.Radiosity.GetWorldToVoxelScale();
			fogData.IndirectLightFactor	=	rs.Radiosity.MasterIntensity;
			fogData.DirectLightFactor	=	rs.SkipDirectLighting ? 0 : 1;

			fogData.FogSizeInv			=	new Vector4( 1.0f / fogSizeX, 1.0f / fogSizeY, 1.0f / fogSizeZ, 0 );
			fogData.FogGridExpK			=	FogGridExpK;

			fogData.FogSizeX			=	(uint)fogSizeX;
			fogData.FogSizeY			=	(uint)fogSizeY;
			fogData.FogSizeZ			=	(uint)fogSizeZ;

			fogData.FogDensity			=	MathUtil.Exp2( rs.Sky.MieScale ) * Sky2.BetaMie.Red;
			fogData.FogHeight			=	rs.Sky.MieHeight;

			fogData.SampleOffset		=	random.NextVector4( Vector4.Zero, Vector4.One );
			fogData.HistoryFactor		=	HistoryFactor;
			fogData.FrameCount			=	frameCounter;


			cbFog.SetData( fogData );

			device.ComputeConstants	[ regFog				]	=	cbFog;
			device.ComputeConstants	[ regSky				]	=	rs.Sky.SkyData;
			device.ComputeConstants	[ regCamera				]	=	camera.CameraData;
			device.ComputeConstants	[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants	[ regCascadeShadow		]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();

			device.ComputeSamplers	[ regLinearClamp		]	=	SamplerState.LinearClamp;
			device.ComputeSamplers	[ regShadowSampler		]	=	SamplerState.ShadowSampler;
		
			device.ComputeResources	[ regFogHistory			]	=	fogHistory;
			device.ComputeResources	[ regClusterTable		]	=	rs.LightManager.LightGrid.GridTexture		;
			device.ComputeResources	[ regLightIndexTable	]	=	rs.LightManager.LightGrid.IndexDataGpu		;
			device.ComputeResources	[ regLightDataTable		]	=	rs.LightManager.LightGrid.LightDataGpu		;
			device.ComputeResources	[ regShadowMap			]	=	rs.LightManager.ShadowMap.ShadowTexture		;
			device.ComputeResources	[ regShadowMask			]	=	rs.LightManager.ShadowMap.ParticleShadowTexture	;
		
			device.ComputeResources	[ regIrradianceVolumeL0	]	= 	rs.Radiosity.LightVolumeL0	;
			device.ComputeResources	[ regIrradianceVolumeL1	]	= 	rs.Radiosity.LightVolumeL1	;
			device.ComputeResources	[ regIrradianceVolumeL2	]	= 	rs.Radiosity.LightVolumeL2	;
			device.ComputeResources	[ regIrradianceVolumeL3	]	= 	rs.Radiosity.LightVolumeL3	;
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

					device.SetComputeUnorderedAccess( regFogTarget, scatteredLight0.UnorderedAccess );

					
					var gx	=	MathUtil.IntDivUp( FogGridSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogGridSizeY, BlockSizeY );
					var gz	=	MathUtil.IntDivUp( FogGridSizeZ, BlockSizeZ );

					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Integrate") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, null );

					FogFlags flags = FogFlags.INTEGRATE;

					if (ShowSlices) flags |= FogFlags.SHOW_SLICE;

					device.PipelineState	=	factory[ (int)flags ];

					device.SetComputeUnorderedAccess( regFogTarget,			integratedLight.UnorderedAccess );
					device.ComputeResources			[ regFogSource ]	=	scatteredLight0;

					
					var gx	=	MathUtil.IntDivUp( FogGridSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogGridSizeY, BlockSizeY );
					var gz	=	1;

					device.Dispatch( gx, gy, gz );
				}

				Misc.Swap( ref scatteredLight0, ref scatteredLight1 );
				frameCounter++;
			}
		}
	}
}
