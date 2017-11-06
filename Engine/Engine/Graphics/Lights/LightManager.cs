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
		const int	SampleNum	=	64;


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


		List<Vector3> points = new List<Vector3>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="instances"></param>
		public void UpdateIrradianceMap ( IEnumerable<MeshInstance> instances, DebugRender dr )
		{
			Log.Message("Building ambient occlusion map");

			points.Clear();

			sphereRandomPoints = Enumerable.Range(0,64).Select( i => rand.NextUpHemispherePoint() ).ToArray();

			foreach ( var p in sphereRandomPoints ) {
				dr.DrawPoint( p, 0.1f, Color.Orange );
			}

			Log.Message("...generating scene");

			var space = new Space();

			foreach ( var instance in instances ) {
				AddMeshInstance( space, instance );
			}

			Log.Message("...tracing");

			var data = new Color[ Width*Height*Depth ];


			for ( int x=0; x<Width;  x++ ) {
				for ( int y=0; y<Height; y++ ) {
					for ( int z=0; z<Depth;  z++ ) {
			//Parallel.For( 216, Width-216, (x) => {

			//for ( int x=220; x<Width-220;  x++ ) {
			//	Log.Message("{0}/{1}", x, Width);
			//	for ( int y=32; y<Height-32; y++ ) {
			//		for ( int z=220; z<Depth-220;  z++ ) {
				
						int index		=	ComputeAddress(x,y,z);

						var offset		=	new Vector3( GridStep/2.0f, GridStep/2.0f, GridStep/2.0f );
						var position	=	new Vector3( x, y, z );
						//var position	=	new Vector3( (x-256+0.5f)/2.0f, (y-64+0.5f)/2.0f, (z-256+0.5f)/2.0f );

						var localAO		=	(byte)(255*CalcLocalOcclusion( space, position ));

						byte byteX		=	(byte)( x*4 );
						byte byteY		=	(byte)( y*4 );
						byte byteZ		=	(byte)( z*4 );

						data[index]		=	new Color(byteX,byteY,byteZ,localAO);
						//data[index]		=	new Color(byteX,byteY,byteZ, (byte)(255*((x+y+z)%2)) );
					}
				}
			}
			//);

			occlusionGrid.SetData( data );

			Log.Message("Done!");
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



		float CalcLocalOcclusion ( Space space, Vector3 point )
		{
			Random rand = new Random();

			points.Add(point);

			float factor = 0;

			for (int i=0; i<SampleNum; i++) {
				var dir		= (sphereRandomPoints[i]);
					dir.Normalize();

				var from	= MathConverter.Convert( point - dir*GridStep );
				var dir2	= MathConverter.Convert( dir );

				var ray		= new BEPUutilities.Ray( from, dir2 );

				RayCastResult result;

				if (space.RayCast( ray, 64, out result )) {
					var localFactor = dir.Y/SampleNum;
					factor = factor + (float)localFactor;
					//points.Add( MathConverter.Convert(result.HitData.Location) );
				}
			}

			return MathUtil.Clamp( factor, 0, 1 );
		}




		void AddMeshInstance ( Space space, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.Select( v2 => new BEPUutilities.Vector3( v2.X, v2.Y, v2.Z ) )
								.ToArray();

			var staticMesh = new StaticMesh( vertices, indices );

			staticMesh.Sidedness = BEPUutilities.TriangleSidedness.Clockwise;

			var scaling		= new BEPUutilities.Vector3(1,1,1);
			var translation = new BEPUutilities.Vector3(0,0,0);
			var rotation	= BEPUutilities.Quaternion.Identity;
			staticMesh.WorldTransform = new BEPUutilities.AffineTransform(scaling, rotation, translation);

			space.Add( staticMesh );
		}
	}
}
