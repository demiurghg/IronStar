using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using System.Diagnostics;

namespace IronStar.AI
{
	class AISystem : StatelessSystem<AIComponent>
	{
		readonly NavSystem nav;
		readonly PhysicsCore physics;
		readonly Aspect aiAspect;
		readonly AIConfig defaultConfig = new AIConfig();

		public AISystem( PhysicsCore physics, NavSystem nav )
		{
			this.nav		=	nav;
			this.physics	=	physics;

			aiAspect	=	new Aspect()
						.Include<AIComponent,Transform>()
						;
		}


		protected override void Process( Entity entity, GameTime gameTime, AIComponent ai )
		{
			var cfg	=	defaultConfig;
			var dt	=	gameTime.ElapsedSec;
			var t	=	entity.GetComponent<Transform>();
			var uc	=	entity.GetComponent<UserCommandComponent>();

			if (ai.ThinkTimer.IsElapsed)
			{
				LookForEnemies( gameTime, entity, ai, cfg );
				SelectTarget( entity, ai, cfg );
				SetAimError( ai, cfg );
				
				for (int i=0; i<10; i++)
				{
					if (InvokeNode( entity, ai, cfg )) break;
				}

				ai.ThinkTimer.SetND( cfg.ThinkTime );
			}

			ai.UpdateTimers( gameTime );

			Attack( dt, entity, ai, t, uc, cfg );
			Move( dt, entity, ai, cfg );
		}

		
		/*-----------------------------------------------------------------------------------------------
		 *	Routing :
		-----------------------------------------------------------------------------------------------*/

		void Move( float dt, Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var route	= ai.Route;

			if (uc==null)
			{
				return;
			}

			if (route!=null)
			{
				var origin	=	t.Position;
				var target	=	Vector3.Zero;
				var factor	=	0f;
				var status	=	route.Follow( origin, out target, out factor );
			
				var rate	=	dt * cfg.RotationRate;

				if (ai.Target==null)
				{
					uc.RotateTo( origin, target, rate, 0 );
					uc.ComputeMoveAndStrafe( origin, target, factor );
				}
				else
				{
					uc.ComputeMoveAndStrafe( origin, target, factor );
				}

				var dir = new Vector3( (float)Math.Cos( -uc.Yaw ), 0, (float)Math.Sin( -uc.Yaw ) );

				dr.DrawPoint( origin, 1, Color.Black, 3 );
				dr.DrawLine( origin, target, Color.Black, Color.Black, 5, 1 );
				dr.DrawLine( origin, origin + dir * 5, Color.Red, Color.Red, 5, 1 );

				for (int i=0; i<route.Count-1; i++)
				{
					dr.DrawLine( route[i], route[i+1], Color.Black, Color.Black, 2, 2 );
				}
			}
			else
			{
				uc.Move		=	0;
				uc.Strafe	=	0;
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Shooting :
		-----------------------------------------------------------------------------------------------*/

		void SetAimError( AIComponent ai, AIConfig cfg )
		{
			ai.PrevAimError	=	ai.NextAimError;
			ai.NextAimError =	MathUtil.Random.UniformRadialDistribution( 0, cfg.Accuracy ) * new Vector3( 1, 0.5f, 1 );
		}

		float AssessTarget( AIComponent ai, Vector3 attackerPos, AITarget target )
		{
			float distance	=	Vector3.Distance( attackerPos, target.LastKnownPosition );
			float curve		=	AIUtils.Falloff( distance, 15 );

			float visFactor		=	target.Visible ? 1 : target.ForgettingTimer.Fraction * 0.5f;
			float stickFactor	=	(ai.Target == target) ? 1.0f : 0.5f;

			return curve * stickFactor * visFactor;
		}


		void SelectTarget( Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var pos		= t.Position;

			ai.Target	=	AIUtils.Select( (tt) => AssessTarget(ai,pos,tt), ai.Targets );
		}


		bool Attack( float dt, Entity e, AIComponent ai, Transform t, UserCommandComponent uc, AIConfig cfg )
		{
			//	clear attack flag :
			uc.Action &= ~UserAction.Attack;

			if (ai.Target==null) return false;

			var dr = e.gs.Game.RenderSystem.RenderWorld.Debug.Async;

			Vector3 aimPoint, attackPov;
			Vector3 aimError = Vector3.Lerp( ai.PrevAimError, ai.NextAimError, 1 - ai.ThinkTimer.Fraction );

			dr.DrawPoint( ai.Target.LastKnownPosition, 1.0f, Color.Red, 3 );

			//	check stuff and try aim :
			if (AIUtils.TryGetPOV(e, out attackPov))
			{
				if (AIUtils.TryGetPOV( ai.Target.Entity, out aimPoint, 0.66f ))
				{
					float rateYaw	=	dt * cfg.RotationRate;
					float ratePitch	=	dt * cfg.RotationRate;
					float distance	=	Vector3.Distance( attackPov, aimPoint );

					aimPoint		=	aimPoint + distance * aimError;

					float error		=	uc.RotateTo( attackPov, aimPoint, rateYaw, ratePitch );

					dr.DrawPoint( aimPoint, 5.0f, Color.Red, 3 );

					if (error<0.1f && ai.Target.Visible && ai.AllowFire)
					{
						uc.Action |= UserAction.Attack;
						return true;
					}
				}	 
			}

			return false;
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Target management :
		-----------------------------------------------------------------------------------------------*/

		Aspect enemyAspect = new Aspect().Include<Transform,HealthComponent>().Any<PlayerComponent,AIComponent>();

		void LookForEnemies( GameTime gameTime, Entity entity, AIComponent ai, AIConfig cfg )
		{
			if (IronStar.IsNoTarget) return;

			Vector3 pov;
			BoundingFrustum frustum;
			BoundingSphere sphere;

			var dr = entity.gs.Game.RenderSystem.RenderWorld.Debug.Async;

			AIUtils.ForgetTargetsAndResetVisibility( gameTime, ai ); 

			bool visibility = false;

			if (AIUtils.GetSensoricBoundingVolumes(entity, cfg, out pov, out frustum, out sphere))
			{
				foreach ( var enemy in entity.gs.QueryEntities( enemyAspect ) )
				{
					var health		=	enemy.GetComponent<HealthComponent>();
					var transform	=	enemy.GetComponent<Transform>();
					var isEnemies	=	AIUtils.IsEnemies( entity, enemy );
					var hasLOS		=	entity.HasLineOfSight( enemy, ref frustum, ref sphere, cfg.VisibilityRange );
					var isAlive		=	health.Health > 0;

					if (isEnemies && hasLOS && isAlive)
					{
						visibility = true;
						AIUtils.SpotTarget( ai, cfg, enemy );
					}
				}

				var color	=	visibility ? Color.Red : Color.Lime;
				dr.DrawFrustum( frustum, color, 0.02f, 2 );
				dr.DrawRing( Matrix.Translation(sphere.Center), sphere.Radius, color, 32, 2, 1 );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Decision making utils :
		-----------------------------------------------------------------------------------------------*/

		bool InvokeNode( Entity e, AIComponent ai, AIConfig cfg )
		{
			switch (ai.DMNode)
			{
				case DMNode.Dead:			return true;
				case DMNode.Stand:			return NodeStand( e, ai, cfg );
				case DMNode.StandGaping:	return NodeStandGaping( e, ai, cfg );
				case DMNode.Roaming:		return NodeRoaming( e, ai, cfg );
				case DMNode.CombatChase:	return NodeCombatChase( e, ai, cfg );
				case DMNode.CombatAttack:	return NodeCombatAttack( e, ai, cfg );
				case DMNode.CombatMove:		return NodeCombatMove( e, ai, cfg );
			}

			return false;
		}

		void EnterNode( DMNode newNode, AIComponent ai, string reason )
		{
			var oldNode		=	ai.DMNode;
			ai.DMNode		=	newNode;	//	set new node
			ai.AllowFire	=	true;		//	reset fire prevention

			Log.Debug("{0} -> {1} : {2}", oldNode, newNode, reason );
		}
					
		/*-----------------------------------------------------------------------------------------------
		 *	Decision making nodes :
		-----------------------------------------------------------------------------------------------*/

		void EnterStand( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			ai.StandTimer.SetND( cfg.IdleTimeout );
			EnterNode( DMNode.Stand, ai, reason );
		}


		bool NodeStand( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();

			if (ai.Target!=null)
			{
				EnterStandGaping( e, ai, cfg, "WTF?" );
				return false;
			}

			if (ai.StandTimer.IsElapsed)
			{
				EnterRoaming( e, ai, cfg, "stand timeout");
				return false;
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterRoaming( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			var transform	=	e.GetComponent<Transform>();
			var location	=	nav.GetReachablePointInRadius( transform.Position, cfg.RoamRadius );
			ai.Route		=	nav.FindRoute( transform.Position, location );

			EnterNode( DMNode.Roaming, ai, reason );
		}


		bool NodeRoaming( Entity e, AIComponent ai, AIConfig cfg )
		{
			if (ai.Target!=null)
			{
				EnterStandGaping( e, ai, cfg, "WTF?" );
				return false;
			}

			if (ai.Route==null)
			{
				EnterStand( e, ai, cfg, "no route");
				return false;
			}
			else if (ai.Route.Status!=Status.InProgress)
			{
				EnterStand( e, ai, cfg, ai.Route.Status==Status.Success ? "roam point is reached" : "routing failure");
				return false;
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterStandGaping( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			ai.GapeTimer.SetND( cfg.GapeTimeout );
			EnterNode( DMNode.StandGaping, ai, reason );

			ai.AllowFire	=	false;	//	we
			ai.Route		=	null;
		}

		bool NodeStandGaping( Entity e, AIComponent ai, AIConfig cfg )
		{
			if (ai.GapeTimer.IsElapsed)
			{
				if (ai.Target!=null && ai.Target.Visible)
				{
					EnterCombatChase( e, ai, cfg, "target detected" );
					return false;
				}

				EnterStand( e, ai, cfg, "false alarm");
				return false;
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterCombatChase( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			Trace.Assert( ai.Target!=null );

			var origin	=	e.GetComponent<Transform>().Position;

			ai.Route	=	nav.FindRoute( origin, ai.Target.LastKnownPosition );

			if (ai.Route==null)
			{
				var randPoint	=	nav.GetReachablePointInRadius( origin, cfg.RoamRadius );
				ai.Route		=	nav.FindRoute( origin, randPoint );
			}

			EnterNode(DMNode.CombatChase, ai, reason);
		}


		bool NodeCombatChase( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();

			if (ai.Target!=null)
			{
				if (ai.Target.Visible)
				{
					EnterCombatAttack( e, ai, cfg, "target is visible" );
					return false;
				}
			}
			else
			{
				EnterStand( e, ai, cfg, "target lost" );
				return false;
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterCombatAttack( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			EnterNode( DMNode.CombatAttack, ai, reason );
			ai.Route = null; // stop moving
			ai.AttackTimer.SetND( cfg.AttackTime );
		}


		bool NodeCombatAttack( Entity e, AIComponent ai, AIConfig cfg )
		{
			if (ai.Target==null)
			{
				EnterStand( e, ai, cfg, "target lost or destroyed");
				return false;
			}
			else
			{
				if (!ai.Target.Visible)
				{
					EnterCombatChase( e, ai, cfg, "target lost LOS");
					return false;
				}

				if (ai.AttackTimer.IsElapsed)
				{
					EnterCombatMove( e, ai, cfg, "attack is timed out");
					return false;
				}
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterCombatMove( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			var origin	=	e.GetComponent<Transform>().Position;
			var dst		=	nav.GetReachablePointInRadius( origin, cfg.CombatMoveRadius );
			ai.Route		=	nav.FindRoute( origin, dst );
			EnterNode( DMNode.CombatMove, ai, reason );

			ai.AllowFire	=	AIUtils.RollTheDice( cfg.AttackWhileMoving );
		}


		bool NodeCombatMove( Entity e, AIComponent ai, AIConfig cfg ) 
		{
			if ( ai.Route==null || ai.Route.Status!=Status.InProgress)
			{
				EnterCombatAttack( e, ai, cfg, "combat move is completed");
				return false;
			}

			if ( ai.Target==null )
			{
				EnterStand( e, ai, cfg, "target is lost" );
				return false;
			}
			else
			{
				if (!ai.Target.Visible)
				{
					ai.AllowFire = false;
				}
			}

			return true;
		}
	}
}
