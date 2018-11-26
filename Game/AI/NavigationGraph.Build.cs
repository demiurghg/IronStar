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

		bool TestWaypointAgainstGeometry ( Waypoint wp )
		{
			var p = wp.Position + Vector3.Up * settings.WalkableHeight/2;
			return ConvexCastAgainstStatic( capsuleShape, p, p ); 
		}


		bool TestWaypointOverlap ( Waypoint a, Waypoint b )
		{
			if ( Math.Abs( a.ProjectedHeight - b.ProjectedHeight ) < settings.WalkableHeight ) {
				if ( Vector2.Distance( a.ProjectedPoint, b.ProjectedPoint ) < settings.WalkableRadius ) {
					return true;
				}
			}
			return false;
		}


		bool TestWaypointPointOverlap ( Waypoint waypoint, Vector3 point )
		{
			if ( point.Y >= waypoint.ProjectedHeight - float.Epsilon ) {
				
				if ( point.Y <= waypoint.ProjectedHeight + settings.WalkableHeight + float.Epsilon ) {

					if ( Vector2.Distance( waypoint.ProjectedPoint, new Vector2( point.X, point.Z ) ) < settings.WalkableRadius ) {
						return true;
					}
				}				
			}

			return false;
		}


		CylinderShape CreateCylinderShape ( float skin )
		{
			var r = settings.WalkableRadius;
			var h = settings.WalkableHeight;

			return new CylinderShape( h - skin, r - skin );
			
		}

		CapsuleShape CreateCapsuleShape ( float skin )
		{
			var r = settings.WalkableRadius;
			var h = settings.WalkableHeight;

			return new CapsuleShape( h - skin - r*2, r - skin );
			
		}

		public bool ConvexCastAgainstStatic ( ConvexShape shape, Vector3 from, Vector3 to )
		{
			BPVector3			sweep		= MathConverter.Convert( to - from );
			BPAffineTransform	identity	= BPAffineTransform.Identity;
			BPRigidTransform	transform	= new BPRigidTransform( MathConverter.Convert( from ) );

			RayCastResult result;

			return physSpace.ConvexCast( shape, ref transform, ref sweep, out result );
		}


		public List<Waypoint> GetWaypointsInTopologycalRange ( Waypoint[] roots, int range )
		{
			List<Waypoint>  L = new List<Waypoint>();
			Queue<Waypoint> Q = new Queue<Waypoint>();

			waypoints.ForEach( (wp)=> { wp.Discovered = false; wp.BfsDepth = 0; } );

			foreach (var root in roots) {
				if (root==null) {
					continue;
				}
				root.BfsDepth = 0;
				root.Discovered = true;
				Q.Enqueue( root );
			}


			while ( Q.Any() ) {
				var n = Q.Dequeue();

				L.Add( n );

				if ( n.BfsDepth >= range ) continue;

				var ndcs = n.Links.Where( (w)=> !w.Discovered );

				foreach ( var c in ndcs ) {
					c.Discovered = true;
					c.BfsDepth   = n.BfsDepth + 1;
					Q.Enqueue( c );
				}
			}

			return L;
		}

	}
}

