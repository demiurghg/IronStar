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

	[RequireShader("lpv", true)]
	[ShaderSharedStructure(typeof(SceneRenderer.LIGHT), typeof(SceneRenderer.LIGHTINDEX))]
	internal partial class LpvRenderer : RenderComponent {

		[ShaderDefine]
		const int LpvSize		=	128;

		[ShaderDefine]
		const int BlockSize		=	4;


		[Flags]
		enum LpvFlags : int
		{
			INJECT		= 0x0001,
			PROPAGATE	= 0x0002,
		}

		//	row_major float4x4 MatrixWVP;      // Offset:    0 Size:    64 [unused]
		//	float3 SunPosition;                // Offset:   64 Size:    12
		//	float4 SunColor;                   // Offset:   80 Size:    16
		//	float Turbidity;                   // Offset:   96 Size:     4 [unused]
		//	float3 Temperature;                // Offset:  100 Size:    12
		//	float SkyIntensity;                // Offset:  112 Size:     4
		[ShaderStructure]
		[StructLayout(LayoutKind.Explicit, Size=160)]
		struct LPVDATA {
			[FieldOffset(  0)] public Matrix 	MatrixWVP;
		}


		Ubershader			shader;
		StateFactory		factory;

		ConstantBuffer		paramsCB;

		Texture3DCompute	fog3d0;
		Texture3DCompute	fog3d1;

		RenderTarget2D		dummyTarget;

		public Texture3DCompute AmbientLight {
			get {
				return fog3d0;
			}
		}


		public RenderTarget2D DummyTarget {
			get { return dummyTarget; }
		}


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rs"></param>
		public LpvRenderer ( RenderSystem rs ) : base( rs )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize() 
		{
			fog3d0		=	new Texture3DCompute( device, LpvSize, LpvSize, LpvSize );
			fog3d1		=	new Texture3DCompute( device, LpvSize, LpvSize, LpvSize );
			dummyTarget	=	new RenderTarget2D( device, ColorFormat.Bgra8, LpvSize, LpvSize );

			paramsCB	=	new ConstantBuffer( device, typeof(LPVDATA) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}



		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			shader		=	Game.Content.Load<Ubershader>("lpv");
			factory		=	shader.CreateFactory( typeof(LpvFlags) );
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
				SafeDispose( ref dummyTarget );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="settings"></param>
		void SetupParameters ( Camera camera, LightSet lightSet )
		{
			//PARAMS param					=	new PARAMS();

			//var view						=	camera.GetViewMatrix( StereoEye.Mono );
			//var projection					=	camera.GetProjectionMatrix( StereoEye.Mono );
			//var cameraMatrix				=	camera.GetCameraMatrix( StereoEye.Mono );

			//param.View						=	view;
			//param.Projection				=   projection;
			//param.ViewProjection			=	view * projection;
			//param.CameraMatrix				=	camera.GetCameraMatrix( StereoEye.Mono );

			//param.CascadeViewProjection0	=	rs.LightManager.ShadowMap.GetCascade( 0 ).ViewProjectionMatrix;
			//param.CascadeViewProjection1	=	rs.LightManager.ShadowMap.GetCascade( 1 ).ViewProjectionMatrix;
			//param.CascadeViewProjection2	=	rs.LightManager.ShadowMap.GetCascade( 2 ).ViewProjectionMatrix;
			//param.CascadeViewProjection3	=	rs.LightManager.ShadowMap.GetCascade( 3 ).ViewProjectionMatrix;
			//param.CascadeScaleOffset0		=	rs.LightManager.ShadowMap.GetCascade( 0 ).ShadowScaleOffset;
			//param.CascadeScaleOffset1		=	rs.LightManager.ShadowMap.GetCascade( 1 ).ShadowScaleOffset;
			//param.CascadeScaleOffset2		=	rs.LightManager.ShadowMap.GetCascade( 2 ).ShadowScaleOffset;
			//param.CascadeScaleOffset3		=	rs.LightManager.ShadowMap.GetCascade( 3 ).ShadowScaleOffset;
			//param.DirectLightDirection		=	new Vector4( lightSet.DirectLight.Direction, 0 );
			//param.DirectLightIntensity		=	lightSet.DirectLight.Intensity;
			//param.CameraForward				=	new Vector4( cameraMatrix.Forward	, 0 );
			//param.CameraRight				=	new Vector4( cameraMatrix.Right		, 0 );
			//param.CameraUp					=	new Vector4( cameraMatrix.Up		, 0 );
			//param.CameraPosition			=	new Vector4( cameraMatrix.TranslationVector	, 1 );

			//param.CameraTangentX			=	camera.CameraTangentX;
			//param.CameraTangentY			=	camera.CameraTangentY;

			////	copy to gpu :
			//paramsCB.SetData( param );

			////	setup resources :
			//device.ComputeShaderResources[1]		=	rs.LightManager.LightGrid.GridTexture;
			//device.ComputeShaderResources[2]		=	rs.LightManager.LightGrid.IndexDataGpu;
			//device.ComputeShaderResources[3]		=	rs.LightManager.LightGrid.LightDataGpu;
			//device.ComputeShaderResources[4]		=	rs.LightManager.ShadowMap.ColorBuffer;
		}



		/// <summary>
		/// Renders fog look-up table
		/// </summary>
		internal void RenderLpv( Camera camera, LightSet lightSet )
		{
			using ( new PixEvent("LPV") ) {
				
				using ( new PixEvent("Injection") ) {

					device.ResetStates();		  
			
					SetupParameters( camera, lightSet );

					device.PipelineState	=	factory[ (int)LpvFlags.INJECT ];

					device.ComputeShaderResources[0]	=	fog3d0;
					device.ComputeShaderConstants[0]	=	paramsCB;

					device.SetCSRWTexture( 0, fog3d1 );

					var gx	=	MathUtil.IntDivUp( LpvSize, BlockSize );
					var gy	=	MathUtil.IntDivUp( LpvSize, BlockSize );
					var gz	=	MathUtil.IntDivUp( LpvSize, BlockSize );

					device.Dispatch( gx, gy, gz );
				}
				

				using ( new PixEvent("Propagation") ) {

					device.ResetStates();		  
			
					device.PipelineState	=	factory[ (int)LpvFlags.PROPAGATE ];

					device.ComputeShaderResources[0]	=	fog3d1;
					device.ComputeShaderConstants[0]	=	paramsCB;

					device.SetCSRWTexture( 0, fog3d0 );

					var gx	=	MathUtil.IntDivUp( LpvSize, BlockSize );
					var gy	=	MathUtil.IntDivUp( LpvSize, BlockSize );
					var gz	=	1;

					device.Dispatch( gx, gy );
				}
			}
		}
	}
}
