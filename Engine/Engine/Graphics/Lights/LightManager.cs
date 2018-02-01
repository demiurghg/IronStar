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

namespace Fusion.Engine.Graphics {

	[RequireShader("relight", true)]
	internal partial class LightManager : RenderComponent {


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
		public LightGrid lightGrid;


		public ShadowMap ShadowMap {
			get { return shadowMap; }
		}
		public ShadowMap shadowMap;


		public Texture3D OcclusionGrid		{ get { return occlusionGrid; }	}

		Texture3D occlusionGrid;

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

			occlusionGrid		=	new Texture3D( rs.Device, ColorFormat.Rgba8, Width,Height,Depth );

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
				SafeDispose( ref occlusionGrid );
			}

			base.Dispose( disposing );
		}


		const int	Width		=	64;
		const int	Height		=	32;
		const int	Depth		=	64;
		const float GridStep	=	0.5f;
		const int	SampleNum	=	91;


		public Matrix OcclusionGridMatrix {
			get {
				return	Matrix.Identity
					*	Matrix.Translation( Width/2.0f*GridStep, 0, Depth/2.0f*GridStep )
					*	Matrix.Translation( 0.5f*GridStep, 0.5f*GridStep, 0.5f*GridStep )
					*	Matrix.Scaling( 1.0f/Width, 1.0f / Height, 1.0f / Depth ) 
					*	Matrix.Scaling( 1.0f/GridStep )
					;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="lightSet"></param>
		public void Update ( GameTime gameTime, LightSet lightSet, IEnumerable<MeshInstance> instances )
		{
			if (Game.Keyboard.IsKeyDown(Input.Keys.R)) {
				UpdateIrradianceMap(instances, lightSet, rs.RenderWorld.Debug);
			}


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
				relightParams.LightProbePosition	=	new Vector4( lightProbe.Position, 1 );
				relightParams.ShadowViewProjection	=	shadowMap.GetLessDetailedCascade().ViewProjectionMatrix;
				relightParams.DirectLightDirection	=	new Vector4( lightSet.DirectLight.Direction, 0 );
				relightParams.DirectLightIntensity	=	lightSet.DirectLight.Intensity;
				relightParams.SkyAmbient			=	skyAmbient;
				relightParams.ShadowRegion			=	shadowMap.GetLessDetailedCascade().ShadowScaleOffset;

				cbRelightParams.SetData( relightParams );
				/*cbLightProbeData.SetData( lightProbeData );*/

				device.ComputeShaderResources[0]    =   colorData;
				device.ComputeShaderResources[1]    =   normalData;
				device.ComputeShaderResources[2]    =   rs.Sky.SkyCube;
				device.ComputeShaderResources[3]	=	shadowMap.ColorBuffer;
				device.ComputeShaderResources[4]	=	null;
				device.ComputeShaderResources[5]	=	occlusionGrid;
				device.ComputeShaderSamplers[0]		=	SamplerState.PointClamp;
				device.ComputeShaderSamplers[1]		=	SamplerState.LinearWrap;
				device.ComputeShaderSamplers[2]		=	SamplerState.ShadowSamplerPoint;
				
				device.ComputeShaderConstants[0]	=	cbRelightParams;
				/*device.ComputeShaderConstants[1]	=	cbLightProbeData;*/
					
				device.SetCSRWTexture( 0, target.GetSingleCubeSurface( cubeIndex, 0 ) );
				
				device.PipelineState = factory[(int)Flags.RELIGHT];

				int size	=	RenderSystem.LightProbeSize;
					
				int tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
				int tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
				int tgz		=	1;

				device.Dispatch( tgx, tgy, tgz );
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

				device.ComputeShaderSamplers[0]		=	SamplerState.PointClamp;
				device.ComputeShaderSamplers[1]		=	SamplerState.LinearWrap;
				device.ComputeShaderSamplers[2]		=	SamplerState.ShadowSamplerPoint;

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
				
					device.SetCSRWTexture( 0, target.GetBatchCubeSurface( batchIndex, mip ) );

					device.ComputeShaderResources[4]	=	target.GetBatchCubeShaderResource( batchIndex, mip - 1 );

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

					device.SetCSRWTexture( 0, target.GetBatchCubeSurface( batchIndex, RenderSystem.LightProbeDiffuseMip ) );

					device.ComputeShaderResources[4]	=	target.GetBatchCubeShaderResource( batchIndex, 3 );

					int size	=	RenderSystem.LightProbeSize;
					int tgx		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeX );
					int tgy		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeY );
					int tgz		=	RenderSystem.LightProbeBatchSize;

					device.Dispatch( tgx, tgy, tgz );
				}

				//
				//	prefilter ambience :
				//
				if (true) {
					device.PipelineState = factory[(int)(Flags.PREFILTER | Flags.AMBIENT)];

					device.SetCSRWTexture( 0, target.GetBatchCubeSurface( batchIndex, RenderSystem.LightProbeAmbientMip ) );

					device.ComputeShaderResources[4]	=	target.GetBatchCubeShaderResource( batchIndex, RenderSystem.LightProbeDiffuseMip );

					int size	=	RenderSystem.LightProbeSize;
					int tgx		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeX );
					int tgy		=	MathUtil.IntDivRoundUp( size, PrefilterBlockSizeY );
					int tgz		=	RenderSystem.LightProbeBatchSize;

					device.Dispatch( tgx, tgy, tgz );
				}
				//}
			}
		}





		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Occlusion grid stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		static Random rand = new Random();

		Vector3[] sphereRandomPoints;
		Vector3[] hemisphereRandomPoints;
		Vector3[] cubeRandomPoints;


		List<Vector3> points = new List<Vector3>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="instances"></param>
		public void UpdateIrradianceMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr )
		{
			Log.Message("Building ambient occlusion map");

			using ( var rtc = new Rtc() ) {

				using ( var scene = new RtcScene( rtc, SceneFlags.Incoherent|SceneFlags.Static, AlgorithmFlags.Intersect1 ) ) {

					points.Clear();

					var min		=	Vector3.One * (-GridStep/2.0f);
					var max		=	Vector3.One * ( GridStep/2.0f);

					sphereRandomPoints		= Enumerable.Range(0,SampleNum).Select( i => Hammersley.SphereUniform(i,SampleNum) ).ToArray();
					hemisphereRandomPoints	= Enumerable.Range(0,SampleNum).Select( i => Hammersley.HemisphereUniform(i,SampleNum) ).ToArray();
					cubeRandomPoints		= Enumerable.Range(0,SampleNum).Select( i => rand.NextVector3( min, max ) ).ToArray();

					foreach ( var p in hemisphereRandomPoints ) {
						dr.DrawPoint( p, 0.1f, Color.Orange );
					}

					Log.Message("...generating scene");

					foreach ( var instance in instances ) {
						AddMeshInstance( scene, instance );
					}

					scene.Commit();

					Log.Message("...tracing");

					var data	= new Color[ Width*Height*Depth ];
					var indices = new Color[ Width*Height*Depth ];
					var weights = new Color[ Width*Height*Depth ];


					for ( int x=0; x<Width;  x++ ) {

						for ( int y=0; y<Height; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var offset		=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var translation	=	new Vector3( -Width/2.0f, 0, -Depth/2.0f );
								var position	=	(new Vector3( x, y, z ) + translation) * GridStep;

								var localAO		=	ComputeLocalOcclusion( scene, position, 3 );
								var globalAO	=	ComputeSkyOcclusion( scene, position, 128 );

								byte byteX		=	(byte)( 255 * (globalAO.X * 0.5+0.5) );
								byte byteY		=	(byte)( 255 * (globalAO.Y * 0.5+0.5) );
								byte byteZ		=	(byte)( 255 * (globalAO.Z * 0.5+0.5) );
								byte byteW		=	(byte)( 255 * localAO );

								data[index]		=	new Color( byteX, byteY, byteZ, byteW );

								/*if (x==0 || y==0 || z==0 || x==Width-1 || y==Height-1 || z==Depth-1 ) {
									data[index]	=	new Color( 127,255,127,0 );
								} */
							}
						}
					}

					occlusionGrid.SetData( data );

					Log.Message("Done!");
				}
			}
		}



		int	ComputeAddress ( int x, int y, int z ) 
		{
			return x + y * Width + z * Height*Width;
		}



		float ComputeLocalOcclusion ( RtcScene scene, Vector3 point, float maxRange )
		{
			float factor = 0;

			for (int i=0; i<SampleNum; i++) {
				
				var dir		=	sphereRandomPoints[i];
				var bias	=	cubeRandomPoints[i];

				var x	=	point.X + bias.X - dir.X;
				var y	=	point.Y + bias.Y - dir.Y;
				var z	=	point.Z + bias.Z - dir.Z;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				var dist	=	scene.Intersect( x,y,z, dx,dy,dz, 0, maxRange );

				if (dist>=0) {
					var localFactor = (float)Math.Exp(-dist*2) / SampleNum;
					factor = factor + (float)localFactor;
				}
			}

			return 1-MathUtil.Clamp( factor * 2, 0, 1 );
		}



		Vector3 ComputeSkyOcclusion ( RtcScene scene, Vector3 point, float maxRange )
		{
			var bentNormal	=	Vector3.Zero;
			var factor		=	0;
			var scale		=	1.0f / SampleNum;

			for (int i=0; i<SampleNum; i++) {
				
				var dir		=	hemisphereRandomPoints[i];
				var bias	=	Vector3.Zero;// cubeRandomPoints[i];

				var x	=	point.X + bias.X + dir.X * GridStep / 2.0f;
				var y	=	point.Y + bias.Y + dir.Y * GridStep / 2.0f;
				var z	=	point.Z + bias.Z + dir.Z * GridStep / 2.0f;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				//var dist	=	scene.Intersect( x,y,z, dx,dy,dz, 0, maxRange );

				//if (dist<=0) {
				//	factor		+= 1;
				//	bentNormal	+= dir;
				//}
				
				var occluded	=	scene.Occluded( x,y,z, dx,dy,dz, 0, maxRange );

				if (!occluded) {
					factor		+= 1;
					bentNormal	+= dir;
				}
			}

			if (bentNormal.Length()>0) {
				bentNormal.Normalize();
				bentNormal = bentNormal * factor * scale;
			} else {
				bentNormal = Vector3.Zero;
			}

			return bentNormal;
		}




		void AddMeshInstance ( RtcScene scene, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.Select( v2 => new BEPUutilities.Vector4( v2.X, v2.Y, v2.Z, 0 ) )
								.ToArray();

			var id		=	scene.NewTriangleMesh( GeometryFlags.Static, indices.Length/3, vertices.Length );

			Log.Message("trimesh: id={0} tris={1} verts={2}", id, indices.Length/3, vertices.Length );


			var pVerts	=	scene.MapBuffer( id, BufferType.VertexBuffer );
			var pInds	=	scene.MapBuffer( id, BufferType.IndexBuffer );

			SharpDX.Utilities.Write( pVerts, vertices, 0, vertices.Length );
			SharpDX.Utilities.Write( pInds,  indices,  0, indices.Length );

			scene.UnmapBuffer( id, BufferType.VertexBuffer );
			scene.UnmapBuffer( id, BufferType.IndexBuffer );

			//scene.UpdateBuffer( id, BufferType.VertexBuffer );
			//scene.UpdateBuffer( id, BufferType.IndexBuffer );

		}
	}
}
