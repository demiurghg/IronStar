using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Reflection;
using Native.Fbx;
using IronStar.Entities;
using Fusion.Core.Content;
using System.IO;
using IronStar.Core;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using BEPUphysics.BroadPhaseEntries;
using Fusion.Core.Mathematics;
using Fusion;
using Native.Recast;
using Newtonsoft.Json;
using NRecast.Recast;
using NRecast.Detour;

namespace IronStar.Mapping {
	public partial class Map {

		Recast.rcConfig	cfg;
		Recast.rcContext ctx;
		Recast.rcHeightfield solid;
		Recast.rcCompactHeightfield chf;
		Recast.rcContourSet cset;
		Recast.rcPolyMesh pmesh;
		Recast.rcPolyMeshDetail dmesh;
		Detour.dtRawTileData navData;
		Detour.dtNavMesh navMesh;
		Detour.dtNavMeshQuery navQuery;
		Detour.dtQueryFilter filter;

		Vector3 pointStart	= new Vector3( 24.25f, -0.5f,  24.25f);
		Vector3 pointEnd	= new Vector3(-24.25f, -0.5f, -24.25f);

		readonly uint[] resultPath = new uint[256];
				 int	resultPathLen = 0;

		class Context : Recast.rcContext {
			protected override void doLog( Recast.rcLogCategory category, string msg )
			{
				Log.Verbose("{0} : {1}", category, msg);
			}
		}

		float Sqr( float x ) { return x * x; }



		public void GetStaticGeometry ( ContentManager content, out float[] verts, out int[] inds, out float[] bmin, out float[] bmax )
		{
			var indices = new List<int>();
			var vertices = new List<Vector3>();

			var staticModels = Nodes
					.Select( n1 => n1 as MapModel )
					.Where( n2 => n2 != null )
					.ToArray();


			foreach ( var mapNode in staticModels ) {

				if (!string.IsNullOrWhiteSpace( mapNode.ScenePath )) {

					var scene = content.Load<Scene>( mapNode.ScenePath );

					var nodeCount = scene.Nodes.Count;
					
					var worldMatricies = new Matrix[nodeCount];

					scene.ComputeAbsoluteTransforms( worldMatricies );

					for ( int i=0; i<scene.Nodes.Count; i++) {

						var worldMatrix = worldMatricies[i] * mapNode.WorldMatrix;

						var node = scene.Nodes[i];

						if (node.MeshIndex<0) {
							continue;
						}

						var mesh		=	scene.Meshes[ node.MeshIndex ];

						indices.AddRange( mesh.GetIndices( vertices.Count ) );

						vertices.AddRange( mesh.Vertices.Select( v1 => Vector3.TransformCoordinate( v1.Position, worldMatrix ) ) );
					}
				}
			}

			var bbox = BoundingBox.FromPoints( vertices );
			bmin = bbox.Minimum.ToArray();
			bmax = bbox.Maximum.ToArray();

			verts = new float[ vertices.Count * 3 ];
			inds  = indices.ToArray();

			for ( int i=0; i<vertices.Count; i++ ) {
				verts[ i*3+0 ] = vertices[i].X;
				verts[ i*3+1 ] = vertices[i].Y;
				verts[ i*3+2 ] = vertices[i].Z;
			}
		}

		

		public void BuildNavMesh ( ContentManager content )
		{
			float[] verts;
			int[] inds;
			float[] bmin;
			float[] bmax;

			GetStaticGeometry( content, out verts, out inds, out bmin, out bmax );

			var nverts = verts.Length / 3;
			var ntris  = inds.Length / 3;

			cfg	=	new Recast.rcConfig();
			ctx =	new Context();

			ctx.enableLog(true);
			ctx.enableTimer(true);

			//----------------------------------------------------------

			#region Step 1. Initialize build config.
			//	Rasterization :
			const float cellSize		=	0.25f;
			const float cellHeight		=	0.25f;

			//	Agent :
			const float walkableSlope	=	45;
			const float walkableHeight	=	2;
			const float walkableClimb	=	0.6f;
			const float walkableRadius	=	0.6f;

			//	Agent :
			//const float walkableSlope	=	15;
			//const float walkableHeight	=	3;
			//const float walkableClimb	=	1;
			//const float walkableRadius	=	2;

			//	Region
			const float regionMinSize	=	8;
			const float regionMergeSize	=	20;

			//	Polygonization :
			const float edgeMaxLen		=	8;
			const float edgeMaxError	=	1.3f;
			const int   vertexPerPoly	=	6;

			// Detail mesh :
			const float detailSampleDistance	=	3;
			const float detailMaxSampleError	=	0.5f;

			cfg.cs						=	cellSize;
			cfg.ch						=	cellHeight;
			cfg.walkableSlopeAngle		=	walkableSlope;
			cfg.walkableHeight			=	(int)Math.Ceiling (walkableHeight / cfg.ch);
			cfg.walkableClimb			=	(int)Math.Floor   (walkableClimb  / cfg.ch);
			cfg.walkableRadius			=	(int)Math.Ceiling (walkableRadius / cfg.cs);
			cfg.maxEdgeLen				=	(int)(edgeMaxLen / cellSize);
			cfg.maxSimplificationError	=	edgeMaxError;
			cfg.minRegionArea			=	(int)Sqr(regionMinSize);		// Note: area = size*size
			cfg.mergeRegionArea			=	(int)Sqr(regionMergeSize);	// Note: area = size*size
			cfg.maxVertsPerPoly			=	vertexPerPoly;
			cfg.detailSampleDist		=	detailSampleDistance < 0.9f ? 0 : cellSize * detailSampleDistance;
			cfg.detailSampleMaxError	=	cellHeight * detailMaxSampleError;
			cfg.bmax					=	bmax;
			cfg.bmin					=	bmin;

			Recast.rcCalcGridSize(cfg.bmin, cfg.bmax, cfg.cs, out cfg.width, out cfg.height);
			#endregion


			// Reset build times gathering.
			ctx.resetTimers();

			// Start the build process.	
			ctx.startTimer(Recast.rcTimerLabel.RC_TIMER_TOTAL);
	
			Log.Message("Building navigation:");
			Log.Message(" - {0} x {1} cells", cfg.width, cfg.height);
			Log.Message(" - {0} verts, {1} tris", nverts, ntris);

			//----------------------------------------------------------
			
			#region Step 2. Rasterize input polygon soup.

			solid	=	new Recast.rcHeightfield();

			Recast.rcCreateHeightfield( ctx, solid, cfg.width, cfg.height, cfg.bmin, cfg.bmax, cfg.cs, cfg.ch );

			var areas = new byte[ ntris ];

			Recast.rcMarkWalkableTriangles( ctx, cfg.walkableSlopeAngle, verts, nverts, inds, ntris, areas );
			Recast.rcRasterizeTriangles( ctx, verts, areas, ntris, solid, cfg.walkableClimb ); 

			#endregion

			//----------------------------------------------------------

			#region Step 3. Filter walkables surfaces.

			Recast.rcFilterLowHangingWalkableObstacles( ctx, cfg.walkableClimb, solid );
			Recast.rcFilterLedgeSpans( ctx, cfg.walkableHeight, cfg.walkableClimb, solid );
			Recast.rcFilterWalkableLowHeightSpans( ctx, cfg.walkableHeight, solid );

			#endregion

			//----------------------------------------------------------

			#region Step 4. Partition walkable surface to simple regions.

			chf	=	new Recast.rcCompactHeightfield();

			Recast.rcBuildCompactHeightfield( ctx, cfg.walkableHeight, cfg.walkableClimb, solid, chf );
			Recast.rcErodeWalkableArea( ctx, cfg.walkableRadius, chf );

			#if false
			Recast.rcMarkConvexPolyArea( ... )
			#endif

			Recast.rcBuildDistanceField( ctx, chf );
			Recast.rcBuildRegions( ctx, chf, cfg.borderSize, cfg.minRegionArea, cfg.mergeRegionArea );

			#endregion

			//----------------------------------------------------------

			#region Step 5. Trace and simplify region contours.

			cset	=	new Recast.rcContourSet();

			Recast.rcBuildContours( ctx, chf, cfg.maxSimplificationError, cfg.maxEdgeLen, cset, (int)Recast.rcBuildContoursFlags.RC_CONTOUR_TESS_WALL_EDGES );

			#endregion

			//----------------------------------------------------------

			#region Step 6. Build polygons mesh from contours.

			pmesh	=	new Recast.rcPolyMesh();
			Recast.rcBuildPolyMesh( ctx, cset, cfg.maxVertsPerPoly, pmesh );

			#endregion

			//----------------------------------------------------------

			#region Step 7. Create detail mesh which allows to access approximate height on each polygon.

			dmesh	=	new Recast.rcPolyMeshDetail();
			Recast.rcBuildPolyMeshDetail( ctx, pmesh, chf, cfg.detailSampleDist, cfg.detailSampleMaxError, dmesh );

			#endregion

			//----------------------------------------------------------

			#region Step 8. Create Detour data from Recast poly mesh.

			var param = new Detour.dtNavMeshCreateParams();
			param.verts				=	pmesh.verts;
			param.vertCount			=	pmesh.nverts;
			param.polys				=	pmesh.polys;
			param.polyAreas			=	pmesh.areas;
			param.polyFlags			=	pmesh.flags;
			param.polyCount			=	pmesh.npolys;
			param.nvp				=	pmesh.nvp;
			param.detailMeshes		=	dmesh.meshes;
			param.detailVerts		=	dmesh.verts;
			param.detailVertsCount	=	dmesh.nverts;
			param.detailTris		=	dmesh.tris;
			param.detailTriCount	=	dmesh.ntris;
			//param.offMeshConVerts	=	m_geom->getOffMeshConnectionVerts();
			//param.offMeshConRad		=	m_geom->getOffMeshConnectionRads();
			//param.offMeshConDir		=	m_geom->getOffMeshConnectionDirs();
			//param.offMeshConAreas	=	m_geom->getOffMeshConnectionAreas();
			//param.offMeshConFlags	=	m_geom->getOffMeshConnectionFlags();
			//param.offMeshConUserID	=	m_geom->getOffMeshConnectionId();
			//param.offMeshConCount	=	m_geom->getOffMeshConnectionCount();
			param.walkableHeight	=	walkableHeight;
			param.walkableRadius	=	walkableRadius;
			param.walkableClimb		=	walkableClimb;
			param.bmin				=	pmesh.bmin.ToArray();
			param.bmax				=	pmesh.bmax.ToArray();
			param.cs				=	cfg.cs;
			param.ch				=	cfg.ch;
			param.buildBvTree		=	true;
		
			if (!Detour.dtCreateNavMeshData( param, out navData )) {
				Log.Error("Could not build Detour navmesh.");
			}

			navMesh	=	new Detour.dtNavMesh();		
		
			uint status;
		
			status = navMesh.init(navData, (int)Detour.dtTileFlags.DT_TILE_FREE_DATA);

			if (Detour.dtStatusFailed(status)) {
				Log.Error("Could not init Detour navmesh.");
			}
		
			navQuery	=	new Detour.dtNavMeshQuery();
			status = navQuery.init(navMesh, 2048);

			if (Detour.dtStatusFailed(status)) {
				Log.Error("Could not init Detour navmesh query");
			}

			#endregion

			#region Detour Test

			var spos	=	pointStart.ToArray();
			var epos	=	pointEnd  .ToArray();
			var extent	=	new Vector3(2,4,2).ToArray();

			filter		=	new Detour.dtQueryFilter();
			filter.setIncludeFlags(0xFFFF);
			filter.setExcludeFlags(0);

			uint refStart = 0;
			uint refEnd   = 0;

			var fnp1 = navQuery.findNearestPoly( spos, extent, filter, ref refStart, ref spos );
			var fnp2 = navQuery.findNearestPoly( epos, extent, filter, ref refEnd,   ref epos );

			Log.Message("{0:X} {1:X} {2} {3}", fnp1, fnp2, refStart, refEnd );

			navQuery.findPath( refStart, refEnd, spos, epos, filter, resultPath, ref resultPathLen, resultPath.Length );

			#endregion
		}



		public void DrawNavMesh( DebugRender dr )
		{
			if (pmesh==null) {
				return;
			}

			var nvp  = pmesh.nvp;
			var cs   = pmesh.cs;
			var ch   = pmesh.ch;
			var orig = pmesh.bmin;
			var poly = new ushort[nvp*2];
			var vi	 = new ushort[3];

			var color= new Color(0,0,64);

			var orig3= new Vector3( orig );

			dr.DrawPoint( pointStart, 1, Color.Green, 3 );
			dr.DrawPoint( pointEnd,   1, Color.Red,   3 );

			for (int i = 0; i < pmesh.npolys; ++i) {

				Array.Copy( pmesh.polys, i*2*nvp, poly, 0, nvp*2 );
				var area = pmesh.areas[ i ];
				
				for (int j = 2; j < nvp; ++j)
				{
					if (poly[j] == Recast.RC_MESH_NULL_IDX) {
						break;
					}

					vi[0] = poly[0];
					vi[1] = poly[j-1];
					vi[2] = poly[j];
					
					for (int k = 0; k < 3; ++k)
					{
						var  v0 = pmesh.verts[vi[k]*3+0];
						var  v1 = pmesh.verts[vi[k]*3+1];
						var  v2 = pmesh.verts[vi[k]*3+2];
						var  x  = orig[0] +  v0*cs;
						var  y  = orig[1] + (v1+1)*ch;
						var  z  = orig[2] +  v2*cs;
						dr.DrawPoint( new Vector3(x,y,z), 0.125f, Color.Orange, 1 );
					}
				}


				for (int j=0; j<nvp; j++) {
				
					if ( poly[j]==Recast.RC_MESH_NULL_IDX ) {
						break;
					}	

					var lineColor = Color.Black;
					var lineWidth = 2;

					//	skip internal edges
					if ( (poly[ nvp+j ] & 0x8000)==0 ) {
						lineWidth = 1;
						lineColor = Color.Black;
					}

					if (resultPath.Contains( (uint)i )) {
						lineColor = Color.Red;
						lineWidth = 3;
					}

					int nj	= (j+1 >= nvp || poly[j+1] == Recast.RC_MESH_NULL_IDX) ? 0 : j+1; 
					int vi0	= poly[j];
					int vi1 = poly[nj];

					var v0	= new Vector3( pmesh.verts[vi0*3+0] * cs, pmesh.verts[vi0*3+1] * ch, pmesh.verts[vi0*3+2] * cs ) + orig3;
					var v1	= new Vector3( pmesh.verts[vi1*3+0] * cs, pmesh.verts[vi1*3+1] * ch, pmesh.verts[vi1*3+2] * cs ) + orig3;

					dr.DrawLine( v0, v1, lineColor, lineColor, lineWidth, lineWidth );

				}

			}
			
		}
	}
}
