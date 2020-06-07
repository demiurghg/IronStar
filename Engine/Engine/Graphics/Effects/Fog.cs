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


namespace Fusion.Engine.Graphics {

	[RequireShader("fog", true)]
	[ShaderSharedStructure(typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	internal partial class Fog : RenderComponent {

		[ShaderDefine]	const int FogSizeX	=	RenderSystem.FogGridWidth;
		[ShaderDefine]	const int FogSizeY	=	RenderSystem.FogGridHeight;
		[ShaderDefine]	const int FogSizeZ	=	RenderSystem.FogGridDepth;

		[ShaderDefine]
		const int BlockSizeX	=	4;

		[ShaderDefine]
		const int BlockSizeY	=	4;

		[ShaderDefine]
		const int BlockSizeZ	=	4;

		static FXConstantBuffer<PARAMS>						regFog						=	new CRegister( 0, "Fog"						);
		static FXConstantBuffer<GpuData.CAMERA>				regCamera					=	new CRegister( 1, "Camera"					);
		static FXConstantBuffer<GpuData.DIRECT_LIGHT>		regDirectLight				=	new CRegister( 4, "DirectLight"				);
		static FXConstantBuffer<ShadowMap.CASCADE_SHADOW>	regCascadeShadow			=	new CRegister( 5, "CascadeShadow"			);

		static FXSamplerState								regLinearClamp				=	new SRegister( 0, "LinearClamp"				);
		static FXSamplerComparisonState						regShadowSampler			=	new SRegister( 1, "ShadowSampler"			);
																										
		static FXTexture3D<Vector4>							regFogSource				=	new TRegister( 0, "FogSource"				);
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
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct FOG_DATA {
			public float 	Dummy;
		}


		Ubershader			shader;
		StateFactory		factory;
		ConstantBuffer		cbFog;
		Texture3DCompute	fog3d0;
		Texture3DCompute	fog3d1;

		public ShaderResource FogGrid {
			get {
				return fog3d0;
			}
		}


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
			fog3d0	=	new Texture3DCompute( device, ColorFormat.Rgba16F, FogSizeX, FogSizeY, FogSizeZ );
			fog3d1	=	new Texture3DCompute( device, ColorFormat.Rgba16F, FogSizeX, FogSizeY, FogSizeZ );

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
			if( disposing ) {
				SafeDispose( ref fog3d0 );
				SafeDispose( ref fog3d1 );
				SafeDispose( ref cbFog );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="settings"></param>
		void SetupParameters ( Camera camera, LightSet lightSet, FogSettings settings )
		{
			var fogData		=	new FOG_DATA();

			fogData.Dummy	=	0;

			cbFog.SetData( fogData );

			device.ComputeConstants	[ regFog				]	=	cbFog;
			device.ComputeConstants	[ regCamera				]	=	camera.CameraData;
			device.ComputeConstants	[ regDirectLight		]	=	rs.LightManager.DirectLightData;
			device.ComputeConstants	[ regCascadeShadow		]	=	rs.LightManager.ShadowMap.GetCascadeShadowConstantBuffer();

			device.ComputeSamplers	[ regLinearClamp		]	=	SamplerState.LinearClamp;
			device.ComputeSamplers	[ regShadowSampler		]	=	SamplerState.ShadowSampler;
		
			device.ComputeResources[ regClusterTable		]	=	rs.LightManager.LightGrid.GridTexture		;
			device.ComputeResources[ regLightIndexTable		]	=	rs.LightManager.LightGrid.IndexDataGpu		;
			device.ComputeResources[ regLightDataTable		]	=	rs.LightManager.LightGrid.LightDataGpu		;
			device.ComputeResources[ regShadowMap			]	=	rs.LightManager.ShadowMap.ShadowTexture		;

			device.ComputeResources[ regShadowMask			]	=	rs.LightManager.ShadowMap.ParticleShadowTexture	;
		
			device.ComputeResources[ regIrradianceVolumeL0 ]	= 	rs.Radiosity.LightVolumeL0	;
			device.ComputeResources[ regIrradianceVolumeL1 ]	= 	rs.Radiosity.LightVolumeL1	;
			device.ComputeResources[ regIrradianceVolumeL2 ]	= 	rs.Radiosity.LightVolumeL2	;
			device.ComputeResources[ regIrradianceVolumeL3 ]	= 	rs.Radiosity.LightVolumeL3	;
		}


		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderFog( Camera camera, LightSet lightSet, FogSettings settings )
		{
			using ( new PixEvent("Fog") ) {
				
				using ( new PixEvent("Lighting") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet, settings );

					device.PipelineState	=	factory[ (int)FogFlags.COMPUTE ];

					var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
					var gz	=	MathUtil.IntDivUp( FogSizeZ, BlockSizeZ );

					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Integrate") ) {

					device.ResetStates();		  
			
					device.PipelineState	=	factory[ (int)FogFlags.INTEGRATE ];

					device.ComputeResources[0]	=	fog3d1;
					device.ComputeConstants[0]	=	cbFog;

					device.SetCSRWTexture( 0, fog3d0 );

					var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
					var gz	=	1;

					device.Dispatch( gx, gy, gz );
				}
			}
		}
	}
}
