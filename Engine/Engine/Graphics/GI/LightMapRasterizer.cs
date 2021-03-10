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
using Fusion.Engine.Graphics.Scenes;
using System.IO;
using Fusion.Engine.Graphics.GI;

namespace Fusion.Engine.Graphics.GI 
{
	internal partial class LightMapRasterizer : DisposableBase 
	{
		const int MinLightMapSize = Radiosity.RegionSize;
		const float NormalBias = 1.0f / 16.0f;

		readonly RenderInstance[] instances;
		readonly RenderSystem rs;
		LightMapGBuffer lmGBuffer;
		RadiositySettings settings;

		/// <summary>
		/// Creates instance of the Lightmap
		/// </summary>
		public LightMapRasterizer( RenderSystem rs, IEnumerable<RenderInstance> instances, RadiositySettings settings )
		{
			this.settings	=	settings;
			this.rs			=	rs;
			this.instances	=	instances.ToArray();
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
		#warning remove debug points for lightmaps
		public static List<Vector3> debug_points = new List<Vector3>();
		public static List<Vector3> debug_normals = new List<Vector3>();
		

		/// <summary>
		/// Update lightmap
		/// </summary>
		public LightMapGBuffer RasterizeGBuffer ()
		{
			var stopwatch		=	new Stopwatch();
			stopwatch.Start();
			debug_points.Clear();
			debug_normals.Clear();

			//-------------------------------------------------
			Log.Message("Allocating lightmap regions...");

			int totalPixels = 0;
			int bias = settings.Bias;

			var lmGroups = instances
					.GroupBy( 
						instance => instance.LightMapRegionName,
						instance => instance,
						(name,inst) => new LightMapGroup( inst.First().LightMapSize.Width, name, inst, bias )
					)
					.ToArray();

			//	minimum size of the lightmap 
			//	must be equal size of the update region
			int lightMapSize = MinLightMapSize;

			Allocator2D allocator;		

			while (true)
			{
				try 
				{
					allocator = new Allocator2D( lightMapSize );

					foreach ( var group in lmGroups ) 
					{
						var addr = allocator.Alloc( group.Region.Width, "");
						group.Region.X = addr.X;
						group.Region.Y = addr.Y;

						totalPixels += group.Region.Width * group.Region.Height;
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

			float utilization = totalPixels / (float)(allocator.Width * allocator.Height);
			Log.Message("Allocating completed: {0}x{1}, {2:0.0}%", allocator.Width, allocator.Height, utilization * 100 );

			//-------------------------------------------------

			lmGBuffer		=	new LightMapGBuffer( rs, allocator.Width );

			foreach ( var group in lmGroups ) 
			{
				lmGBuffer.Regions.Add( group.Name, group.Region );
			}

			//-------------------------------------------------

			Log.Message("Rasterizing lightmap G-buffer...");

			foreach ( var group in lmGroups ) 
			{
				foreach ( var instance in group.Instances ) 
				{
					instance.BakingLMRegion = group.Region;
					RasterizeInstance( lmGBuffer, instance, group.Region );
				}
			}

			//--------------------------------------

			using ( var rtc = new Rtc() ) 
			{
				using ( var scene = BuildRtcScene( rtc, instances ) ) 
				{
					Log.Message("Fix geometry overlaps...");

					ForEachLightMapPixel( lmGroups, (i,j) => 
					{
						var p = lmGBuffer.Position[i,j];
						var n = lmGBuffer.Normal[i,j];
						var a = lmGBuffer.Area[i,j];

						p = FixGeometryOverlap( scene, p, n, a );

						lmGBuffer.Position[i,j] = p;

						//debug_points.Add( p );
						//debug_normals.Add( n );

					}, true);
				}
			}

			//--------------------------------------

			lmGBuffer.ComputeBoundingBoxes();

			//--------------------------------------

			stopwatch.Stop();
			Log.Message("Resterizing completed : {0}", stopwatch.Elapsed.ToString());

			lmGBuffer.UpdateGpuData();

			return lmGBuffer;
		}



		/// <summary>
		/// Fix centroid partially overlapped by another geometry
		/// </summary>
		Vector3 FixGeometryOverlap ( RtcScene scene, Vector3 position, Vector3 normal, float area )
		{
			var basis	=	MathUtil.ComputeAimedBasis( normal );
			var dirs	=	new[] { basis.Right, basis.Left, basis.Up, basis.Down };
			var ray		=	new RtcRay();
			var minT	=	float.MaxValue;
			var result	=	position;


			foreach ( var dir in dirs ) 
			{
				var searchRadius	=	(float)Math.Sqrt(area) * 0.5f;
				var backOffset		=	dir * searchRadius * (float)Math.Sqrt(2.0f);
				EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, searchRadius );

				if ( scene.Intersect( ref ray ) ) 
				{
					if ( ray.TFar < minT ) 
					{
						var n	= -ray.GetHitNormal().Normalized();	

						if ( Vector3.Dot( n, dir ) > 0 ) 
						{
							minT	= ray.TFar;
							result	= ray.GetHitPoint() + n * NormalBias;
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

		/// <summary>
		/// Rasterizes LM texcoords to lightmap
		/// </summary>
		/// <param name="lightmap"></param>
		/// <param name="instance"></param>
		void RasterizeInstance ( LightMapGBuffer lightmap, RenderInstance instance, Rectangle viewport )
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
					var bias	=	n * NormalBias;

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
