using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.Mathematics;

namespace IronStar.AI
{
	public class Route
	{
		public float acceptanceRadius	=	1;
		public float failureRadius	=	7;
		public float leadingDistance	=	3;
		
		int stuckCounter = 0;
		Vector3 lastOrigin;
		float stuckThreshold = 0.0001f;

		readonly Vector3[] route;

		public int Count { get { return route.Length; } }

		public Vector3 this[int index] { get { return route[index]; } }

		public Status Status { get { return lastStatus; } }
		Status lastStatus = Status.InProgress;

		
		public Route( IEnumerable<Vector3> waypoints )
		{
			route	=	waypoints.ToArray();
		}


		public Status Follow( Vector3 origin, out Vector3 target, out float factor )
		{ 
			lastStatus	=	FollowInternal( origin, out target, out factor );
			return lastStatus;
		}


		Status FollowInternal( Vector3 origin, out Vector3 target, out float factor )
		{
			factor = 0f;
			target = origin;

			var fraction = 0f;
			var distance = 0f;
			int lastSegmentIndex = route.Length-2;

			/*if (Vector3.Distance(origin, lastOrigin) < stuckThreshold)
			{
				stuckCounter++;
				if (stuckCounter>5)
				{
					return Status.Failure;
				}
			}
			else
			{
				stuckCounter = 0;
			}*/

			lastOrigin	=	origin;

			if (route.Length<2) 
			{
				target		=	route[0];
				distance	=	Vector3.Distance( target, origin );

				if (distance <= acceptanceRadius) return Status.Success;
				if (distance >= failureRadius) return Status.Failure;
				return Status.InProgress;
			}
			else
			{
				int segmentIndex = GetClosestSegment( route, origin, out distance, out fraction );
				var a	= route[segmentIndex];
				var b	= route[segmentIndex+1];
				var ab	= b - a;
				var p	= Vector3.Lerp( a, b, fraction );

				if (segmentIndex==lastSegmentIndex)
				{
					target	=	b;
				}
				else
				{
					target = p + ab.Normalized() * leadingDistance;
				}

				factor	=	MathUtil.Clamp( ProjectedDistance(origin, target) / leadingDistance, 0, 1 );

				var distanceToTarget = ProjectedDistance( route.Last(), origin );

				if (distanceToTarget <= acceptanceRadius) return Status.Success;
				if (distance >= failureRadius) return Status.Failure;
				return Status.InProgress;
			}
		}


		float ProjectedDistance( Vector3 a, Vector3 b )
		{
			return Vector2.Distance( new Vector2(a.X, a.Z), new Vector2(b.X, b.Z) );
		}


		int GetClosestSegment( Vector3[] route, Vector3 origin, out float distance, out float fraction )
		{
			var closestSegment = 0;
			distance = float.MaxValue;
			fraction = 0;

			for (int i=route.Length-2; i>=0; i--)
			{
				float d, t;
				var a = route[i];
				var b = route[i+1];
				Intersection.DistancePointToLineSegment( a, b, origin, out d, out t );

				if ( d < distance )
				{
					closestSegment	= i;
					distance		= d;
					fraction		= t;
				}
			}

			return closestSegment;
		}
	}
}
