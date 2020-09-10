using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.Mathematics;
using Fusion;

namespace IronStar.AI
{
	public static class NavigationRouter
	{
		public static BTStatus FollowRoute( Vector3[] route, Vector3 origin, float acceptanceRadius, float failureRadius, float leadingDistance, out Vector3 target )
		{
			float fraction = 0f;
			float distance = 0f;

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
				target = p + ab.Normalized() * leadingDistance;

				var distanceToTarget = Vector3.Distance( route.Last(), origin );

				if (distanceToTarget <= acceptanceRadius) return BTStatus.Success;
				if (distance >= failureRadius) return BTStatus.Failure;
				return BTStatus.InProgress;
			}
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
