using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.IO;
using Fusion.Engine.Graphics.Ubershaders;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using Native.Embree;
using Fusion.Engine.Graphics.Lights;

namespace Fusion.Engine.Graphics {

	[RequireShader("relight", true)]
	internal partial class LightManager : RenderComponent 
	{
		static FXConstantBuffer<RELIGHT_PARAMS> regParams = new CRegister( 0, "RelightParams" );
		

		[ShaderDefine]
		const int BlockSizeX = 16;

		[ShaderDefine]
		const int BlockSizeY = 16;

		[ShaderDefine]
		const int PrefilterBlockSizeX = 8;

		[ShaderDefine]
		const int PrefilterBlockSizeY = 8;

		[ShaderDefine]
		const int LightProbeSize = RenderSystem.LightProbeSize;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=192)]
		struct RELIGHT_PARAMS {
			public	Matrix	ShadowViewProjection;
			public	Vector4	LightProbePosition;
			public	Color4	DirectLightIntensity;
			public	Vector4	DirectLightDirection;
			public	Vector4	ShadowRegion;
			public	Color4	SkyAmbient;
			public	float	CubeIndex;
			public	float	Roughness;
			public	float	TargetSize;
		}


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=16)]
		struct LIGHTPROBE_DATA {
			public	Vector4	Position;
		}


		public LightGrid LightGrid {
			get { return lightGrid; }
		}
		LightGrid lightGrid;


		public ShadowMap ShadowMap {
			get { return shadowMap; }
		}
		ShadowMap shadowMap;

		public ConstantBuffer DirectLightData {
			get { return cbDirectLightData; }
		}


		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	cbRelightParams;
		ConstantBuffer	cbLightProbeData;

		ConstantBuffer	cbDirectLightData;


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
		


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		public LightManager( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			lightGrid	=	new LightGrid( rs, 16, 8, 24 );

			shadowMap	=	new ShadowMap( rs, rs.ShadowQuality );

			cbRelightParams		=	new ConstantBuffer( rs.Device, typeof(RELIGHT_PARAMS) );
			cbLightProbeData	=	new ConstantBuffer( rs.Device, typeof(LIGHTPROBE_DATA), RenderSystem.LightProbeBatchSize );
			cbDirectLightData	=	new ConstantBuffer( rs.Device, typeof(GpuData.DIRECT_LIGHT) );

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



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref cbRelightParams );
				SafeDispose( ref cbLightProbeData );
				SafeDispose( ref factory );
				SafeDispose( ref lightGrid );
				SafeDispose( ref shadowMap );
				SafeDispose( ref cbDirectLightData );
			}

			base.Dispose( disposing );
		}





		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="lightSet"></param>
		public void Update ( GameTime gameTime, LightSet lightSet, IEnumerable<RenderInstance> instances )
		{
			if (shadowMap.ShadowQuality!=rs.ShadowQuality) {
				SafeDispose( ref shadowMap );
				shadowMap	=	new ShadowMap( rs, rs.ShadowQuality );
			}


			foreach ( var omni in lightSet.OmniLights ) {
				omni.Timer += (uint)gameTime.Elapsed.TotalMilliseconds;
				if (omni.Timer<0) omni.Timer = 0;
			}

			foreach ( var spot in lightSet.SpotLights ) {
				spot.Timer += (uint)gameTime.Elapsed.TotalMilliseconds;
				if (spot.Timer<0) spot.Timer = 0;
			}

			//	update direct light CB :
			GpuData.DIRECT_LIGHT directLightData =	new GpuData.DIRECT_LIGHT();
			directLightData.DirectLightDirection	=	new Vector4( rs.RenderWorld.LightSet.DirectLight.Direction, 0 );
			directLightData.DirectLightIntensity	=	rs.RenderWorld.LightSet.DirectLight.Intensity;
			directLightData.DirectLightAngularSize	=	rs.RenderWorld.LightSet.DirectLight.AngularSize;

			cbDirectLightData.SetData( directLightData );
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Light probe relighting :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		public void RelightLightProbe ( TextureCubeArrayRW colorData, TextureCubeArrayRW normalData, LightProbe lightProbe, LightSet lightSet, Color4 skyAmbient, TextureCubeArrayRW target )
		{
			using ( new PixEvent( "RelightLightProbe" ) ) {

				var relightParams	=	new RELIGHT_PARAMS();
				/*var lightProbeData	=	new LIGHTPROBE_DATA[ RenderSystem.LightProbeBatchSize ];*/

				var cubeIndex	=	lightProbe.ImageIndex;

				relightParams.CubeIndex				=	lightProbe.ImageIndex;
				relightParams.LightProbePosition	=	new Vector4( lightProbe.ProbeMatrix.TranslationVector, 1 );
				relightParams.ShadowViewProjection	=	shadowMap.GetLessDetailedCascade().ViewProjectionMatrix;
				relightParams.DirectLightDirection	=	new Vector4( lightSet.DirectLight.Direction, 0 );
				relightParams.DirectLightIntensity	=	lightSet.DirectLight.Intensity;
				relightParams.SkyAmbient			=	skyAmbient;
				relightParams.ShadowRegion			=	shadowMap.GetLessDetailedCascade().ShadowScaleOffset;

				cbRelightParams.SetData( ref relightParams );
				/*cbLightProbeData.SetData( lightProbeData );*/

				device.ComputeResources[0]    =   colorData;
				device.ComputeResources[1]    =   normalData;
				device.ComputeResources[2]    =   rs.Sky.SkyCube;
				device.ComputeResources[3]	=	shadowMap.ShadowTexture;
				device.ComputeResources[4]	=	null;
				device.ComputeResources[5]	=	null;
				device.ComputeSamplers[0]		=	SamplerState.PointClamp;
				device.ComputeSamplers[1]		=	SamplerState.LinearWrap;
				device.ComputeSamplers[2]		=	SamplerState.ShadowSamplerPoint;
				
				device.ComputeConstants[0]	=	cbRelightParams;
				/*device.ComputeShaderConstants[1]	=	cbLightProbeData;*/
					
				device.SetComputeUnorderedAccess( 0, target.GetSingleCubeSurface( cubeIndex, 0 ).UnorderedAccess );
				
				device.PipelineState = factory[(int)Flags.RELIGHT];

				int size	=	RenderSystem.LightProbeSize;
					
				int tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
				int tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
				int tgz		=	1;

				device.Dispatch( tgx, tgy, tgz );
			}
		}



		public void PrefilterLightProbesAll( LightSet lightSet, TextureCubeArrayRW target )
		{
			int batchCount = RenderSystem.MaxEnvLights / RenderSystem.LightProbeBatchSize;

			for ( int batch = 0; batch < batchCount; batch++ ) {
				PrefilterLightProbes( lightSet, target, batch );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="lightSet"></param>
		/// <param name="target"></param>
		public void PrefilterLightProbes ( LightSet lightSet, TextureCubeArrayRW target, int counter )
		{
			device.ResetStates();
			
			using ( new PixEvent( "PrefilterLightProbes" ) ) {

				int batchCount = RenderSystem.MaxEnvLights / RenderSystem.LightProbeBatchSize;

				//for ( int i=0; i<batchCount; i++ ) {

				int batchIndex = counter % batchCount;

				device.ComputeSamplers[0]		=	SamplerState.PointClamp;
				device.ComputeSamplers[1]		=	SamplerState.LinearWrap;
				device.ComputeSamplers[2]		=	SamplerState.ShadowSamplerPoint;

				//
				//	prefilter specular :
				//
				for (int mip=1; mip<=RenderSystem.LightProbeMaxSpecularMip; mip++) {

					Flags flag;

					switch (mip) {
						case 1:	 flag = Flags.PREFILTER | Flags.SPECULAR | Flags.ROUGHNESS_025; break;
						case 2:	 flag = Flags.PREFILTER | Flags.SPECULAR | Flags.ROUGHNESS_050; break;
						case 3:	 flag = Flags.PREFILTER | Flags.SPECULAR | Flags.ROUGHNESS_075; break;
						case 4:	 flag = Flags.PREFILTER | Flags.SPECULAR | Flags.ROUGHNESS_100; break;
						default: flag = Flags.PREFILTER | Flags.SPECULAR | Flags.ROUGHNESS_100;	break;
					}
					
					device.PipelineState = factory[(int)flag];
				
					device.SetComputeUnorderedAccess( 0, target.GetBatchCubeSurface( batchIndex, mip ).UnorderedAccess );

					device.ComputeResources[4]	=	target.GetBatchCubeShaderResource( batchIndex, mip - 1 );

					int size	=	RenderSystem.LightProbeSize >> mip;
					int tgx		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeX );
					int tgy		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeY );
					int tgz		=	RenderSystem.LightProbeBatchSize;

					device.Dispatch( tgx, tgy, tgz );
				}

				//
				//	prefilter diffuse :
				//
				if (true) {
					device.PipelineState = factory[(int)(Flags.PREFILTER | Flags.DIFFUSE)];

					device.SetComputeUnorderedAccess( 0, target.GetBatchCubeSurface( batchIndex, RenderSystem.LightProbeMaxMips ).UnorderedAccess );

					device.ComputeResources[4]	=	target.GetBatchCubeShaderResource( batchIndex, 3 );

					int size	=	RenderSystem.LightProbeSize;
					int tgx		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeX );
					int tgy		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeY );
					int tgz		=	RenderSystem.LightProbeBatchSize;

					device.Dispatch( tgx, tgy, tgz );
				}
			}
		}
	}
}
