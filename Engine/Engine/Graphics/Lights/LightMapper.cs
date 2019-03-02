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

namespace Fusion.Engine.Graphics.Lights {

	[RequireShader("lightmap", true)]
	internal class LightMapper : RenderComponent {

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


		public ShaderResource LightMapSHL1R {
			get { return lightMapSHL1R ?? blackLightMap.Srv; }
		}
		public ShaderResource LightMapSHL1G {
			get { return lightMapSHL1G ?? blackLightMap.Srv; }
		}
		public ShaderResource LightMapSHL1B {
			get { return lightMapSHL1B ?? blackLightMap.Srv; }
		}

		public ShaderResource LightMap3D {
			get { return lightMap3D; }
		}

		public Matrix LightMap3DMatrix {
			get { return Matrix.Identity; }
		}


		Texture2D		gbufferPosition;
		Texture2D		gbufferNormal;
		Texture2D		gbufferColor;
		Texture2D		lightMapSHL1R;
		Texture2D		lightMapSHL1G;
		Texture2D		lightMapSHL1B;
		Texture3D		lightMap3D;
		DynamicTexture	blackLightMap;

		LightMapGBuffer	lightMapSet;


		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMapper(RenderSystem rs) : base(rs)
		{
			constBuffer		=	new ConstantBuffer( rs.Device, typeof(BAKE_PARAMS) );

			blackLightMap	=	new DynamicTexture(rs, 32, 32, typeof(Color), false, false );
			blackLightMap.SetData( new GenericImage<Color>(32,32,Color.Black).RawImageData );

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
				SafeDispose( ref lightMapSHL1R	 );
				SafeDispose( ref lightMapSHL1G	 );
				SafeDispose( ref lightMapSHL1B	 );
				SafeDispose( ref lightMap3D		 );
				SafeDispose( ref blackLightMap	 );
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

		class LMGroup {
			public LMGroup ( int size, IEnumerable<MeshInstance> instances )
			{
				Region = new Rectangle(0,0,size,size);
				Instances = instances.ToArray();
			}
			public Rectangle Region;
			public MeshInstance[] Instances;
		}


		Random rand		=	new Random();

		/// <summary>
		/// Update lightmap
		/// </summary>
		public LightMapGBuffer BakeLightMap ( IEnumerable<MeshInstance> instances, LightSet lightSet, int numSamples, bool filter, int sizeBias )
		{
			var hammersley		=	Hammersley.GenerateSphereUniform(numSamples);
			var instanceArray	=	instances.ToArray();

			//-------------------------------------------------

			Log.Message("Allocaing lightmap regions...");

			var lmGroups = instances
					.Where( i0 => i0.Group==InstanceGroup.Static )
					.GroupBy( 
						instance => instance.LightMapTag,
						instance => instance,
						(tag,inst) => new LMGroup( inst.First().LightMapSize.Width, inst )
					)
					.ToArray();


			Allocator2D allocator = null;

			for ( int size = 256; size<=4096; size *= 2 ) {

				Log.Message("...attempt: {0}x{0}", size );

				allocator = new Allocator2D(size);

				try {

					foreach ( var group in lmGroups ) {

						var addr = allocator.Alloc( group.Region.Width, "");

						group.Region.X = addr.X;
						group.Region.Y = addr.Y;

						foreach ( var inst in group.Instances ) {
							inst.LightMapScaleOffset	=	group.Region.GetMadOpScaleOffsetNDC( allocator.Width, allocator.Height );
						}
					}

					Log.Message("Completed");
					break;

				} catch ( OutOfMemoryException ) {
					continue;
				}
			}

			//-------------------------------------------------

			Log.Message("Allocating buffers...");

			var lightmap	=	new LightMapGBuffer( allocator.Width, allocator.Height );

			lightMapSHL1R	=	new Texture2D( rs.Device, allocator.Width, allocator.Height, ColorFormat.Rgba32F, false, false );
			lightMapSHL1G	=	new Texture2D( rs.Device, allocator.Width, allocator.Height, ColorFormat.Rgba32F, false, false );
			lightMapSHL1B	=	new Texture2D( rs.Device, allocator.Width, allocator.Height, ColorFormat.Rgba32F, false, false );

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var group in lmGroups ) {
				foreach ( var instance in group.Instances ) {
					RasterizeInstance( lightmap, instance, group.Region );
				}
			}

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				var sceneFlags	=	SceneFlags.Static|SceneFlags.Coherent;
				var algFlags	=	AlgorithmFlags.Intersect1;

				using ( var scene = new RtcScene( rtc, sceneFlags, algFlags ) ) {

					//--------------------------------------

					Log.Message("Generating RTC scene...");

					foreach ( var instance in instances ) {
						if (instance.Group==InstanceGroup.Static || instance.Group==InstanceGroup.Dynamic) {
							AddMeshInstance( scene, instance );
						}
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

					Log.Message("Indirect light ray tracing...");

					var sw = new Stopwatch();
					sw.Start();

					for ( int i=0; i<lightmap.Width; i++ ) {

						Log.Message("... tracing : {0}/{1}", i, lightmap.Width );

						for ( int j=0; j<lightmap.Height; j++ ) {

							var p = lightmap.Position[i,j];
							var n = lightmap.Normal[i,j];
							var c = lightmap.Albedo[i,j];

							if (c.A>0) {
								var r	=	ComputeRadiance( scene, instanceArray, hammersley, lightSet, p, n );
								lightmap.IrradianceR[i,j]	=	r.Red;
								lightmap.IrradianceG[i,j]	=	r.Green;
								lightmap.IrradianceB[i,j]	=	r.Blue;
							} else {
								lightmap.IrradianceR[i,j]	=	SHL1.Zero;
								lightmap.IrradianceG[i,j]	=	SHL1.Zero;
								lightmap.IrradianceB[i,j]	=	SHL1.Zero;
							}
						}
					}  //*/

					sw.Stop();
					Log.Message("{0} ms", sw.ElapsedMilliseconds);

				}
			}	 //*/

			//--------------------------------------

			Log.Message("Dilate radiance...");

			lightmap.DilateRadiance();

			if (filter) {
				lightmap.BlurRadianceBilateral();
			}
			
			//--------------------------------------

			Log.Message("Uploading lightmap to GPU...");

			lightMapSHL1R.SetData( lightmap.IrradianceR.RawImageData );
			lightMapSHL1G.SetData( lightmap.IrradianceG.RawImageData );
			lightMapSHL1B.SetData( lightmap.IrradianceB.RawImageData );

			var image = new Image( lightmap.Albedo );
			Image.SaveTga( image, @"E:\GITHUB\testlm.tga" );

			Log.Message("Completed.");

			this.lightMapSet	=	lightmap;

			return lightmap;
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



		Color4 GetAlbedo ( MeshInstance[] instances, ref RtcRay ray )
		{
			var geomId	=	ray.GeometryId;
			var primId	=	ray.PrimitiveId;

			if (geomId==RtcRay.InvalidGeometryID) {
				return Color4.Zero;
			}

			var instance = instances[geomId];

			foreach ( var subset in instance.Subsets ) {
				if (primId>=subset.StartPrimitive && primId<subset.StartPrimitive+subset.PrimitiveCount) {
					var segment = rs.RenderWorld.VirtualTexture.GetTextureSegmentInfo( subset.Name );
					return segment.AverageColor.ToColor4();
				}
			}

			return new Color4(1,0,1,1);
		}

		
		Irradiance ComputeRadiance ( RtcScene scene, MeshInstance[] instances, Vector3[] randomPoints, LightSet lightSet, Vector3 position, Vector3 normal )
		{
			var sampleCount		=	randomPoints.Length;
			var invSampleCount	=	1.0f / sampleCount;

			var skyAmbient		=	rs.RenderWorld.SkySettings.AmbientLevel;

			var irradiance		=	new Irradiance();

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
					irradiance.Add( skyAmbient * invSampleCount, dir );
				}

				//-------------------------------------------
				//	trying to find direct light :
				if (intersect) {

					var albedo		=	GetAlbedo( instances, ref ray );

					var origin		=	EmbreeExtensions.Convert( ray.Origin );
					var direction	=	EmbreeExtensions.Convert( ray.Direction );
					var hitPoint	=	origin + direction * (ray.TFar);
					var hitNormal	=	(-1) * EmbreeExtensions.Convert( ray.HitNormal ).Normalized();

					var dirDotN		=	Vector3.Dot( hitNormal, direction );

					if (dirDotN<0) // we hit front side of the face
					{
						var directLight	=	ComputeDirectLight( scene, lightSet, hitPoint, hitNormal );

						irradiance.Add( directLight * invSampleCount * albedo * (-dirDotN), dir );
					}
				}
			} 

			return irradiance;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		Color4 ComputeDirectLight ( RtcScene scene, LightSet lightSet, Vector3 position, Vector3 normal )
		{
			var dirLightDir		=	-(lightSet.DirectLight.Direction).Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity;

			var directLight		=	Color4.Zero;
			var ray				=	new RtcRay();

			if (true) {
				var nDotL	=	 Math.Max( 0, Vector3.Dot( dirLightDir, normal ) );


				var bias	=	normal / 16.0f;

				if (nDotL>0) {
					EmbreeExtensions.UpdateRay( ref ray, position + bias, dirLightDir, 0, 9999 );

					var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * dirLightColor * shadow;
				}
			}

			foreach ( var ol in lightSet.OmniLights ) {

				var dir		=	ol.Position - position;
				var dirN	=	dir.Normalized();
				var falloff	=	MathUtil.Clamp( 1 - dir.Length() / ol.RadiusOuter, 0, 1 );
					falloff *=	falloff;

				var nDotL	=	Math.Max( 0, Vector3.Dot( dirN, normal ) );

				if ( falloff * nDotL > 0 ) {

					EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 1 );
					var shadow	=	 1;//scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * falloff * shadow * ol.Intensity;
				}

			}

			return directLight;
		}


		/// <summary>
		/// Rasterizes LM texcoords to lightmap
		/// </summary>
		/// <param name="lightmap"></param>
		/// <param name="instance"></param>
		void RasterizeInstance ( LightMapGBuffer lightmap, MeshInstance instance, Rectangle viewport )
		{
			var mesh		=	instance.Mesh;

			var scale		=	new Vector2( viewport.Width, viewport.Height );
			var offset		=	new Vector2( viewport.X, viewport.Y );

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
								.Select( v2 => v2.TexCoord1 * scale + offset )
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
								//Log.Warning("LM coverage conflict: {0}", xy );
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
			Log.Message("{0}", id);

			var pVerts	=	scene.MapBuffer( id, BufferType.VertexBuffer );
			var pInds	=	scene.MapBuffer( id, BufferType.IndexBuffer );

			SharpDX.Utilities.Write( pVerts, vertices, 0, vertices.Length );
			SharpDX.Utilities.Write( pInds,  indices,  0, indices.Length );

			scene.UnmapBuffer( id, BufferType.VertexBuffer );
			scene.UnmapBuffer( id, BufferType.IndexBuffer );
		}
	}
}
