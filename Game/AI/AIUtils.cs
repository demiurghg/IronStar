using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Mathematics;
using System.Diagnostics;

namespace IronStar.AI
{
	public enum Status { Failure, Success, InProgress }

	public static class AIUtils
	{
		/*-----------------------------------------------------------------------------------------
		 *	Fuzzy logic
		-----------------------------------------------------------------------------------------*/

		public static T Select<T>( Func<T,float> weight, IEnumerable<T> options )
		{
			unsafe
			{
				var sumw = stackalloc float[ options.Count() ];
				var sum = 0f;

				for (int i=0; i<options.Count(); i++)
				{
					var w	=	weight( options.ElementAt(i) );
					sum		+=	w;
					sumw[i]	=	sum + 1f/8192.0f;
				}

				float selector = MathUtil.Random.NextFloat(0, sum);

				for (int i=0; i<options.Count(); i++)
				{
					if (selector<=sumw[i])
					{
						return options.ElementAt(i);
					}
				}
			}

			return options.FirstOrDefault();
		}

		
		public static float Falloff( float distance, float radius )
		{
			return radius * radius / ( distance * distance + radius * radius );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Visibility utils :
		-----------------------------------------------------------------------------------------*/

		public static bool GetSensoricBoundingVolumes( Entity npcEntity, AIConfig cfg, out Vector3 pov, out BoundingFrustum frustum, out BoundingSphere sphere )
		{
			frustum	=	new BoundingFrustum();
			sphere	=	new BoundingSphere();
			pov		=	new Vector3();

			var cc			=	npcEntity.GetComponent<CharacterController>();
			var transform	=	npcEntity.GetComponent<Transform>();
			var uc			=	npcEntity.GetComponent<UserCommandComponent>();

			if (cc==null || transform==null || transform==null || uc==null )
			{
				return false;
			}

			var head		=	GameUtil.ComputePovTransform( uc, transform, cc, null );
			var view		=	Matrix.Invert( head );

			var proj		=	Matrix.PerspectiveFovRH( cfg.VisibilityFov, 2, 0.01f, cfg.VisibilityRange );

			pov				=	head.TranslationVector;
			frustum			=	new BoundingFrustum( view * proj );
			sphere			=	new BoundingSphere( transform.Position, cfg.HearingRange );

			return true;
		}


		public static bool TryGetPOV( this Entity entity, out Vector3 pov, float factor = 1.0f )
		{
			var transform	=	entity.GetComponent<Transform>();
			var controller	=	entity.GetComponent<CharacterController>();
			
			pov	=	Vector3.Zero;
			
			if (transform!=null) 
			{
				if (controller!=null) 
				{
					pov	=	transform.Position + controller.PovOffset * factor;
				} 
				else
				{
					pov	=	transform.Position;
				}

				return true;
			}

			return false;
		}


		public static bool HasLineOfSight( this Entity attacker, Entity target, ref BoundingFrustum frustum, ref BoundingSphere sphere, float maxDistance = float.MaxValue )
		{
			if (attacker==null || target==null) 
			{
				return false;
			}

			Vector3 from;
			Vector3 to;

			if (attacker.TryGetPOV(out from) && target.TryGetPOV(out to))
			{
				if (Vector3.Distance(from,to) <= maxDistance)
				{
					if ((sphere.Contains(ref to)==ContainmentType.Contains) || (frustum.Contains(ref to)==ContainmentType.Contains))
					{
						if (attacker.gs.GetService<PhysicsCore>().HasLineOfSight( from, to, attacker, target ))
						{
							return true;
						}
					}
				}
			}

			return false;
		}


		public static void SpotTarget( AIComponent ai, AIConfig cfg, Entity enemy )
		{
			var enemyPosition = enemy.GetComponent<Transform>().Position;

			foreach ( var target in ai.Targets )
			{
				if (target.Entity==enemy)
				{
					target.ForgettingTimer.Set( cfg.TimeToForget );
					target.LastKnownPosition = enemyPosition;
					target.Visible = true;
					return;
				}
			}

			ai.Targets.Add( new AITarget( enemy, enemyPosition, cfg.TimeToForget ) );
		}


		public static void ForgetTargetsAndResetVisibility( GameTime gameTime, AIComponent ai )
		{
			foreach ( var target in ai.Targets )
			{
				target.ForgettingTimer.Update( gameTime );
				target.Visible = false; // assume, SpotTarget will update visibility flag
			}

			ai.Targets.RemoveAll( tt => tt.ForgettingTimer.IsElapsed );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Navigation utils :
		-----------------------------------------------------------------------------------------*/

		public static Status FollowRoute( Vector3[] route, Vector3 origin, float acceptanceRadius, float failureRadius, float leadingDistance, out Vector3 target, out float velocity )
		{
			float fraction = 0f;
			float distance = 0f;
			velocity = 0f;
			int lastSegmentIndex = route.Length-2;

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

				velocity	=	MathUtil.Clamp( ProjectedDistance(origin, target) / leadingDistance, 0, 1 );

				var distanceToTarget = ProjectedDistance( route.Last(), origin );

				if (distanceToTarget <= acceptanceRadius) return Status.Success;
				if (distance >= failureRadius) return Status.Failure;
				return Status.InProgress;
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
