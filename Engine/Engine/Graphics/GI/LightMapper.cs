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

	internal partial class LightMapper : DisposableBase {

		readonly RadiositySettings settings;
		readonly Vector3[] hammersley;
		readonly MeshInstance[] instances;
		readonly RenderSystem rs;

		FormFactor formFactor;

		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMapper( RenderSystem rs, RadiositySettings settings, IEnumerable<MeshInstance> instances )
		{
			this.rs			=	rs;
			this.settings	=	settings;
			hammersley		=	Hammersley.GenerateSphereUniform(settings.LightMapSampleCount);
			this.instances	=	instances
					.Where( inst => inst.Group==InstanceGroup.Static || inst.Group==InstanceGroup.Kinematic )
					.ToArray();
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


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Lightmap stuff
		 * 
		-----------------------------------------------------------------------------------------*/

		Random rand		=	new Random();
		

		/// <summary>
		/// Update lightmap
		/// </summary>
		public FormFactor BakeLightMap ()
		{
			var stopwatch		=	new Stopwatch();
			stopwatch.Start();

			//-------------------------------------------------
			Log.Message("");
			Log.Message("-------- Building radiosity form-factor --------");

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

			int lightMapSize = 32;

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

			formFactor		=	new FormFactor( allocator.Width );

			foreach ( var group in lmGroups ) {
				formFactor.Regions.Add( group.Guid, group.Region );
			}

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var group in lmGroups ) {
				foreach ( var instance in group.Instances ) {
					instance.BakingLMRegion = group.Region;
					RasterizeInstance( formFactor, instance, group.Region, settings );
				}
			}

			Log.Message("Generating patch LODs...");
			formFactor.GeneratePatchLods();

			//--------------------------------------

			using ( var rtc = new Rtc() ) {

				using ( var scene = BuildRtcScene( rtc, instances ) ) {

					Log.Message("Fix geometry overlaps...");

					ForEachLightMapPixel( lmGroups, (i,j) => 
					{
						var p = formFactor.Position[i,j];
						var n = formFactor.Normal[i,j];

						p = FixGeometryOverlap( scene, p, n );

						formFactor.Position[i,j] = p;
					}, true);

					//--------------------------------------

					Log.Message("Searching for radiating patches...");

					ForEachLightMapTile( lmGroups, (tx,ty) => BakeTile( scene, tx, ty ) );
				}
			}

			//--------------------------------------

			if (settings.DebugLightmaps)
			{
				Log.Message("Saving debug images...");
				formFactor.SaveDebugImages();
			}
	
			Log.Message("Generating bounding boxes...");
			formFactor.ComputeBoundingBoxes();

			stopwatch.Stop();
			Log.Message("Completed : build time : {0}", stopwatch.Elapsed.ToString());

			var sampleCount =  formFactor.IndexMap.GetLinearData()
				.Select( index => index & 0xFF )
				.Where( count => count!=0 );

			var cachedSamples = formFactor.Tiles.GetLinearData()
				.Select( cached => cached.Y )
				.Where( count => count!=0 );

			Log.Message("   average cached samples : {0:0.00}", cachedSamples.Average( s => s ) );
			Log.Message("   max cached samples     : {0}", cachedSamples.Max( s => s ) );
			Log.Message("   min cached samples     : {0}", cachedSamples.Min( s => s ) );

			Log.Message("----------------");

			return formFactor;
		}



		/// <summary>
		/// 
		/// </summary>
		void BakeTile ( RtcScene scene, int tileX, int tileY )
		{
			int tileSize			=	RadiositySettings.TileSize;
			var offset				=	new Int2( tileX * tileSize, tileY * tileSize );
			var gatheringResults	=	new GatheringResults[tileSize*tileSize];

			for (uint i=0; i<tileSize*tileSize; i++)
			{
				var xy = MortonCode.Decode2( i ) + offset;

				var p = formFactor.Position[xy];
				var n = formFactor.Normal[xy];
				var c = formFactor.Albedo[xy];

				gatheringResults[i]	=	(c.A > 0) ? GatherRadiosityPatches( scene, p, n ) : null;
			}

			//	merge all hit patches (thir coords) and upload cache-line to formfactor
			//	retrieving (offset, count) of uploaded cache-line
			var globalPatches	=	gatheringResults
				.Where( results0 => results0 != null )
				.SelectMany( results1 => results1.Patches )
				.DistinctBy( patch => patch.Coords )
				.ToArray();

			formFactor.Tiles[tileX,tileY]	=	formFactor.AddGlobalPatchIndices( globalPatches );

			for (uint i=0; i<tileSize*tileSize; i++)
			{
				var xy = MortonCode.Decode2( i ) + offset;

				if (gatheringResults[i]==null)
				{
					formFactor.IndexMap[xy] = 0;
					formFactor.Sky[xy] = Vector3.Zero;
				}
				else
				{
					var cachedPatches	=	gatheringResults[i].Patches	
						.GroupBy( patch0 => patch0.Coords )
						.Select( group => new { 
							Patch = group.First(), 
							Hits = group.Count(),
							Dir = Radiosity.EncodeDirection( formFactor.Position[ group.First().Coords ] - gatheringResults[i].Origin )
						} )
						.Select( patch1 => new CachedPatchIndex( GetPatchIndexInCache(globalPatches, patch1.Patch), patch1.Dir, patch1.Hits ) )
						.ToArray();

					formFactor.IndexMap[xy] = formFactor.AddCachedPatchIndices( cachedPatches );
					formFactor.Sky[xy]		= gatheringResults[i].Sky;
				}
			}
		}


		int GetPatchIndexInCache( GlobalPatchIndex[] cacheLine, GlobalPatchIndex patch )
		{
			for (int i=0; i<cacheLine.Length; i++)
			{
				if (cacheLine[i].Index == patch.Index) return i;
			}
			return -1;
		}


		/// <summary>
		/// 
		/// </summary>
		public IrradianceVolume BakeIrradianceVolume (	IEnumerable<MeshInstance> instances2, LightSet lightSet, int numSamples, int w, int h, int d, float stride )
		{
			#if false
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
			#endif

			return null; //irrVolume;
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
		bool GetLightMapCoordinates( ref RtcRay ray, out Int2 coord )
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


		class GatheringResults 
		{
			public GatheringResults( Vector3 origin ) { Origin = origin; }
			public readonly Vector3	Origin;
			public Vector3	Sky;
			public GlobalPatchIndex[]	Patches;
		}


		/// <summary>
		/// Computes indirect radiance in given point
		/// </summary>
		GatheringResults GatherRadiosityPatches ( RtcScene scene, Vector3 position, Vector3 normal, float bias=0 )
		{
			var sampleCount		=	hammersley.Length;
			var invSampleCount	=	1.0f / sampleCount;
			var	result			=	new GatheringResults(position);

			var normalLength	=	normal.Length();

			//---------------------------------
			var randVector		=	rand.NextVector3(-Vector3.One, Vector3.One).Normalized();

			var lmAddrList = new List<GlobalPatchIndex>();

			for ( int i = 0; i<sampleCount; i++ ) {

				var dir		= Vector3.Reflect( -hammersley[i], randVector );

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

						if (GetLightMapCoordinates( ref ray, out coords ))
						{
							if (formFactor.SelectPatch( coords, distance, dirDotN, settings, out patch ) )
							{
								var area	=	formFactor.Area	[ patch ];
								var ppos	=	formFactor.Position[ patch ];
								var pnormal	=	formFactor.Normal	[ patch ].Normalized();
								var pdir	=	Vector3.Normalize( origin - ppos );
								var pdist	=	Vector3.Distance( ppos, origin );
								var pDotL	=	Vector3.Dot( pdir, pnormal );
								var weight	=	area * pDotL * Radiosity.Falloff(pdist) / 4.0f / MathUtil.Pi;

								if (weight>settings.RadianceThreshold) 
								{
									lmAddrList.Add( new GlobalPatchIndex( patch ) );
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
		void RasterizeInstance ( FormFactor lightmap, MeshInstance instance, Rectangle viewport, RadiositySettings settings )
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
				var albedo	=	settings.UseWhiteDiffuse ? new Color(0.5f) : segment.AverageColor;
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
