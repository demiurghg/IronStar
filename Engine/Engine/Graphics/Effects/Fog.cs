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
		[AECategory("Fog")]
		[AEValueRange(1, 256, 16, 1)]
		public float NumSamples { get; set; } = 64;


		[ShaderDefine]	const int FogSizeX	=	RenderSystem.FogGridWidth;
		[ShaderDefine]	const int FogSizeY	=	RenderSystem.FogGridHeight;
		[ShaderDefine]	const int FogSizeZ	=	RenderSystem.FogGridDepth;

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
		}


		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=256)]
		struct FOG_DATA 
		{
			public Vector4	WorldToVoxelScale;
			public Vector4	WorldToVoxelOffset;
			public Vector4	SampleOffset;
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
			fogDensity		=	new Texture3DCompute( device, ColorFormat.Rgba8,	FogSizeX, FogSizeY, FogSizeZ );
			scatteredLight0	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	FogSizeX, FogSizeY, FogSizeZ );
			scatteredLight1	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	FogSizeX, FogSizeY, FogSizeZ );
			integratedLight	=	new Texture3DCompute( device, ColorFormat.Rgba16F,	FogSizeX, FogSizeY, FogSizeZ );

			device.Clear( scatteredLight0.UnorderedAccess, Int4.Zero );
			device.Clear( scatteredLight1.UnorderedAccess, Int4.Zero );

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
		void SetupParameters ( Camera camera, LightSet lightSet, FogSettings settings, ShaderResource fogHistory )
		{
			var fogData		=	new FOG_DATA();

			fogData.WorldToVoxelOffset	=	rs.Radiosity.GetWorldToVoxelOffset();
			fogData.WorldToVoxelScale	=	rs.Radiosity.GetWorldToVoxelScale();
			fogData.IndirectLightFactor	=	rs.Radiosity.MasterIntensity;
			fogData.DirectLightFactor	=	rs.SkipDirectLighting ? 0 : 1;

			//fogData.FogDensity			=	FogDensity;
			//fogData.FogHeight			=	FogHeight;
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
		
			device.ComputeResources[ regFogHistory			]	=	fogHistory;
			device.ComputeResources[ regClusterTable		]	=	rs.LightManager.LightGrid.GridTexture		;
			device.ComputeResources[ regLightIndexTable		]	=	rs.LightManager.LightGrid.IndexDataGpu		;
			device.ComputeResources[ regLightDataTable		]	=	rs.LightManager.LightGrid.LightDataGpu		;
			device.ComputeResources[ regShadowMap			]	=	rs.LightManager.ShadowMap.ShadowTexture		;
			device.ComputeResources[ regShadowMask			]	=	rs.LightManager.ShadowMap.ParticleShadowTexture	;
		
			device.ComputeResources[ regIrradianceVolumeL0	]	= 	rs.Radiosity.LightVolumeL0	;
			device.ComputeResources[ regIrradianceVolumeL1	]	= 	rs.Radiosity.LightVolumeL1	;
			device.ComputeResources[ regIrradianceVolumeL2	]	= 	rs.Radiosity.LightVolumeL2	;
			device.ComputeResources[ regIrradianceVolumeL3	]	= 	rs.Radiosity.LightVolumeL3	;
		}


		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderFog( Camera camera, LightSet lightSet, FogSettings settings )
		{
			using ( new PixEvent("Fog") ) {
				
				using ( new PixEvent("Lighting") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, settings, scatteredLight1 );

					device.PipelineState	=	factory[ (int)FogFlags.COMPUTE ];

					device.SetComputeUnorderedAccess( regFogTarget, scatteredLight0.UnorderedAccess );

					
					var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
					var gz	=	MathUtil.IntDivUp( FogSizeZ, BlockSizeZ );

					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Integrate") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, settings, null );

					device.PipelineState	=	factory[ (int)FogFlags.INTEGRATE ];

					device.SetComputeUnorderedAccess( regFogTarget,			integratedLight.UnorderedAccess );
					device.ComputeResources			[ regFogSource ]	=	scatteredLight0;

					
					var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
					var gz	=	1;

					device.Dispatch( gx, gy, gz );
				}

				Misc.Swap( ref scatteredLight0, ref scatteredLight1 );
				frameCounter++;
			}
		}
	}
}
