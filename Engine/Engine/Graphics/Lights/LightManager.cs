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

	internal partial class LightManager : RenderComponent {


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
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
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
			int count = Math.Min(255, lightSet.EnvLights.Count);

			return GetClosestLightProbe( lightSet, point );
		}



		byte GetClosestLightProbe ( LightSet lightSet, Vector3 point )
		{
			int index = lightSet.EnvLights.IndexOfMaximum( (p) => Vector3.Distance( point, p.Position ) );

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

			var weight4	=	rand.NextVector4( Vector4.Zero, Vector4.One );
			var sum		=	Vector4.Dot( Vector4.One, weight4 );

				weight4	/=	(sum + 0.00001f);

			int count	=	lightSet.EnvLights.Count;

			indices.R	=	(byte)(rand.Next(0, count));
			indices.G	=	(byte)(rand.Next(0, count));
			indices.B	=	(byte)(rand.Next(0, count));
			indices.A	=	(byte)(rand.Next(0, count));

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
