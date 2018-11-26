using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics;
using BEPUphysics.BroadPhaseEntries;
using Native.Embree;
using BEPUphysics;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BPAffineTransform = BEPUutilities.AffineTransform;
using BPBoundingBox = BEPUutilities.BoundingBox;
using BPVector3 = BEPUutilities.Vector3;
using BPVector2 = BEPUutilities.Vector2;
using BPRigidTransform = BEPUutilities.RigidTransform;
using Fusion;

namespace IronStar.AI {

	public partial class NavigationGraph {

		List<Waypoint> waypoints = new List<Waypoint>();
		readonly NavigationSettings settings;
		readonly Space physSpace;
		readonly CapsuleShape  capsuleShape;
		readonly CylinderShape cylinderShape;


		/// <summary>
		/// Builds instance of navigation graph with given settings and mesh.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="vertices"></param>
		/// <param name="indices"></param>
		public NavigationGraph ( NavigationSettings settings, Vector3[] vertices, int[] indices )
		{
			this.settings	=	settings;

			capsuleShape	=	CreateCapsuleShape(0);
			cylinderShape	=	CreateCylinderShape(0);

			using ( var rtc = new Rtc() ) {

				using ( var rtcScene = new RtcScene( rtc, SceneFlags.Incoherent, AlgorithmFlags.Intersect1 ) ) {

					Log.Message("...creating RTC scene");
					BuildRtcScene( rtcScene, vertices, indices );

					Log.Message("...creating physical space");
					physSpace = BuildPhysSpace( vertices, indices );

					Log.Message("...rasterizing scene");
					RasterizeMesh( vertices, indices );

					Log.Message("...removing overlaping waypoints");
					RemoveOverlappingWaypoints();

					Log.Message("...removing scene intersections");
					RemoveSceneIntersections();

					Log.Message("...establish primary links");
					EstablishPrimaryLinks();

					Log.Message("...compute waypoint importance");
					ComputeWaypointImportance();

					Log.Message("...collapse waypoints");
					CollapseWaypoints();
				}

			}
		}


		/// <summary>
		/// Draw graph for debug purposes.
		/// </summary>
		/// <param name="dr"></param>
		public void DrawGraphDebug ( DebugRender dr )
		{
			foreach ( var wp in waypoints ) {

				var color = Color.Lerp( Color.Navy, Color.Yellow, wp.Importance );

				dr.DrawWaypoint( wp.Position + Vector3.Up * 0.125f, 0.25f, color, 2 );

				if ( wp.Links!=null ) {
					foreach ( var link in wp.Links ) {
						dr.DrawLine( wp.Position, link.Position, Color.Black );
					}
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Internal preprocessing stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		void BuildRtcScene ( RtcScene scene, Vector3[] vertices, int[] indices )
		{
			var id		=	scene.NewTriangleMesh( GeometryFlags.Static, indices.Length/3, vertices.Length );

			scene.UpdateGeometryBuffer( id, BufferType.VertexBuffer, vertices );
			scene.UpdateGeometryBuffer( id, BufferType.IndexBuffer,  indices );
		}


		Space BuildPhysSpace ( Vector3[] vertices, int[] indices )
		{
			var bpVerts = vertices.Select( v => MathConverter.Convert(v) ).ToArray();
			var bpInds  = indices;

			var space = new Space();
			var mesh  = new StaticMesh( bpVerts, bpInds ) ;

			space.Add( mesh );

			return space;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertices"></param>
		/// <param name="indices"></param>
		void RasterizeMesh ( Vector3[] vertices, int[] indices )
		{
			var trisNum		=	indices.Length / 3;
			var climb		=	settings.WalkableClimb;
			//var minSlope	=	(float)Math.Cos( Math.PI -  settings.WalkableAngle);
			var minSlope	=	(float)Math.Cos( settings.WalkableAngle );
			var up			=	Vector3.Up;

			for ( int i=0; i<trisNum; i++ ) {
				
				var p0		=	vertices[ indices[i*3+0] ];
				var p1		=	vertices[ indices[i*3+1] ];
				var p2		=	vertices[ indices[i*3+2] ];

				var v01		=	(p1 - p0).Normalized();
				var v02		=	(p2 - p0).Normalized();

				var n		=	Vector3.Cross( v01, v02 ).Normalized();

				var slope	=	Vector3.Dot( n, Vector3.Up );

				var offset	=	up * climb;

				var walk	=	slope > minSlope;

				var step	=	settings.VoxelStep;

				if (walk) {
					Voxelizer.RasterizeTriangle( p0, p1, p2, step, (p) => waypoints.Add( new Waypoint(p,n,walk) ) );
				}
			}
		}


		void RemoveOverlappingWaypoints ()
		{
			var kdTree = new KdTree2<Waypoint>();

			foreach ( var wp in waypoints ) {
				kdTree.Add( wp.ProjectedPoint, wp );
			}

			foreach ( var wp in waypoints ) {

				KdTree2<Waypoint>.SearchCallback callback = delegate (Waypoint w, Vector2 p, float d) {
					if (w==wp || !w.Walkable) {
						return false;
					}
					if (TestWaypointPointOverlap(wp,w.Position)) {
						wp.Walkable = false;
						return true;
					}
					return false;
				};

				kdTree.NearestRadius( 
					wp.ProjectedPoint, 
					settings.VoxelStep/2, 
					callback );
			}

			waypoints.RemoveAll( wp => !wp.Walkable );
		}


		void RemoveSceneIntersections ()
		{
			var height	= settings.WalkableHeight;
			var climb	= settings.WalkableClimb;

			var offset1	= Vector3.Up * (height/2 + climb);
			var offset2	= Vector3.Up * (height/2);

			foreach ( var wp in waypoints ) {

				var pos1	=	wp.Position + offset1;
				var pos2	=	wp.Position + offset2;

				if ( ConvexCastAgainstStatic( capsuleShape, pos1, pos1 ) ) {
					if ( ConvexCastAgainstStatic( capsuleShape, pos2, pos2 ) ) {
						wp.Walkable = false;
					}
				}
			}

			waypoints.RemoveAll( wp => !wp.Walkable );
		}



		void EstablishPrimaryLinks ()
		{
			var kdTree	= new KdTree2<Waypoint>();

			var radius	= settings.VoxelStep * 1.42f; // slightly more than sqrt(2);

			foreach ( var wp in waypoints ) {
				kdTree.Add( wp.ProjectedPoint, wp );
			}

			foreach ( var wp in waypoints ) {
				
				var links = kdTree.NearestRadius( wp.ProjectedPoint, radius, 
					(otherWp) => (wp!=otherWp) && Math.Abs( wp.ProjectedHeight - otherWp.ProjectedHeight ) <= settings.WalkableClimb 
				);

				wp.Links = links;
			}

			waypoints.RemoveAll( wp => wp.Links==null || !wp.Links.Any() );
		}


		void ComputeWaypointImportance ()
		{
			var kdTree	= new KdTree2<Waypoint>();

			var rand = new Random();

			var radius	= settings.VoxelStep * 1.42f; // slightly more than sqrt(2);

			foreach ( var wp in waypoints ) {
				kdTree.Add( wp.ProjectedPoint, wp );
			}

			foreach ( var wp in waypoints ) {
				if ( wp.Links.Length <= 4 || wp.Links.Length==6 ) {
					wp.Importance = 1;
				}

				if ( wp.Links.Length == 5 ) {
					wp.Importance = 0.5f;
				}

				if ( wp.Links.Count( wp1 => wp1.Links.Length==5 )==6 ) {
					wp.Importance = 1;
				}
			}

			for ( int i=0; i<64; i++) {

				foreach ( var wp in waypoints ) {

					float count = wp.Links.Length + 1;

					if (wp.Importance<1) {
						wp.Importance = (wp.Importance + wp.Links.Sum( w => w.Importance )) / count + rand.NextFloat(0, 0.001f);
					}
				}
			}

			waypoints = waypoints.OrderBy( w => w.Importance ).ToList();
		}



		void CollapseWaypoints ()
		{
			float radius = 5;

			foreach ( var wp in waypoints ) {

				if (wp.Walkable) {
					var closestWps = GetWaypointsInTopologycalRange( new[] {wp}, 4 )
						.Where( wp1 => wp1.Walkable && wp1 != wp )
						.Where( wp2 => wp2.Importance < wp.Importance )
						.Where( wp3 => wp3.Importance < 0.9f )
						.ToList();

					if (closestWps.Count>1) {
						wp.Importance = 1;
					}

					closestWps.ForEach( wp3 => wp3.Walkable = false );
				}
			}

			waypoints.RemoveAll( wp4 => !wp4.Walkable );

			waypoints.ForEach( wp5 => wp5.Links = null );
		}
	}
}

