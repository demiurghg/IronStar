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
using Fusion.Core.Extensions;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Scenes;
using System.IO;
using Fusion.Engine.Graphics.GI;

namespace Fusion.Engine.Graphics.Lights {

	internal partial class LightMapper : RenderComponent {

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

		Random rand		=	new Random();
		
		LightMapContent lightmapGBuffer;


		MeshInstance[] SelectOccludingInstances ( IEnumerable<MeshInstance> instances )
		{
			return instances
					.Where( inst => inst.Group==InstanceGroup.Static || inst.Group==InstanceGroup.Kinematic )
					.ToArray();
		}



		/// <summary>
		/// Update lightmap
		/// </summary>
		public LightMapContent BakeLightMap ( IEnumerable<MeshInstance> instances2, LightSet lightSet, RadiositySettings settings )
		{
			var hammersley		=	Hammersley.GenerateSphereUniform(settings.LightMapSampleCount);
			var instances		=	SelectOccludingInstances( instances2 );

			//-------------------------------------------------

			Log.Message("Allocaing lightmap regions...");

			int totalSizeInPixels = 0;

			var lmGroups = instances
					.Where( i0 => i0.Group==InstanceGroup.Static )
					.GroupBy( 
						instance => instance.LightMapGuid, // guid
						instance => instance,              // instance
						(guid,inst) => new LightMapGroup( inst.First().LightMapSize.Width, guid, inst, 0 )
					)
					.ToArray();

			int lightMapSize = 256;

			Allocator2D allocator;		

			while (true)
			{
				try {

					allocator = new Allocator2D( lightMapSize );

					foreach ( var group in lmGroups ) {
						var addr = allocator.Alloc( group.Region.Width, "");
						group.Region.X = addr.X;
						group.Region.Y = addr.Y;
					}

					break;
				} 
				catch ( OutOfMemoryException oom )
				{
					lightMapSize *= 2;
					if (lightMapSize>4096)
					{
						throw new OutOfMemoryException("Light map is too big (4096x4096)", oom);
					}
				}
			}

			Log.Message("Completed: {0} %", totalSizeInPixels / (float)(RenderSystem.LightmapSize * RenderSystem.LightmapSize) );

			//-------------------------------------------------

			Log.Message("Allocating buffers...");

			lightmapGBuffer		=	new LightMapContent( allocator.Width );

			foreach ( var group in lmGroups ) {
				lightmapGBuffer.Regions.Add( group.Guid, group.Region );
			}

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var group in lmGroups ) {
				foreach ( var instance in group.Instances ) {
					instance.BakingLMRegion = group.Region;
					RasterizeInstance( lightmapGBuffer, instance, group.Region );
				}
			}

			Log.Message("Generating patch LODs...");
			lightmapGBuffer.GeneratePatchLods();

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				using ( var scene = BuildRtcScene( rtc, instances ) ) {

					Log.Message("Fix geometry overlaps...");

					ForEachLightMapPixel( lmGroups, (i,j) => 
					{
						var p = lightmapGBuffer.Position[i,j];
						var n = lightmapGBuffer.Normal[i,j];

						p = FixGeometryOverlap( scene, p, n );

						lightmapGBuffer.Position[i,j] = p;
					}, true);

					//--------------------------------------

					Log.Message("Searching for radiating patches...");

					ForEachLightMapPixel( lmGroups, (i,j) => 
					{
						var p = lightmapGBuffer.Position[i,j];
						var n = lightmapGBuffer.Normal[i,j];
						var c = lightmapGBuffer.Albedo[i,j];

						if (c.A>0) 
						{
							var r = GatherRadiosityPatches( scene, instances, hammersley, lightSet, p, n, settings );

							lightmapGBuffer.Sky		[i,j]	=	r.Sky;
							lightmapGBuffer.IndexMap[i,j]	=	lightmapGBuffer.AddFormFactorPatchIndices( r.Patches, settings );
						} 
						else 
						{
							lightmapGBuffer.Sky		[i,j]	=	Vector3.Zero;
							lightmapGBuffer.IndexMap[i,j]	=	0;
						}
					}, true);
				}
			}

			//--------------------------------------

			if (settings.DebugLightmaps)
			{
				Log.Message("Saving debug images...");
				lightmapGBuffer.SaveDebugImages();
			}
	
			Log.Message("Completed.");

			return lightmapGBuffer;
		}


		byte GradeSampleCount(int samples)
		{
			if (samples>128) return 0;
			if (samples> 64) return 1;
			if (samples> 32) return 2;
			if (samples> 16) return 3;
			if (samples>  8) return 4;
			if (samples>  4) return 5;
			return 255;
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
								var y = ( j-h/2f ) * stride + stride/2;
								var z = ( k-d/2f ) * stride + stride/2;

								var p = new Vector3(x,y,z);
								var n = Vector3.Zero;

								//var r	=	GatherRadiosityPatches( scene, instances, hammersley, lightSet, p, n, -0*stride/2.0f );
							}
						}
					}
				}
			}

			irrVolume.UpdateGPUTextures();

			return irrVolume;
		}



		/// <summary>
		/// Fix centroid partially overlapped by another geometry
		/// </summary>
		Vector3 FixGeometryOverlap ( RtcScene scene, Vector3 position, Vector3 normal)
		{
			var basis	=	MathUtil.ComputeAimedBasis( normal );
			var dirs	=	new[] { basis.Right, basis.Left, basis.Up, basis.Down };
			var ray		=	new RtcRay();
			var minT	=	float.MaxValue;
			var result	=	position;

			foreach ( var dir in dirs ) 
			{
				EmbreeExtensions.UpdateRay( ref ray, position - dir*0.125f, dir, 0, 3 );

				if ( scene.Intersect( ref ray ) ) 
				{
					if ( ray.TFar < minT ) 
					{
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


		/// <summary>
		/// Gets integer coordinates where ray hist lightmap
		/// </summary>
		bool GetLightMapCoordinates( MeshInstance[] instances, ref RtcRay ray, out Int2 coord )
		{
			var geomId	=	ray.GeometryId;
			var primId	=	ray.PrimitiveId;
			coord		=	Int2.Zero;

			if (geomId==RtcRay.InvalidGeometryID) 
			{
				return false;
			}

			var instance =	instances[geomId];
			var triangle =	instance.Mesh.Triangles[(int)primId];
			var v0		 =	instance.Mesh.Vertices[ triangle.Index0 ].TexCoord1;
			var v1		 =	instance.Mesh.Vertices[ triangle.Index1 ].TexCoord1;
			var v2		 =	instance.Mesh.Vertices[ triangle.Index2 ].TexCoord1;

			var lmScale	 =	new Vector2( instance.LightMapScaleOffset.X, instance.LightMapScaleOffset.Y );
			var lmOffset =	new Vector2( instance.LightMapScaleOffset.Z, instance.LightMapScaleOffset.W );

			var lmTC	 =	InterpolateTexCoord( v0, v1, v2, ray.HitU, ray.HitV );

			var lmRect	=	instance.BakingLMRegion;

			var i		=	MathUtil.Lerp( lmRect.Left, lmRect.Right,  lmTC.X );
			var j		=	MathUtil.Lerp( lmRect.Top,  lmRect.Bottom, lmTC.Y );
			var w		=	RenderSystem.LightmapSize;
			var h		=	RenderSystem.LightmapSize;

			if (i<0 || j<0 || i>=w || j>=h )
			{
				return false;
			}

			coord	=	new Int2( i, j );
			return true;
		}


		byte GetMaximumPatchSize( float distance )
		{
			if (distance< 2*2) return  1;
			if (distance< 4*2) return  2;
			if (distance< 8*2) return  4;
			if (distance<16*2) return  8;
			if (distance<32*2) return 16;
			if (distance<64*2) return 32;
			return 32;
		}


		class GatheringResults 
		{
			public PatchIndex[]	Patches;
			public Vector3		Sky;
		}


		/// <summary>
		/// Computes indirect radiance in given point
		/// </summary>
		GatheringResults GatherRadiosityPatches ( RtcScene scene, MeshInstance[] instances, Vector3[] randomPoints, LightSet lightSet, Vector3 position, Vector3 normal, RadiositySettings settings, float bias=0 )
		{
			var sampleCount		=	randomPoints.Length;
			var invSampleCount	=	1.0f / sampleCount;
			var	result			=	new GatheringResults();

			var normalLength	=	normal.Length();

			//---------------------------------
			var randVector		=	rand.NextVector3(-Vector3.One, Vector3.One).Normalized();

			var lmAddrList = new List<PatchIndex>();

			for ( int i = 0; i<sampleCount; i++ ) {

				var dir		= Vector3.Reflect( -randomPoints[i], randVector );

				var nDotL	= Vector3.Dot( dir, normal );

				if (normalLength>0 && nDotL<=0) {
					continue;
				}

				var ray		=	new RtcRay();
				var pos		=	position + dir * bias;

				EmbreeExtensions.UpdateRay( ref ray, pos, dir, 0, 4096 );

				var intersect	=	 scene.Intersect( ref ray );
					
				//-------------------------------------------
				//	ray hits nothing, so this is sky light :
				if (!intersect && dir.Y>0) 
				{
					result.Sky	+=	dir * invSampleCount;
				}

				//-------------------------------------------
				//	trying to find direct light :
				if (intersect) 
				{
					var origin		=	EmbreeExtensions.Convert( ray.Origin );
					var distance	=	ray.TFar; // we assume, that dir is normalized
					var direction	=	EmbreeExtensions.Convert( ray.Direction );
					var hitPoint	=	origin + direction * (ray.TFar);
					var hitNormal	=	(-1) * EmbreeExtensions.Convert( ray.HitNormal ).Normalized();

					var dirDotN		=	Vector3.Dot( hitNormal, -direction );

					if (dirDotN>0) // we hit front side of the face
					{
						Int2 coords;
						Int3 patch;

						if (GetLightMapCoordinates( instances, ref ray, out coords ))
						{
							if (lightmapGBuffer.SelectPatch( coords, distance, dirDotN, settings, out patch ) )
							{
								var area	=	lightmapGBuffer.Area	[ patch ];
								var ppos	=	lightmapGBuffer.Position[ patch ];
								var pnormal	=	lightmapGBuffer.Normal	[ patch ].Normalized();
								var pdir	=	Vector3.Normalize( origin - ppos );
								var pdist	=	Vector3.Distance( ppos, origin );
								var pDotL	=	Vector3.Dot( pdir, pnormal );
								var weight	=	area * pDotL * Radiosity.Falloff(pdist) / 4.0f / MathUtil.Pi;
								var index	=	Radiosity.EncodeLMAddress( patch );

								if (weight>settings.RadianceThreshold) 
								{
									lmAddrList.Add( new PatchIndex( patch, 1 ) );
								}
							}
						}

					}
				}
			} 

			result.Patches	=	lmAddrList.ToArray();

			return result;
		}


		/// <summary>
		/// Rasterizes LM texcoords to lightmap
		/// </summary>
		/// <param name="lightmap"></param>
		/// <param name="instance"></param>
		void RasterizeInstance ( LightMapContent lightmap, MeshInstance instance, Rectangle viewport )
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

			foreach ( var subset in instance.Subsets )
			{
				var segment =	rs.RenderWorld.VirtualTexture.GetTextureSegmentInfo( subset.Name );
				var albedo	=	segment.AverageColor;
				albedo.A	=	255;

				for (int i=subset.StartPrimitive; i<subset.StartPrimitive+subset.PrimitiveCount; i++) 
				{
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

					var n		=	Vector3.Cross( p1 - p0, p2 - p0 ).Normalized();
					var area	=	ComputeLightMapTexelArea( p0, p1, p2,  d0, d1, d2 );
					var bias	=	n * 1 / 16.0f;

					Rasterizer.RasterizeTriangleConservative( d0, d1, d2, //Rasterizer.Samples8x,
						(xy,s,t,coverage) => 
						{
							if (lightmap.Coverage[xy]==0) 
							{
								lightmap.Albedo	 [xy] =	albedo;
								lightmap.Position[xy] = InterpolatePosition	( p0, p1, p2, s, t ) + bias;
								lightmap.Normal  [xy] = InterpolateNormal	( n0, n1, n2, s, t );
								lightmap.Area	 [xy] = area;
								lightmap.Coverage[xy] = coverage;
							}
							else
							{
								if (coverage!=0) 
								{
									//Log.Warning("LM coverage conflict: {0}", xy );
								}
							}
						} 
					);
				}
			}
		}
	}
}
