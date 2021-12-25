using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.Mathematics;
using Fusion;
using Native.NRecast;

namespace IronStar.AI
{
	public static class NavRouter
	{
		public static BTStatus FollowRoute( Vector3[] route, Vector3 origin, float acceptanceRadius, float failureRadius, float leadingDistance, out Vector3 target, out float velocity )
		{
			float fraction = 0f;
			float distance = 0f;
			velocity = 0f;
			int lastSegmentIndex = route.Length-2;

			if (route.Length<2) 
			{
				target		=	route[0];
				distance	=	Vector3.Distance( target, origin );

				if (distance <= acceptanceRadius) return BTStatus.Success;
				if (distance >= failureRadius) return BTStatus.Failure;
				return BTStatus.InProgress;
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

				velocity	=	MathUtil.Clamp( ProjectedDistance(origin, target) / leadingDistance, 0, 1 );

				var distanceToTarget = ProjectedDistance( route.Last(), origin );

				if (distanceToTarget <= acceptanceRadius) return BTStatus.Success;
				if (distance >= failureRadius) return BTStatus.Failure;
				return BTStatus.InProgress;
			}
		}


		static float ProjectedDistance( Vector3 a, Vector3 b )
		{
			return Vector2.Distance( new Vector2(a.X, a.Z), new Vector2(b.X, b.Z) );
		}


		static int GetClosestSegment( Vector3[] route, Vector3 origin, out float distance, out float fraction )
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
