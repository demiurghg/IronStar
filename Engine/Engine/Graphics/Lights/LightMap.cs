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

			lightMap2D		=	new Texture2D( rs.Device, 256,256, ColorFormat.Rgba8, false, false );

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

		/// <summary>
		/// Update lightmap
		/// </summary>
		public void BakeLightMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			var rand		=	new Random();
			var colorMap	=	new GenericImage<Color>(256,256);

			colorMap.PerpixelProcessing( (c) => rand.NextColor() );
			colorMap.Fill( Color.Red );




			foreach ( var instance in instances ) {

				RasterizeInstance( colorMap, instance );

			}

			lightMap2D.SetData( colorMap.RawImageData );

			var image = new Image( colorMap );
			Image.SaveTga( image, @"E:\Github\testlm.tga");
		}



		void RasterizeInstance ( GenericImage<Color> lightmap, MeshInstance instance )
		{
			var mesh		=	instance.Mesh;

			if (mesh==null) {	
				return;
			}

			var indices		=	mesh.GetIndices();
			var positions	=	mesh.Vertices
								.Select( v1 => Vector3.TransformCoordinate( v1.Position, instance.World ) )
								.ToArray();

			var points		=	mesh.Vertices
								.Select( v2 => v2.TexCoord0 * 256 )
								.ToArray();

			var bias = new Vector3(4,4,4);

			for (int i=0; i<indices.Length/3; i++) {

				var p0 = positions[indices[i*3+0]];
				var p1 = positions[indices[i*3+1]];
				var p2 = positions[indices[i*3+2]];

				var d0 = points[indices[i*3+0]];
				var d1 = points[indices[i*3+1]];
				var d2 = points[indices[i*3+2]];

				var n  = Vector3.Cross( p1 - p0, p2 - p0 ).Normalized();

				Rasterizer.RasterizeTriangle( d0, d1, d2, (xy,s,t) => lightmap.SetPixel(xy.X, xy.Y, Color.White) );
				/*Voxelizer.RasterizeTriangle( p0, p1, p2, 2, (p) => {
					var vpl = new VPL(p-bias, n);
					vpls.Add( vpl );
				} );//*/
			}
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
