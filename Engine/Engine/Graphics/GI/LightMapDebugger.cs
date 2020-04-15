using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Engine.Graphics.Lights {

	[RequireShader("lightmapDebug",true)]
	public class LightMapDebugger : RenderComponent, IPipelineStateProvider {

		[Config]
		public float LightProbeSize { get; set; } = 5.0f;
		
		[Config]
		public float LightProbeMipLevel { get; set; } = 0.0f;
		
		[Config]
		public bool ShowLightProbes { get; set; } = false;
		
		[Config]
		public bool ShowLightVolume { get; set; } = false;
		
		[Config]
		public bool DrawLightProbeCubes { get; set; } = false;

		[ShaderDefine]
		const int MaxLightProbes = RenderSystem.MaxEnvLights;


		static FXConstantBuffer<GpuData.CAMERA>				regCamera			=	new CRegister(0, "Camera");
		static FXConstantBuffer<DEBUG_PARAMS>				regParams			=	new CRegister(1, "Params");
		static FXSamplerState								regSampler			=	new SRegister(0, "Sampler"); 
		
		static FXTexture3D<Vector4>							regLightVolumeR		=	new TRegister(0, "LightVolumeR");
		static FXTexture3D<Vector4>							regLightVolumeG		=	new TRegister(1, "LightVolumeG");
		static FXTexture3D<Vector4>							regLightVolumeB		=	new TRegister(2, "LightVolumeB");
		
		static FXTextureCubeArray<Vector4>					regLightProbes		=	new TRegister(3, "LightProbes");
		static FXStructuredBuffer<SceneRenderer.LIGHTPROBE> regLightProbeData	=	new TRegister(4, "LightProbeData");

		[StructLayout(LayoutKind.Sequential, Pack=4)]
		struct DEBUG_PARAMS 
		{
			public Matrix	VolumeTransform;

			public float	LightProbeSize;
			public float	LightProbeMipLevel;
			public float	Dummy1;
			public float	Dummy2;

			public uint		VolumeWidth;
			public uint		VolumeHeight;
			public uint		VolumeDepth;
			public float	VolumeStride;
		}

		Ubershader		shader;
		StateFactory	factory;
		Scene			sphere;
		Mesh			sphereMesh;
		Scene			sphere2;
		Mesh			sphere2Mesh;

		Scene			cube;
		Mesh			cubeMesh;

		ConstantBuffer	cbParams;

		enum Flags {
			SHOW_LIGHTVOLUME	=	0x0001,
			SHOW_LIGHTPROBES	=	0x0002,
			CUBES				=	0x0004,
			SPHERES				=	0x0008
		}


		public LightMapDebugger ( RenderSystem rs ) : base(rs)
		{
		}


		public override void Initialize()
		{
			cbParams	=	new ConstantBuffer( device, typeof(DEBUG_PARAMS) );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();

			base.Initialize();
		}


		void LoadContent ()
		{
			sphere		=	Game.Content.Load<Scene>(@"misc\sphere");
			sphereMesh	=	sphere.Meshes.FirstOrDefault();

			sphere2		=	Game.Content.Load<Scene>(@"misc\sphere2");
			sphere2Mesh	=	sphere2.Meshes.FirstOrDefault();

			cube		=	Game.Content.Load<Scene>(@"misc\cube");
			cubeMesh	=	cube.Meshes.FirstOrDefault();

			shader	=	Game.Content.Load<Ubershader>("lightmapDebug");
			factory	=	shader.CreateFactory( typeof(Flags), Primitive.TriangleList, VertexColorTextureTBNRigid.Elements );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref cbParams );
			}

			base.Dispose( disposing );
		}


		internal void Render ( Camera camera, HdrFrame hdrFrame )
		{
			device.ResetStates();
			device.SetTargets( hdrFrame.DepthBuffer.Surface, hdrFrame.HdrBuffer.Surface );

			var paramData	= new DEBUG_PARAMS();
			var lightVolume	= rs.RenderWorld.IrradianceVolume;

			paramData.LightProbeSize		=	LightProbeSize;
			paramData.LightProbeMipLevel	=	LightProbeMipLevel;

			paramData.VolumeTransform		=	lightVolume.WorldPosToTexCoord;
			paramData.VolumeWidth			=	(uint)lightVolume.Width;
			paramData.VolumeHeight			=	(uint)lightVolume.Height;
			paramData.VolumeDepth			=	(uint)lightVolume.Depth;
			paramData.VolumeStride			=	lightVolume.Stride;

			int volumeElementCount			=	lightVolume.Width * lightVolume.Height * lightVolume.Depth;

			cbParams.SetData( paramData );


			device.GfxConstants[ regCamera ]			=   camera.CameraData;
			device.GfxConstants[ regParams ]			=	cbParams;

			device.GfxSamplers[ regSampler ]			=	SamplerState.LinearWrap;

			device.GfxResources[ regLightVolumeR	]	=	lightVolume.LightVolumeR;
			device.GfxResources[ regLightVolumeG	]	=	lightVolume.LightVolumeG;
			device.GfxResources[ regLightVolumeB	]	=	lightVolume.LightVolumeB;

			device.GfxResources[ regLightProbes		]	=	rs.LightMapResources.IrradianceCubeMaps;
			device.GfxResources[ regLightProbeData	]	=	rs.LightManager.LightGrid.ProbeDataGpu;

			if (ShowLightProbes)
			{
				using ( new PixEvent( "Debug Light Probes" ) )
				{
					Flags flags = 	Flags.SHOW_LIGHTPROBES | (DrawLightProbeCubes ? Flags.CUBES : Flags.SPHERES );
	
					device.PipelineState	=	factory[ (int)flags ];
			
					if (DrawLightProbeCubes) 
					{
						DrawInstancedCubes( RenderSystem.MaxEnvLights );
					}
					else
					{
						DrawInstancedSpheres2( RenderSystem.MaxEnvLights );
					}
				}							
			}

			if (ShowLightVolume)
			{
				using ( new PixEvent( "Debug Light Volume" ) )
				{
					Flags flags = 	Flags.SHOW_LIGHTVOLUME;
	
					device.PipelineState	=	factory[ (int)flags ];
			
					DrawInstancedSpheres( volumeElementCount );
				}							
			}
		}


		void DrawInstancedCubes( int instanceCount )
		{
			device.SetupVertexInput( cubeMesh.VertexBuffer, cubeMesh.IndexBuffer );
			device.DrawInstancedIndexed( cubeMesh.IndexCount, instanceCount, 0, 0, 0 );
		}


		void DrawInstancedSpheres( int instanceCount )
		{
			device.SetupVertexInput( sphereMesh.VertexBuffer, sphereMesh.IndexBuffer );
			device.DrawInstancedIndexed( sphereMesh.IndexCount, instanceCount, 0, 0, 0 );
		}


		void DrawInstancedSpheres2( int instanceCount )
		{
			device.SetupVertexInput( sphere2Mesh.VertexBuffer, sphere2Mesh.IndexBuffer );
			device.DrawInstancedIndexed( sphere2Mesh.IndexCount, instanceCount, 0, 0, 0 );
		}


	}
}
