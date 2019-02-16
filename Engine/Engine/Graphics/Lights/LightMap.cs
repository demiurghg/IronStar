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

namespace Fusion.Engine.Graphics.Lights {

	[RequireShader("lightmap", true)]
	internal class LightMap : RenderComponent {


		[ShaderStructure()]
		[StructLayout(LayoutKind.Sequential, Pack=4, Size=192)]
		struct BAKE_PARAMS {
			public	Matrix	ShadowViewProjection;
			public	Matrix	OcclusionGridTransform;
			public	Vector4 LightDirection;
		}


		ConstantBuffer		constBuffer;
		StateFactory		factory;
		Ubershader			shader;

		enum Flags {
			BAKE,
			COPY,
		}


		public ShaderResource LightMap2D {
			get { return lightMap2D; }
		}

		public ShaderResource LightMap3D {
			get { return lightMap3D; }
		}

		public Matrix LightMap3DMatrix {
			get { return Matrix.Identity; }
		}


		Texture2D	gbufferPosition;
		Texture2D	gbufferNormal;
		Texture2D	gbufferColor;
		Texture2D	lightMap2D;
		Texture3D	lightMap3D;


		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMap(RenderSystem rs) : base(rs)
		{
			constBuffer		=	new ConstantBuffer( rs.Device, typeof(BAKE_PARAMS) );

			lightMap2D		=	new Texture2D( rs.Device, 256,256, ColorFormat.Rgba32F, false, false );

			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		/// <summary>
		/// Loads content if necessary
		/// </summary>
		void LoadContent ()
		{
			SafeDispose( ref factory );

			shader	=	Game.Content.Load<Ubershader>("lightmap");
			factory	=	shader.CreateFactory( typeof(Flags) );
		}


		/// <summary>
		/// Disposes stuff 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref factory );
				SafeDispose( ref constBuffer );		 

				SafeDispose( ref gbufferPosition );
				SafeDispose( ref gbufferNormal	 );
				SafeDispose( ref gbufferColor	 );
				SafeDispose( ref lightMap2D		 );
				SafeDispose( ref lightMap3D		 );
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Updates stuff
		/// </summary>
		public void Update ( GameTime gameTime )
		{
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Lightmap stuff
		 * 
		-----------------------------------------------------------------------------------------*/


		class LightMapSet {

			public readonly int Width;
			public readonly int Height;

			public LightMapSet( int w, int h ) 
			{
				Width		=	w;
				Height		=	h;

				Albedo		=	new GenericImage<Color>		( w, h, Color.Zero	 );
				Position	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
				Normal		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
				Radiance	=	new GenericImage<Color4>	( w, h, Color4.Zero );
			}
			
			public readonly GenericImage<Color>		Albedo;
			public readonly GenericImage<Vector3>	Position;
			public readonly GenericImage<Vector3>	Normal;
			public readonly GenericImage<Color4>	Radiance;
		}


		Random rand		=	new Random();

		/// <summary>
		/// Update lightmap
		/// </summary>
		public void BakeLightMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			var lightmap	=	new LightMapSet( 256, 256 );
			var hammersley	=	Hammersley.GenerateSphereUniform(256);

			lightmap.Radiance.PerpixelProcessing( (c) => rand.NextColor().ToColor4() );

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var instance in instances ) {
				RasterizeInstance( lightmap, instance );
			}

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				var sceneFlags	=	SceneFlags.Coherent|SceneFlags.Static;
				var algFlags	=	AlgorithmFlags.Intersect1;

				using ( var scene = new RtcScene( rtc, sceneFlags, algFlags ) ) {

					//--------------------------------------

					Log.Message("Generating RTC scene...");

					foreach ( var instance in instances ) {
						AddMeshInstance( scene, instance );
					}

					scene.Commit();

					//--------------------------------------

					Log.Message("Lightmap ray tracing...");

					for ( int i=0; i<lightmap.Width; i++ ) {
						for ( int j=0; j<lightmap.Height; j++ ) {

							var p = lightmap.Position[i,j];
							var n = lightmap.Normal[i,j];
							var c = lightmap.Albedo[i,j];

							var r = ComputeRadiance( scene, hammersley, lightSet, p, n, c );

							lightmap.Radiance[i,j]	=	r;

						}
					}

				}
			}

			//--------------------------------------

			Log.Message("Uploading lightmap to GPU...");

			lightMap2D.SetData( lightmap.Radiance.RawImageData );

			Log.Message("Completed.");
		}



		/// <summary>
		/// 
		/// </summary>
		Color4 ComputeRadiance ( RtcScene scene, Vector3[] randomPoints, LightSet lightSet, Vector3 position, Vector3 normal, Color albedo )
		{
			var sampleCount		=	randomPoints.Length;
			var invSamleCount	=	1.0f / sampleCount;
			var result			=	Color4.Zero;

			var dirLightDir		=	-(lightSet.DirectLight.Direction).Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity;

			var skyAmbient		=	rs.RenderWorld.SkySettings.AmbientLevel;

			//---------------------------------
			//	direct light :

			if (true) {

				var nDotL	=	 Math.Max( 0, Vector3.Dot( dirLightDir, normal ) );

				var ray		=	new RtcRay();

				EmbreeExtensions.UpdateRay( ref ray, position, dirLightDir, 0, 9999 );

				var shadow	=	 scene.Occluded( ray ) ? 0 : 1;

				result		+=	nDotL * dirLightColor * shadow;
			}

			//---------------------------------
			//	sky light :

			for ( int i = 0; i<sampleCount; i++ ) {

				var dir		= randomPoints[i];

				var nDotL	= Vector3.Dot( dir, normal );

				if (nDotL<=0) {
					continue;
				}

				if (dir.Y<0) {
					continue;
				}

				var ray		=	new RtcRay();

				EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 9999 );

				var shadow	=	 scene.Occluded( ray ) ? 0 : 1;

				result		+=	nDotL * skyAmbient * shadow * invSamleCount; 

			} 

			return result;
		}



		/// <summary>
		/// Rasterizes LM texcoords to lightmap
		/// </summary>
		/// <param name="lightmap"></param>
		/// <param name="instance"></param>
		void RasterizeInstance ( LightMapSet lightmap, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices		=	mesh.GetIndices();
			var positions	=	mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.ToArray();

			var normals		=	mesh.Vertices
								.Select( v1 => Vector3.TransformNormal( v1.Normal, instance.World ) )
								.ToArray();

			var color		=	mesh.Vertices
								.Select( v3 => rand.NextColor() )
								.ToArray();

			var points		=	mesh.Vertices
								.Select( v2 => v2.TexCoord0 * 256 )
								.ToArray();


			for (int i=0; i<indices.Length/3; i++) {

				var i0 = indices[i*3+0];
				var i1 = indices[i*3+1];
				var i2 = indices[i*3+2];

				var p0 = positions[i0];
				var p1 = positions[i1];
				var p2 = positions[i2];

				var d0 = points[i0];
				var d1 = points[i1];
				var d2 = points[i2];

				var n0 = normals[i0];
				var n1 = normals[i1];
				var n2 = normals[i2];

				var c0 = color[i0];
				var c1 = color[i1];
				var c2 = color[i2];

				var n  = Vector3.Cross( p1 - p0, p2 - p0 ).Normalized();

				var bias	=	n * 1 / 16.0f;

				Rasterizer.RasterizeTriangle( d0, d1, d2, 
					(xy,s,t) => {
						lightmap.Albedo	 [xy] = InterpolateColor	( c0, c1, c2, s, t );
						lightmap.Position[xy] = InterpolatePosition	( p0, p1, p2, s, t ) + bias;
						lightmap.Normal  [xy] = InterpolateNormal	( n0, n1, n2, s, t );
					} 
				);
			}
		}


		Color InterpolateColor ( Color c0, Color c1, Color c2, float s, float t )
		{
			float q = 1 - s - t;
			return (q * c0) + (s * c1) + (t * c2);
		}


		Vector3 InterpolatePosition ( Vector3 p0, Vector3 p1, Vector3 p2, float s, float t )
		{
			float q = 1 - s - t;
			return (q * p0) + (s * p1) + (t * p2);
		}


		Vector3 InterpolateNormal ( Vector3 n0, Vector3 n1, Vector3 n2, float s, float t )
		{
			float q = 1 - s - t;
			return Vector3.Normalize( (q * n0) + (s * n1) + (t * n2) );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Embree stuff
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Adds mesh instance to the RTC scene
		/// </summary>
		void AddMeshInstance ( RtcScene scene, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices     =   mesh.GetIndices();
			var vertices    =   mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.Select( v2 => new Vector4( v2.X, v2.Y, v2.Z, 0 ) )
								.ToArray();

			var id		=	scene.NewTriangleMesh( GeometryFlags.Static, indices.Length/3, vertices.Length );

			var pVerts	=	scene.MapBuffer( id, BufferType.VertexBuffer );
			var pInds	=	scene.MapBuffer( id, BufferType.IndexBuffer );

			SharpDX.Utilities.Write( pVerts, vertices, 0, vertices.Length );
			SharpDX.Utilities.Write( pInds,  indices,  0, indices.Length );

			scene.UnmapBuffer( id, BufferType.VertexBuffer );
			scene.UnmapBuffer( id, BufferType.IndexBuffer );
		}
	}
}
