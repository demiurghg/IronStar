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
using System.Runtime.CompilerServices;

namespace Fusion.Engine.Graphics.Lights {

	internal class LightMapper : RenderComponent {

		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMapper(RenderSystem rs) : base(rs)
		{
			LoadContent();

			Game.Reloading += (s,e) => LoadContent();
		}


		/// <summary>
		/// Loads content if necessary
		/// </summary>
		void LoadContent ()
		{
		}


		/// <summary>
		/// Disposes stuff 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
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

		class LMGroup {
			public LMGroup ( int size, Guid guid, IEnumerable<MeshInstance> instances )
			{
				Guid		=	guid;
				Region		=	new Rectangle(0,0,size,size);
				Instances	=	instances.ToArray();
			}
			public Guid Guid;
			public Rectangle Region;
			public MeshInstance[] Instances;
		}


		Random rand		=	new Random();


		MeshInstance[] SelectOccludingInstances ( IEnumerable<MeshInstance> instances )
		{
			return instances
					.Where( inst => inst.Group==InstanceGroup.Static || inst.Group==InstanceGroup.Kinematic )
					.ToArray();
		}


		/// <summary>
		/// Update lightmap
		/// </summary>
		public IrradianceMap BakeIrradianceMap ( IEnumerable<MeshInstance> instances2, LightSet lightSet, int numSamples, bool filter, int sizeBias )
		{
			var hammersley		=	Hammersley.GenerateSphereUniform(numSamples);
			var instances		=	SelectOccludingInstances( instances2 );

			//-------------------------------------------------

			Log.Message("Allocaing lightmap regions...");

			var lmGroups = instances
					.Where( i0 => i0.Group==InstanceGroup.Static )
					.GroupBy( 
						instance => instance.LightMapGuid, // guid
						instance => instance,              // instance
						(guid,inst) => new LMGroup( inst.First().LightMapSize.Width, guid, inst )
					)
					.ToArray();

			foreach ( var lmGroup in lmGroups ) {
				Log.Message("...{0} : {1}x{2}", lmGroup.Guid, lmGroup.Region.Width, lmGroup.Region.Height );
			}


			Allocator2D allocator = null;

			for ( int size = 256; size<=4096; size *= 2 ) {

				Log.Message("...attempt: {0}x{0}", size );

				allocator = new Allocator2D(size);

				try {

					foreach ( var group in lmGroups ) {
						var addr = allocator.Alloc( group.Region.Width, "");
						group.Region.X = addr.X;
						group.Region.Y = addr.Y;
					}

					Log.Message("Completed");
					break;

				} catch ( OutOfMemoryException ) {
					continue;
				}
			}

			//-------------------------------------------------

			Log.Message("Allocating buffers...");

			var irradianceMap	=	new IrradianceMap( rs, allocator.Width, allocator.Height );
			var lightmapGBuffer	=	new LightMapGBuffer( allocator.Width, allocator.Height );

			foreach ( var group in lmGroups ) {
				irradianceMap.AddRegion( group.Guid, group.Region );
			}

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var group in lmGroups ) {
				foreach ( var instance in group.Instances ) {
					RasterizeInstance( lightmapGBuffer, instance, group.Region );
				}
			}

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				using ( var scene = BuildRtcScene( rtc, instances ) ) {

					Log.Message("Fix geometry overlaps...");

					for ( int i=0; i<lightmapGBuffer.Width; i++ ) {
						for ( int j=0; j<lightmapGBuffer.Height; j++ ) {

							var p = lightmapGBuffer.Position[i,j];
							var n = lightmapGBuffer.Normal[i,j];
							lightmapGBuffer.PositionOld[i,j] = p;

							p = FixGeometryOverlap( scene, p, n );

							lightmapGBuffer.Position[i,j] = p;
						}
					}

					//--------------------------------------

					Log.Message("Indirect light ray tracing...");

					for ( int i=0; i<lightmapGBuffer.Width; i++ ) {

						Log.Message("... tracing : {0}/{1}", i, lightmapGBuffer.Width );

						for ( int j=0; j<lightmapGBuffer.Height; j++ ) {

							var p = lightmapGBuffer.Position[i,j];
							var n = lightmapGBuffer.Normal[i,j];
							var c = lightmapGBuffer.Albedo[i,j];

							if (c.A>0) {
								var r	=	ComputeRadiance( scene, instances, hammersley, lightSet, p, n );
								irradianceMap.IrradianceRed		[i,j]	=	r.Red;
								irradianceMap.IrradianceGreen	[i,j]	=	r.Green;
								irradianceMap.IrradianceBlue	[i,j]	=	r.Blue;
							} else {
								irradianceMap.IrradianceRed		[i,j]	=	SHL1.Zero;
								irradianceMap.IrradianceGreen	[i,j]	=	SHL1.Zero;
								irradianceMap.IrradianceBlue	[i,j]	=	SHL1.Zero;
							}
						}
					}
				}
			}

			//--------------------------------------

			Log.Message("Dilate radiance...");

			irradianceMap.DilateRadiance( lightmapGBuffer.Albedo );

			if (filter) {
				lightmapGBuffer.BlurRadianceBilateral();
			}
			
			//--------------------------------------

			Log.Message("Uploading lightmap to GPU...");

			irradianceMap.UpdateGPUTextures();

			Log.Message("Completed.");

			return irradianceMap;
		}



		/// <summary>
		/// 
		/// </summary>
		public IrradianceVolume BakeIrradianceVolume (	IEnumerable<MeshInstance> instances2, LightSet lightSet, int numSamples, int w, int h, int d, float stride )
		{
			var instances	=	SelectOccludingInstances( instances2 );
			var hammersley	=	Hammersley.GenerateSphereUniform(numSamples);
			var irrVolume	=	new IrradianceVolume( rs, w,h,d, stride );

			using ( var rtc = new Rtc() ) {

				using ( var scene = BuildRtcScene( rtc, instances ) ) {

					scene.Commit();

					Log.Message("Indirect light ray tracing...");

					Log.Message("   WHD: {0}x{1}x{2} @ {3}", irrVolume.Width, irrVolume.Height, irrVolume.Depth, irrVolume.Stride );

					for ( int i=0; i<irrVolume.Width; i++ ) {

						Log.Message("... tracing : {0}/{1}", i, irrVolume.Width );

						for ( int j=0; j<irrVolume.Height; j++ ) {

							for ( int k=0; k<irrVolume.Depth; k++ ) {

								var x = ( i-w/2f ) * stride + stride/2;
								var y = ( j      ) * stride + stride/2;
								var z = ( k-d/2f ) * stride + stride/2;

								var p = new Vector3(x,y,z);
								var n = Vector3.Zero;

								var r	=	ComputeRadiance( scene, instances, hammersley, lightSet, p, n, stride );
								irrVolume.IrradianceRed	 [i,j,k]	=	r.Red;
								irrVolume.IrradianceGreen[i,j,k]	=	r.Green;
								irrVolume.IrradianceBlue [i,j,k]	=	r.Blue;
							}
						}
					}
				}
			}

			return irrVolume;
		}



		/// <summary>
		/// 
		/// </summary>
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

			foreach ( var subset in instance.Subsets ) 
			{
				if (primId >= subset.StartPrimitive && primId < subset.StartPrimitive + subset.PrimitiveCount) 
				{
					var segment = rs.RenderWorld.VirtualTexture.GetTextureSegmentInfo( subset.Name );
					return segment.AverageColor.ToColor4();
				}
			}

			return new Color4(1,0,1,1);
		}

		

		Irradiance ComputeRadiance ( RtcScene scene, MeshInstance[] instances, Vector3[] randomPoints, LightSet lightSet, Vector3 position, Vector3 normal, float bias=0 )
		{
			var sampleCount		=	randomPoints.Length;
			var invSampleCount	=	1.0f / sampleCount;

			var skyAmbient		=	rs.RenderWorld.SkySettings.AmbientLevel;

			var irradiance		=	new Irradiance();
			var normalLength	=	normal.Length();

			//---------------------------------

			for ( int i = 0; i<sampleCount; i++ ) {

				var dir		= randomPoints[i];

				var nDotL	= Vector3.Dot( dir, normal );

				if (normalLength>0 && nDotL<=0) {
					continue;
				}

				var ray		=	new RtcRay();
				var pos		=	position + dir * bias;

				EmbreeExtensions.UpdateRay( ref ray, pos, dir, 0, 1024 );

				var intersect	=	 scene.Intersect( ref ray );
					
				//-------------------------------------------
				//	ray hits nothing, so this is sky light :
				if (!intersect && dir.Y>0) {
					irradiance.Add( skyAmbient * invSampleCount, dir );
				}

				//-------------------------------------------
				//	trying to find direct light :
				if (intersect) {

					//Log.Message("HUY!");

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
			var bias			=	normal / 16.0f;

			position			+=	bias;

			if (true) 
			{
				var nDotL	=	 Math.Max( 0, Vector3.Dot( dirLightDir, normal ) );

				if (nDotL>0) 
				{
					EmbreeExtensions.UpdateRay( ref ray, position, dirLightDir, 0, 9999 );

					var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * dirLightColor * shadow;
				}
			}

			foreach ( var ol in lightSet.OmniLights ) 
			{
				var dir			=	ol.Position - position;
				var dist		=	dir.Length();
				var dirN		=	dir.Normalized();
				var falloff		=	MathUtil.Clamp( 1 - dir.Length() / ol.RadiusOuter, 0, 1 );
					falloff		*=	falloff;

				var falloff2	=	MathUtil.Clamp( 1 - dir.Length() / ol.RadiusOuter / 3, 0, 1 );

				var nDotL		=	Math.Max( 0, Vector3.Dot( dirN, normal ) );

				if ( falloff * falloff2 * nDotL > 0 ) {

					EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 1 );
					var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * falloff * shadow * ol.Intensity;
				}

			}

			foreach ( var sl in lightSet.SpotLights ) 
			{
				var dir			=	sl.Position - position;
				var dist		=	dir.Length();
				var dirN		=	dir.Normalized();
				var falloff		=	MathUtil.Clamp( 1 - dir.Length() / sl.RadiusOuter, 0, 1 );
					falloff		*=	falloff;

				var falloff2	=	MathUtil.Clamp( 1 - dir.Length() / sl.RadiusOuter / 3, 0, 1 );

				var nDotL		=	Math.Max( 0, Vector3.Dot( dirN, normal ) );

				var viewProj	=	sl.SpotView * sl.Projection;
				var projPos		=	Vector3.TransformCoordinate( position, viewProj );
				var axialDist	=	new Vector2( projPos.X, projPos.Y ).Length();

				if (axialDist<1) 
				{
					if ( falloff * falloff2 * nDotL > 0 ) 
					{
						EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 1 );
						var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
						directLight	+=	nDotL * falloff * shadow * sl.Intensity;
					}
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
		/// 
		/// </summary>
		/// <param name="rtc"></param>
		/// <param name="instances"></param>
		/// <returns></returns>
		RtcScene BuildRtcScene ( Rtc rtc, IEnumerable<MeshInstance> instances )
		{
			Log.Message("Generating RTC scene...");

			var sceneFlags	=	SceneFlags.Static|SceneFlags.Coherent;
			var algFlags	=	AlgorithmFlags.Intersect1;

			var scene		=	new RtcScene( rtc, sceneFlags, algFlags );

			foreach ( var instance in instances ) {
				if (instance.Group==InstanceGroup.Static || instance.Group==InstanceGroup.Kinematic) {
					AddMeshInstance( scene, instance );
				}
			}

			scene.Commit();

			return scene;
		}


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
