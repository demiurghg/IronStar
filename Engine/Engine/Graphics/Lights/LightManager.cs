﻿using System;
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
		const int LightProbeSize = RenderSystem.EnvMapSize;


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=192)]
		struct RELIGHT_PARAMS {
			public	Matrix	ShadowViewProjection;
			public	Vector4	LightProbePosition;
			public	Color4	DirectLightIntensity;
			public	Vector4	DirectLightDirection;
			public	Vector4	ShadowRegion;
			public	float	CubeIndex;
			public	float	Roughness;
			public	float	TargetSize;
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
		public Texture3D LightProbeIndices	{ get { return lightProbeIndices; }	}
		public Texture3D LightProbeWeights	{ get { return lightProbeWeights; }	}

		Texture3D occlusionGrid;
		Texture3D lightProbeIndices;
		Texture3D lightProbeWeights;

		Ubershader		shader;
		StateFactory	factory;
		ConstantBuffer	constBuffer;


		enum Flags {
			RELIGHT		=	0x0001,
			PREFILTER	=	0x0002,
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
			lightProbeIndices	=	new Texture3D( rs.Device, ColorFormat.Rgba8, Width,Height,Depth );
			lightProbeWeights	=	new Texture3D( rs.Device, ColorFormat.Rgba8, Width,Height,Depth );

			constBuffer			=	new ConstantBuffer( rs.Device, typeof(RELIGHT_PARAMS) );

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
				SafeDispose( ref constBuffer );
				SafeDispose( ref factory );
				SafeDispose( ref lightGrid );
				SafeDispose( ref shadowMap );
				SafeDispose( ref occlusionGrid );
				SafeDispose( ref lightProbeIndices );
				SafeDispose( ref lightProbeWeights );
			}

			base.Dispose( disposing );
		}


		const int	Width		=	128;
		const int	Height		=	64;
		const int	Depth		=	128;
		const float GridStep	=	1.0f;
		const int	SampleNum	=	16;


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
		public void RelightLightProbe ( TextureCubeArray colorData, TextureCubeArray normalData, LightProbe lightProbe, LightSet lightSet, RenderTargetCube target )
		{
			using ( new PixEvent( "RelightLightProbe" ) ) {

				device.ResetStates();

				var constData = new RELIGHT_PARAMS();

				constData.CubeIndex				=	lightProbe.ImageIndex;
				constData.LightProbePosition	=	new Vector4( lightProbe.Position, 1 );
				constData.ShadowViewProjection	=	shadowMap.GetLessDetailedCascade().ViewProjectionMatrix;
				constData.DirectLightDirection	=	new Vector4( lightSet.DirectLight.Direction, 0 );
				constData.DirectLightIntensity	=	lightSet.DirectLight.Intensity;
				constData.ShadowRegion			=	shadowMap.GetLessDetailedCascade().ShadowScaleOffset;

				constBuffer.SetData( constData );

				device.ComputeShaderResources[0]    =   colorData;
				device.ComputeShaderResources[1]    =   normalData;
				device.ComputeShaderResources[2]    =   rs.Sky.SkyCube;
				device.ComputeShaderResources[3]	=	shadowMap.ColorBuffer;
				device.ComputeShaderSamplers[0]		=	SamplerState.PointClamp;
				device.ComputeShaderSamplers[0]		=	SamplerState.LinearWrap;
				device.ComputeShaderSamplers[2]		=	SamplerState.ShadowSamplerPoint;
				device.ComputeShaderConstants[0]	=	constBuffer;
					
				for (int i=0; i<6; i++) {
					device.SetCSRWTexture( i, target.GetSurface( 0, (CubeFace)i ) );
				}

				device.PipelineState = factory[(int)Flags.RELIGHT];

				int size	=	RenderSystem.EnvMapSize;
					
				int tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
				int tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
				int tgz		=	1;

				device.Dispatch( tgx, tgy, tgz );

				//
				//	prefilter :
				//
				device.PipelineState = factory[(int)Flags.PREFILTER];
				
				for (int mip=1; mip<RenderSystem.EnvMapSpecularMipCount; mip++) {
					
					constData.Roughness		=	(float)mip / (RenderSystem.EnvMapSpecularMipCount-1);
					constData.TargetSize	=	RenderSystem.EnvMapSize >> mip;
					constBuffer.SetData( constData );

					for (int i=0; i<6; i++) {
						device.SetCSRWTexture( i, target.GetSurface( mip, (CubeFace)i ) );
					}

					device.ComputeShaderResources[4]	=	target.GetCubeShaderResource( mip - 1 );

					size	=	RenderSystem.EnvMapSize >> mip;
					tgx		=	MathUtil.IntDivRoundUp( size, BlockSizeX );
					tgy		=	MathUtil.IntDivRoundUp( size, BlockSizeY );
					tgz		=	1;

					device.Dispatch( tgx, tgy, tgz );
				}
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
					hemisphereRandomPoints	= Enumerable.Range(0,SampleNum).Select( i => Hammersley.HemisphereCosine(i,SampleNum) ).ToArray();
					cubeRandomPoints		= Enumerable.Range(0,SampleNum).Select( i => rand.NextVector3( min, max ) ).ToArray();

					foreach ( var p in sphereRandomPoints ) {
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

					Color lpIndex, lpWeight;


					for ( int x=0; x<Width;  x++ ) {

						for ( int y=0; y<Height/2; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var offset		=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var position	=	new Vector3( x, y, z );

								var localAO		=	ComputeLocalOcclusion( scene, position, 5 );
								var globalAO	=	ComputeSkyOcclusion( scene, position, 512 );

								GetLightProbeIndicesAndWeights( lightSet, position, out lpIndex, out lpWeight );
								//var probeIndex	=	GetLightProbeIndex( scene, lightSet, position );

								byte byteX		=	(byte)( 255 * (globalAO.X * 0.5+0.5) );
								byte byteY		=	(byte)( 255 * (globalAO.Y * 0.5+0.5) );
								byte byteZ		=	(byte)( 255 * (globalAO.Z * 0.5+0.5) );
								byte byteW		=	(byte)( 255 * localAO );

								data[index]		=	new Color( byteX, byteY, byteZ, byteW );

								indices[index]	=	lpIndex;
								weights[index]	=	lpWeight;
							}
						}
					}

					occlusionGrid.SetData( data );
					lightProbeIndices.SetData( indices );
					lightProbeWeights.SetData( weights );

					Log.Message("Done!");
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="z"></param>
		/// <returns></returns>
		int	ComputeAddress ( int x, int y, int z ) 
		{
			return x + y * Width + z * Height*Width;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="point"></param>
		/// <returns></returns>
		byte GetLightProbeIndex ( RtcScene scene, LightSet lightSet, Vector3 point )
		{
			int count = Math.Min(255, lightSet.LightProbes.Count);

			return GetClosestLightProbe( lightSet, point );
		}



		byte GetClosestLightProbe ( LightSet lightSet, Vector3 point )
		{
			int index = lightSet.LightProbes.IndexOfMaximum( (p) => Vector3.Distance( point, p.Position ) );

			if (index<0) {
				return 0;
			} else {
				return (byte)index;
			}
		}




		void GetLightProbeIndicesAndWeights ( LightSet lightSet, Vector3 point, out Color indices, out Color weights )
		{
			indices		=	Color.Zero;
			weights		=	Color.Zero;

			var envLights	=	lightSet.LightProbes.ToList();

			var candidates	=	envLights.OrderByDescending( lp => Vector3.Distance(lp.Position, point) ).Take(4).ToArray();

			indices.R	=	candidates.Length > 0 ? (byte)(envLights.IndexOf( candidates[0] )) : (byte)0;
			indices.G	=	candidates.Length > 1 ? (byte)(envLights.IndexOf( candidates[1] )) : (byte)0;
			indices.B	=	candidates.Length > 2 ? (byte)(envLights.IndexOf( candidates[2] )) : (byte)0;
			indices.A	=	candidates.Length > 3 ? (byte)(envLights.IndexOf( candidates[3] )) : (byte)0;

			var weight4	=	Vector4.Zero;

			if (candidates.Length > 0) weight4.X = 1 / (Vector3.DistanceSquared(candidates[0].Position, point) + 1);
			if (candidates.Length > 1) weight4.Y = 1 / (Vector3.DistanceSquared(candidates[1].Position, point) + 1);
			if (candidates.Length > 2) weight4.Z = 1 / (Vector3.DistanceSquared(candidates[2].Position, point) + 1);
			if (candidates.Length > 3) weight4.W = 1 / (Vector3.DistanceSquared(candidates[3].Position, point) + 1);

			var sum		=	Vector4.Dot( Vector4.One, weight4 );

				weight4	/=	(sum + 0.00001f);

			int count	=	lightSet.LightProbes.Count;

			weights.R	=	(byte)(weight4.X * 255);
			weights.G	=	(byte)(weight4.Y * 255);
			weights.B	=	(byte)(weight4.Z * 255);
			weights.A	=	(byte)(weight4.W * 255);
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
					var localFactor = (float)Math.Exp(-dist+0.5f) / SampleNum;
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

				var x	=	point.X + bias.X + dir.X / 2.0f;
				var y	=	point.Y + bias.Y + dir.Y / 2.0f;
				var z	=	point.Z + bias.Z + dir.Z / 2.0f;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				var dist	=	scene.Intersect( x,y,z, dx,dy,dz, 0, maxRange );

				if (dist<=0) {
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
