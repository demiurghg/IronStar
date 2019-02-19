﻿using System;
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

		LightMapSet	lightMapSet;

		const int LMSize = 256;


		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMap(RenderSystem rs) : base(rs)
		{
			constBuffer		=	new ConstantBuffer( rs.Device, typeof(BAKE_PARAMS) );

			lightMap2D		=	new Texture2D( rs.Device, LMSize,LMSize, ColorFormat.Rgba32F, false, false );

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
			if (lightMapSet!=null) {

				for ( int i=114; i<136; i++ ) {
					for ( int j=1; j<12; j++ ) {
				//for ( int i=0; i<lightMapSet.Width; i++ ) {
				//	for ( int j=0; j<lightMapSet.Height; j++ ) {
					
						var p = lightMapSet.Position[i,j];
						var po=	lightMapSet.PositionOld[i,j];
						var n = lightMapSet.Normal[i,j];

						if (p!=po || true) {
							rs.RenderWorld.Debug.DrawPoint( p, 0.5f			, Color.Red );
							rs.RenderWorld.Debug.DrawLine ( p, p + n * 1.5f	, Color.Blue );
							rs.RenderWorld.Debug.DrawLine ( p, po, Color.Gray );
						}

					}
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Lightmap stuff
		 * 
		-----------------------------------------------------------------------------------------*/


		public class LightMapSet {

			public readonly int Width;
			public readonly int Height;

			public LightMapSet( int w, int h ) 
			{
				Width		=	w;
				Height		=	h;

				Albedo		=	new GenericImage<Color>		( w, h, Color.Zero	 );
				Position	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
				PositionOld	=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
				Normal		=	new GenericImage<Vector3>	( w, h, Vector3.Zero );
				Radiance	=	new GenericImage<Color4>	( w, h, Color4.Zero );
				Temp	=	new GenericImage<Color4>	( w, h, Color4.Zero );
				Coverage	=	new GenericImage<Bool>		( w, h, false );
			}
			
			public readonly GenericImage<Color>		Albedo;
			public readonly GenericImage<Vector3>	Position;
			public readonly GenericImage<Vector3>	PositionOld;
			public readonly GenericImage<Vector3>	Normal;
			public readonly GenericImage<Color4>	Radiance;
			public readonly GenericImage<Color4>	Temp;
			public readonly GenericImage<Bool>		Coverage;
		}


		Random rand		=	new Random();

		/// <summary>
		/// Update lightmap
		/// </summary>
		public LightMapSet BakeLightMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, DebugRender dr, int numSamples )
		{
			var lightmap	=	new LightMapSet( LMSize, LMSize );
			var hammersley	=	Hammersley.GenerateSphereUniform(2048);

			lightmap.Radiance.PerpixelProcessing( (c) => rand.NextColor().ToColor4() );

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var instance in instances ) {
				RasterizeInstance( lightmap, instance, lightmap.Width, lightmap.Height );
			}

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				var sceneFlags	=	SceneFlags.Static;
				var algFlags	=	AlgorithmFlags.Intersect1;

				using ( var scene = new RtcScene( rtc, sceneFlags, algFlags ) ) {

					//--------------------------------------

					Log.Message("Generating RTC scene...");

					foreach ( var instance in instances ) {
						AddMeshInstance( scene, instance );
					}

					scene.Commit();

					//--------------------------------------

					Log.Message("Fix geometry overlaps...");

					for ( int i=0; i<lightmap.Width; i++ ) {
						for ( int j=0; j<lightmap.Height; j++ ) {

							var p = lightmap.Position[i,j];
							var n = lightmap.Normal[i,j];
							lightmap.PositionOld[i,j] = p;

							p = FixGeometryOverlap( scene, p, n );

							lightmap.Position[i,j] = p;
						}
					}

					//--------------------------------------

					Log.Message("Lightmap ray tracing...");

					var sw = new Stopwatch();
					sw.Start();

					for ( int i=0; i<lightmap.Width; i++ ) {

						Log.Message("... tracing : {0}/{1}", i, lightmap.Width );

						for ( int j=0; j<lightmap.Height; j++ ) {

							var p = lightmap.Position[i,j];
							var n = lightmap.Normal[i,j];
							var c = lightmap.Albedo[i,j];

							if (c.A>0) {
								var r = ComputeRadiance( scene, hammersley, lightSet, p, n, c );
								lightmap.Radiance[i,j]	=	r;
							} else {
								lightmap.Radiance[i,j]	=	Color4.Zero;
							}
						}
					}

					sw.Stop();
					Log.Message("{0} ms", sw.ElapsedMilliseconds);

				}
			}	 //*/

			//--------------------------------------

			Log.Message("Dilate radiance...");

			DilateRadiance( lightmap );
			//BilateralBlur( lightmap );
			
			//--------------------------------------

			Log.Message("Clusterizing...");

			var points = Hammersley.GenerateUniform2D(1024)
						.Select( v => new Int2( (int)(v.X*255), (int)(v.Y*255) ) )
						.ToArray();

			foreach ( var p in points ) {
				if (lightmap.Albedo[p]!=Color.Zero) {
					lightmap.Albedo[p] = Color.Black;
				}
			}

			//--------------------------------------

			Log.Message("Uploading lightmap to GPU...");

			lightMap2D.SetData( lightmap.Radiance.RawImageData );
			//lightMap2D.SetData( lightmap.Position.RawImageData.Select( p => new Vector4(p,1) ).ToArray() );
			//lightMap2D.SetData( lightmap.Albedo.RawImageData.Select( c => c.ToColor4() ).ToArray() );

			var image = new Image( lightmap.Albedo );
			Image.SaveTga( image, @"E:\GITHUB\testlm.tga" );

			Log.Message("Completed.");

			this.lightMapSet	=	lightmap;

			return lightmap;
		}



		void DilateRadiance ( LightMapSet lightmap )
		{
			for ( int i=0; i<lightmap.Width; i++ ) {
				for ( int j=0; j<lightmap.Height; j++ ) {

					var c = lightmap.Radiance[i,j];

					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i+1, j+0];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i-1, j+0];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i+0, j+1];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i+0, j-1];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i+1, j+1];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i-1, j-1];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i+1, j-1];
					c	=	c.Alpha > 0 ? c : lightmap.Radiance[i-1, j+1];

					lightmap.Temp[i,j] = c;
				}
			}

			lightmap.Temp.CopyTo( lightmap.Radiance );
		}



		void BilateralBlur ( LightMapSet lightmap )
		{
			for ( int i=1; i<lightmap.Width-1; i++ ) {
				for ( int j=1; j<lightmap.Height-1; j++ ) {

					var c = lightmap.Radiance[i+0, j+0];

					if (c.Alpha>0) {
						c	+=	lightmap.Radiance[i+1, j+1];
						c	+=	lightmap.Radiance[i+1, j+0];
						c	+=	lightmap.Radiance[i+1, j-1];
						c	+=	lightmap.Radiance[i+0, j+1];
						c	+=	lightmap.Radiance[i+0, j-1];
						c	+=	lightmap.Radiance[i-1, j+1];
						c	+=	lightmap.Radiance[i-1, j+0];
						c	+=	lightmap.Radiance[i-1, j-1];

						lightmap.Temp[i,j] = c / c.Alpha;
					}
				}
			}//*/

			lightmap.Temp.CopyTo( lightmap.Radiance );
		}



		Vector3 FixGeometryOverlap ( RtcScene scene, Vector3 position, Vector3 normal)
		{
			var basis	=	MathUtil.ComputeAimedBasis( normal );
			var dirs	=	new[] { basis.Right, basis.Left, basis.Up, basis.Down };
			var ray		=	new RtcRay();
			var minT	=	float.MaxValue;
			var result	=	position;

			foreach ( var dir in dirs ) {
				
				EmbreeExtensions.UpdateRay( ref ray, position - dir*0.125f, dir, 0, 3 );

				if ( scene.Intersect( ref ray ) ) {

					if ( ray.TFar < minT ) {
					
						var n	= -ray.GetHitNormal().Normalized();	

						if ( Vector3.Dot( n, dir ) > 0 ) {
							minT	= ray.TFar;
							result	= ray.GetHitPoint() + n / 16f;
						}
					}
				}
			}

			return result;
		}


		
		Color4 ComputeRadiance ( RtcScene scene, Vector3[] randomPoints, LightSet lightSet, Vector3 position, Vector3 normal, Color albedo )
		{
			var sampleCount		=	randomPoints.Length;
			var invSampleCount	=	1.0f / sampleCount;
			var result			=	Color4.Zero;

			var dirLightDir		=	-(lightSet.DirectLight.Direction).Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity;

			var skyAmbient		=	rs.RenderWorld.SkySettings.AmbientLevel;

			//---------------------------------

			for ( int i = 0; i<sampleCount; i++ ) {

				var dir		= randomPoints[i];

				var nDotL	= Vector3.Dot( dir, normal );

				if (nDotL<=0) {
					continue;
				}

				var ray		=	new RtcRay();

				EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 128 );

				var intersect	=	 scene.Intersect( ref ray );
					
				//-------------------------------------------
				//	ray hits nothing, so this is sky light :
				if (!intersect && dir.Y>0) {
					result		+=	nDotL * skyAmbient * invSampleCount; 
				}

				//-------------------------------------------
				//	trying to find direct light :
				if (intersect) {
					
					var origin		=	EmbreeExtensions.Convert( ray.Origin );
					var direction	=	EmbreeExtensions.Convert( ray.Direction );
					var hitPoint	=	origin + direction * (ray.TFar);
					var hitNormal	=	(-1) * EmbreeExtensions.Convert( ray.HitNormal ).Normalized();

					var dirDotN		=	Vector3.Dot( hitNormal, direction );

					if (dirDotN<0) // we hit front side of the face
					{
						var directLight	=	ComputeDirectLight( scene, dirLightDir, dirLightColor, hitPoint, hitNormal );

						result			+=	directLight * invSampleCount * 0.5f * (-dirDotN);
					}
				}
			} 

			result.Alpha = 1;

			return result;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		Color4 ComputeDirectLight ( RtcScene scene, Vector3 lightDir, Color4 lightColor, Vector3 position, Vector3 normal )
		{
			var nDotL	=	 Math.Max( 0, Vector3.Dot( lightDir, normal ) );

			var ray		=	new RtcRay();

			var bias	=	normal / 16.0f;

			EmbreeExtensions.UpdateRay( ref ray, position + bias, lightDir, 0, 9999 );

			var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
			return	nDotL * lightColor * shadow;
		}


		/// <summary>
		/// Rasterizes LM texcoords to lightmap
		/// </summary>
		/// <param name="lightmap"></param>
		/// <param name="instance"></param>
		void RasterizeInstance ( LightMapSet lightmap, MeshInstance instance, float w, float h )
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
								.Select( v2 => v2.TexCoord0 * new Vector2(w,h) )
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

				Rasterizer.RasterizeTriangleConservative( d0, d1, d2, 
					(xy,s,t,coverage) => {
						if (!lightmap.Coverage[xy]) {
							lightmap.Albedo	 [xy] = Color.Yellow;// InterpolateColor	( c0, c1, c2, s, t );
							lightmap.Position[xy] = InterpolatePosition	( p0, p1, p2, s, t ) + bias;
							lightmap.Normal  [xy] = InterpolateNormal	( n0, n1, n2, s, t );
							lightmap.Coverage[xy] = coverage;
						} else {
							if (coverage) {
								Log.Warning("LM coverage conflict: {0}", xy );
							}
						}
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
