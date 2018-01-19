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

		[ShaderDefine]
		const int FogSizeX		=	128;

		[ShaderDefine]
		const int FogSizeY		=	64;

		[ShaderDefine]
		const int FogSizeZ		=	192;

		[ShaderDefine]
		const int BlockSizeX	=	4;

		[ShaderDefine]
		const int BlockSizeY	=	4;

		[ShaderDefine]
		const int BlockSizeZ	=	4;

		[ShaderDefine]	public const uint LightTypeOmni			=	SceneRenderer.LightTypeOmni;
		[ShaderDefine]	public const uint LightTypeSpotShadow	=	SceneRenderer.LightTypeSpotShadow;
		[ShaderDefine]	public const uint LightSpotShapeRound	=	SceneRenderer.LightSpotShapeRound;
		[ShaderDefine]	public const uint LightSpotShapeSquare	=	SceneRenderer.LightSpotShapeSquare;



		[Flags]
		enum FogFlags : int
		{
			COMPUTE		= 0x0001,
			INTEGRATE	= 0x0002,
		}

		//	row_major float4x4 MatrixWVP;      // Offset:    0 Size:    64 [unused]
		//	float3 SunPosition;                // Offset:   64 Size:    12
		//	float4 SunColor;                   // Offset:   80 Size:    16
		//	float Turbidity;                   // Offset:   96 Size:     4 [unused]
		//	float3 Temperature;                // Offset:  100 Size:    12
		//	float SkyIntensity;                // Offset:  112 Size:     4
		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit, Size=160)]
		struct FogConsts {
			[FieldOffset(  0)] public Matrix 	MatrixWVP;
			[FieldOffset( 64)] public Vector3	SunPosition;
			[FieldOffset( 80)] public Color4	SunColor;
			[FieldOffset( 96)] public float		Turbidity;
			[FieldOffset(100)] public Vector3	Temperature; 
			[FieldOffset(112)] public float		SkyIntensity; 
			[FieldOffset(116)] public Vector3	Ambient;
			[FieldOffset(128)] public float		Time;
			[FieldOffset(132)] public Vector3	ViewPos;
			[FieldOffset(136)] public float		SunAngularSize;

		}


		Ubershader			shader;
		StateFactory		factory;

		ConstantBuffer		paramsCB;

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
			fog3d0		=	new Texture3DCompute( device, FogSizeX, FogSizeY, FogSizeZ );
			fog3d1		=	new Texture3DCompute( device, FogSizeX, FogSizeY, FogSizeZ );

			paramsCB	=	new ConstantBuffer( device, typeof(PARAMS) );

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
				SafeDispose( ref paramsCB );
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
			PARAMS param					=	new PARAMS();

			var view						=	camera.GetViewMatrix( StereoEye.Mono );
			var projection					=	camera.GetProjectionMatrix( StereoEye.Mono );
			var cameraMatrix				=	camera.GetCameraMatrix( StereoEye.Mono );

			param.View						=	view;
			param.Projection				=   projection;
			param.ViewProjection			=	view * projection;
			param.CameraMatrix				=	camera.GetCameraMatrix( StereoEye.Mono );

			param.CascadeViewProjection0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ViewProjectionMatrix;
			param.CascadeViewProjection1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ViewProjectionMatrix;
			param.CascadeViewProjection2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ViewProjectionMatrix;
			param.CascadeViewProjection3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ViewProjectionMatrix;
			param.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
			param.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
			param.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
			param.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;
			param.DirectLightDirection		=	new Vector4( lightSet.DirectLight.Direction, 0 );
			param.DirectLightIntensity		=	lightSet.DirectLight.Intensity;
			param.CameraForward				=	new Vector4( cameraMatrix.Forward	, 0 );
			param.CameraRight				=	new Vector4( cameraMatrix.Right		, 0 );
			param.CameraUp					=	new Vector4( cameraMatrix.Up		, 0 );
			param.CameraPosition			=	new Vector4( cameraMatrix.TranslationVector	, 1 );

			param.CameraTangentX			=	camera.CameraTangentX;
			param.CameraTangentY			=	camera.CameraTangentY;

			//	copy to gpu :
			paramsCB.SetData( param );

			//	setup resources :
			device.ComputeShaderResources[1]		=	rs.LightManager.LightGrid.GridTexture;
			device.ComputeShaderResources[2]		=	rs.LightManager.LightGrid.IndexDataGpu;
			device.ComputeShaderResources[3]		=	rs.LightManager.LightGrid.LightDataGpu;
			device.ComputeShaderResources[4]		=	rs.LightManager.ShadowMap.ColorBuffer;
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

					device.ComputeShaderResources[0]	=	fog3d0;
					device.ComputeShaderConstants[0]	=	paramsCB;
					device.ComputeShaderSamplers[0]		=	SamplerState.LinearClamp;
					device.ComputeShaderSamplers[0]		=	SamplerState.ShadowSampler;

					device.SetCSRWTexture( 0, fog3d1 );

					var gx	=	MathUtil.IntDivUp( FogSizeX, BlockSizeX );
					var gy	=	MathUtil.IntDivUp( FogSizeY, BlockSizeY );
					var gz	=	MathUtil.IntDivUp( FogSizeZ, BlockSizeZ );

					#warning D3D11 ERROR: ID3D11DeviceContext::Dispatch: The Compute Shader unit expects a Sampler configured for comparison filtering to be set at Slot 1, but the sampler bound at this slot is configured for default filtering.  This mismatch will produce undefined behavior if the sampler is used (e.g. it is not skipped due to shader code branching). [ EXECUTION ERROR #390: DEVICE_DRAW_SAMPLER_MISMATCH]
					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Integrate") ) {

					device.ResetStates();		  
			
					device.PipelineState	=	factory[ (int)FogFlags.INTEGRATE ];

					device.ComputeShaderResources[0]	=	fog3d1;
					device.ComputeShaderConstants[0]	=	paramsCB;

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
