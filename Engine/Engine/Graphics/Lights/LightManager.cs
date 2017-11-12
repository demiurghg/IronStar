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

	internal partial class LightManager : RenderComponent {


		public LightGrid LightGrid {
			get { return lightGrid; }
		}
		public LightGrid lightGrid;


		public ShadowMap ShadowMap {
			get { return shadowMap; }
		}
		public ShadowMap shadowMap;


		public Texture3D OcclusionGrid {
			get { return occlusionGrid; }
		}

		Texture3D occlusionGrid;
		


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

			occlusionGrid	=	new Texture3D( rs.Device, ColorFormat.Rgba8, Width,Height,Depth );

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
			}

			base.Dispose( disposing );
		}


		const int	Width		=	64;
		const int	Height		=	64;
		const int	Depth		=	64;
		const float GridStep	=	1.0f;
		const int	SampleNum	=	256;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="lightSet"></param>
		public void Update ( GameTime gameTime, LightSet lightSet, IEnumerable<MeshInstance> instances )
		{
			if (Game.Keyboard.IsKeyDown(Input.Keys.R)) {
				UpdateIrradianceMap(instances, rs.RenderWorld.Debug);
			}

			if (Game.Keyboard.IsKeyDown(Input.Keys.T)) {
				foreach ( var p in points ) {
					rs.RenderWorld.Debug.DrawPoint( p, 0.1f, Color.Orange );
				}
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


		List<Vector3> points = new List<Vector3>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="instances"></param>
		public void UpdateIrradianceMap ( IEnumerable<MeshInstance> instances, DebugRender dr )
		{
			Log.Message("Building ambient occlusion map");

			using ( var rtc = new Rtc() ) {

				using ( var scene = new RtcScene( rtc, SceneFlags.Incoherent|SceneFlags.Static, AlgorithmFlags.Intersect1 ) ) {

					points.Clear();

					sphereRandomPoints		= Enumerable.Range(0,SampleNum).Select( i => rand.NextVector3OnSphere() ).ToArray();
					hemisphereRandomPoints	= Enumerable.Range(0,SampleNum).Select( i => rand.NextUpHemispherePoint().Normalized() ).ToArray();

					foreach ( var p in sphereRandomPoints ) {
						dr.DrawPoint( p, 0.1f, Color.Orange );
					}

					Log.Message("...generating scene");

					foreach ( var instance in instances ) {
						AddMeshInstance( scene, instance );
					}

					scene.Commit();

					Log.Message("...tracing");

					var data = new Color[ Width*Height*Depth ];


					for ( int x=0; x<Width;  x++ ) {

						for ( int y=0; y<Height/2; y++ ) {

							for ( int z=0; z<Depth;  z++ ) {

								int index		=	ComputeAddress(x,y,z);

								var offset		=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
								var position	=	new Vector3( x, y, z );

								var localAO		=	(byte)(255*ComputeOcclusion( scene, position, sphereRandomPoints, 4 ));
								var globalAO	=	(byte)(255*ComputeOcclusion( scene, position, hemisphereRandomPoints, 512 ));

								byte byteX		=	(byte)( x*4 );
								byte byteY		=	(byte)( y*4 );
								byte byteZ		=	(byte)( z*4 );

								data[index]		=	new Color(globalAO,globalAO,globalAO,localAO);
							}
						}
					}

					occlusionGrid.SetData( data );

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



		float ComputeOcclusion ( RtcScene scene, Vector3 point, Vector3[] dirs, float maxRange )
		{
			var rand	=	new Random();

			var min		=	Vector3.One * (-GridStep/2.0f);
			var max		=	Vector3.One * ( GridStep/2.0f);
			var range	=	new Vector3(Width*GridStep, Height*GridStep, Depth*GridStep).Length();

			points.Add(point);

			float factor = 0;

			for (int i=0; i<SampleNum; i++) {
				
				var dir		=	dirs[i];
				var bias	=	rand.NextVector3( min, max );

				var x	=	point.X + bias.X;
				var y	=	point.Y + bias.Y;
				var z	=	point.Z + bias.Z;
				var dx	=	dir.X;
				var dy	=	dir.Y;
				var dz	=	dir.Z;

				if (scene.Occluded (  x,y,z, dx,dy,dz, 0,maxRange ) ) {
					var localFactor = dir.Y/SampleNum*2;
					factor = factor + (float)localFactor;
				}
			}

			return 1-MathUtil.Clamp( factor, 0, 1 );
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
