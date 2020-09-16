using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.BTCore.Actions;
using IronStar.BTCore.Decorators;
using IronStar.AI.Actions;
using IronStar.Gameplay;
using IronStar.Mathematics;

namespace IronStar.AI
{
	class MovementSystem : StatelessSystem<Transform,MovementComponent,UserCommandComponent>
	{
		public bool Enabled = true;
		readonly PhysicsCore physics;

		
		public MovementSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}

		
		protected override void Process( Entity entity, GameTime gameTime, Transform transform, MovementComponent movement, UserCommandComponent command )
		{
			Vector3 targetPoint;

			if (movement.Route!=null && movement.RoutingStatus==BTStatus.InProgress)
			{
				movement.RoutingStatus = FollowRoute( movement.Route, transform.Position, 1.0f, 10.0f, 3.0f, out targetPoint );
			}
		}


		BTStatus FollowRoute( NavigationRoute route, Vector3 origin, float acceptanceRadius, float failureRadius, float leadingDistance, out Vector3 target )
		{
			float fraction = 0f;
			float distance = 0f;

			if (route.Count<2) 
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


		int GetClosestSegment( NavigationRoute route, Vector3 origin, out float distance, out float fraction )
		{
			var closestSegment = 0;
			distance = float.MaxValue;
			fraction = 0;

			for (int i=route.Count-2; i>=0; i--)
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
