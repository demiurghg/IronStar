using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Content;
using Fusion.Core.Configuration;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;
using Fusion.Engine.Graphics.Lights;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.Collections;
using System.Diagnostics;

namespace Fusion.Engine.Graphics.GI
{
	[ConfigClass]
	[RequireShader("relight", true)]
	internal partial class LightProbeRelighter : RenderComponent
	{
		static FXConstantBuffer<RELIGHT_PARAMS> regParams = new CRegister( 0, "RelightParams" );
		
		static FXTextureCubeArray<Vector4>	regGBufferColorData		=	new TRegister(0, "GBufferColorData"		);
		static FXTextureCubeArray<Vector4>	regGBufferNormalData	=	new TRegister(1, "GBufferNormalData"	);
		static FXTexture2D<Vector4>			regLightMap0			=	new TRegister(2, "LightMap0"			);
		static FXTexture2D<Vector4>			regLightMap1			=	new TRegister(3, "LightMap1"			);
		static FXTextureCube<Vector4>		regSkyCube				=	new TRegister(4, "SkyCube"				);

		static FXSamplerState				regPointSampler			= 	new SRegister(0, "PointSampler"			);
		static FXSamplerState				regLinearSampler		= 	new SRegister(1, "LinearSampler"		);
		static FXSamplerComparisonState		regShadowSampler		= 	new SRegister(2, "ShadowSampler"		);

		static FXRWTexture2DArray<Vector4>	regTargetCube			=	new URegister(0, "TargetCube"			); 

		[ShaderDefine]	 const int BlockSizeX = 16;
		[ShaderDefine]	 const int BlockSizeY = 16;
		[ShaderDefine]	 const int PrefilterBlockSizeX = 8;
		[ShaderDefine]	 const int PrefilterBlockSizeY = 8;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=64)]
		struct RELIGHT_PARAMS {
			public	uint	CubeIndex;
			public	float	RadiosityLevel;
			public	float	Dummy0;
			public	float	Dummy1;
		}


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=16)]
		struct LIGHTPROBE_DATA {
			public	Vector4	Position;
		}



		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbRelightParams;
		ConstantBuffer	cbLightProbeData;


		enum Flags {
			RELIGHT			=	0x0001,
			PREFILTER		=	0x0002,
			SPECULAR		=	0x0004,
			DIFFUSE			=	0x0008,
			AMBIENT			=	0x0010,

			ROUGHNESS_025	=	0x0100,
			ROUGHNESS_050	=	0x0200,
			ROUGHNESS_075	=	0x0400,
			ROUGHNESS_100	=	0x0800,
		}
		
		[Config]
		static public int MaxLPPF { get; set; } = 8;


		public LightProbeRelighter( RenderSystem rs ) : base(rs)
		{
		}


		public override void Initialize()
		{
			cbRelightParams		=	new ConstantBuffer( rs.Device, typeof(RELIGHT_PARAMS) );
			cbLightProbeData	=	new ConstantBuffer( rs.Device, typeof(LIGHTPROBE_DATA), RenderSystem.LightProbeBatchSize );

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			SafeDispose( ref factory );

			shader	=	Game.Content.Load<Ubershader>("relight");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}






		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbRelightParams );
				SafeDispose( ref cbLightProbeData );
				SafeDispose( ref factory );
			}

			base.Dispose( disposing );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Light probe relighting :
		-----------------------------------------------------------------------------------------*/

		public void RelightLightProbes ( LightSet lightSet, Camera camera )
		{
			var cubeMapFilter	=	Game.GetService<CubeMapFilter>();

			var gbufferColor	=	rs.LightMapResources.LightProbeColorArray;
			var gbufferMapping	=	rs.LightMapResources.LightProbeMappingArray;
			var radianceArray	=	rs.LightMapResources.LightProbeRadianceArray;
			var radianceTemp	=	rs.LightMapResources.LightProbeRadiance;

			var frustum			=	camera.Frustum;

			foreach ( var lightProbe in lightSet.LightProbes )
			{
				ScoreLightProbe( lightProbe, ref frustum );
			}

			var lightProbesToRelight = lightSet.LightProbes
				.OrderByDescending( lpb => lpb.RelightScore )
				.Take( MaxLPPF )
				.ToArray();

			using ( new PixEvent( "Relight Light Probes" ) )
			{
				foreach ( var lightProbe in lightProbesToRelight )
				{
					lightProbe.RelightScore *= 0.5f;

					if (lightProbe.ImageIndex<0) continue;

					//var target = radianceArray.GetBatchCubeSurface( lightProbe.ImageIndex, 0 ).UnorderedAccess;
					RelightLightProbe( gbufferColor, gbufferMapping, lightProbe, radianceTemp.GetCubeSurface(0).UnorderedAccess );

					cubeMapFilter.GenerateCubeMipLevel( radianceTemp );
					//radianceTemp.BuildMipmaps();

					rs.Device.ResetStates();

					cubeMapFilter.PrefilterLightProbe( radianceTemp, radianceArray, lightProbe.ImageIndex );
				}
			}
		}



		public void ScoreLightProbe( LightProbe lightProbe, ref BoundingFrustum frustum )
		{
			lightProbe.RelightScore++;

			if ( frustum.Contains( lightProbe.BoundingBox ) == ContainmentType.Disjoint )
			{
				lightProbe.RelightScore *= 0.25f;
			}

			if (lightProbe.ImageIndex<0)
			{
				lightProbe.RelightScore = 0;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void RelightLightProbe ( TextureCubeArray colorData, TextureCubeArray normalData, LightProbe lightProbe, UnorderedAccess target )
		{
			//	skip unassigned cubemaps :
			if (lightProbe.ImageIndex<0) return;
			
			using ( new PixEvent( "LightProbe #" + lightProbe.ImageIndex.ToString() ) ) 
			{

				var relightParams	=	new RELIGHT_PARAMS();
				var radiosity		=	Game.GetService<Radiosity>();

				relightParams.CubeIndex			=	(uint)lightProbe.ImageIndex;
				relightParams.RadiosityLevel	=	Radiosity.MasterIntensity;// - Radiosity.SecondBounce;
				cbRelightParams.SetData( ref relightParams );


				device.ComputeConstants	[ regParams				]	=	cbRelightParams;

				device.ComputeResources	[ regGBufferColorData	]	=	colorData;
				device.ComputeResources	[ regGBufferNormalData	]	=	normalData;
				device.ComputeResources	[ regLightMap0			]	=	rs.RenderWorld.LightMap.GetLightmap(0);
				device.ComputeResources	[ regLightMap1			]	=	rs.RenderWorld.LightMap.GetLightmap(0);
				device.ComputeResources	[ regSkyCube			]	=	rs.Sky.SkyCube;
				device.ComputeSamplers	[ regPointSampler		]	=	SamplerState.PointClamp;
				device.ComputeSamplers	[ regLinearSampler		]	=	SamplerState.LinearWrap;
				device.ComputeSamplers	[ regShadowSampler		]	=	SamplerState.ShadowSamplerPoint;
					
				device.SetComputeUnorderedAccess( regTargetCube, target );
				
				device.PipelineState = factory[(int)Flags.RELIGHT];

				int size	=	(int)RenderSystem.LightProbeSize;
					
				int tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
				int tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
				int tgz		=	1;

				device.Dispatch( tgx, tgy, tgz );
			}
		}
	}
}
