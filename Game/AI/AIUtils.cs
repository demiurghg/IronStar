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
using IronStar.Gameplay.Components;
using IronStar.SFX;

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


		public static bool RollTheDice( float probability )
		{
			return MathUtil.Random.NextFloat(0, 1) <= probability;
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


		public static bool HasLineOfSight( this Entity attacker, Entity target, ref BoundingFrustum frustum, float visibilityRange, float hearingRange )
		{
			if (attacker==null || target==null) 
			{
				return false;
			}

			Vector3 from;
			Vector3 to;

			if (attacker.TryGetPOV(out from) && target.TryGetPOV(out to))
			{
				var distance = Vector3.Distance(from,to);

				//	audial detection :
				if (distance < hearingRange)
				{
					if (attacker.gs.GetService<PhysicsCore>().HasLineOfSight( from, to, attacker, target ))
					{
						return true;
					}
				}

				//	visual detection :
				if (distance < visibilityRange)
				{
					if (frustum.Contains(ref to)==ContainmentType.Contains)
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


		public static void ForgetTargetsAndResetVisibility( int msec, AIComponent ai )
		{
			foreach ( var target in ai.Targets )
			{
				target.ForgettingTimer.Update( msec );
				target.Visible = false; // assume, SpotTarget will update visibility flag
			}

			ai.Targets.RemoveAll( tt => tt.ForgettingTimer.IsElapsed || !IsAlive(tt.Entity) );
		}


		static bool IsAlive( Entity a )
		{
			var health = a?.GetComponent<HealthComponent>();
			return health==null ? true : health.Health > 0;
		}


		public static bool IsEnemies( Entity a, Entity b )
		{
			var teamA = a.GetComponent<TeamComponent>();
			var teamB = b.GetComponent<TeamComponent>();

			if (teamA==null || teamB==null)
			{
				return false;
			}

			if (teamA.Team!=teamB.Team)
			{
				return true;
			}

			return false;
		}
	}
}
