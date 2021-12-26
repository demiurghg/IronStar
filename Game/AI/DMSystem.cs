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

namespace IronStar.AI
{
	class DMSystem : StatelessSystem<AIComponent>
	{
		readonly NavSystem nav;
		readonly PhysicsCore physics;
		readonly Aspect aiAspect;
		readonly AIConfig defaultConfig = new AIConfig();

		public DMSystem( PhysicsCore physics, NavSystem nav )
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

			ai.ThinkTimer.Update( gameTime );
			ai.IdleTimer.Update( gameTime );

			Attack( dt, entity, ai, t, uc, cfg );
			//Move( dt, entity, ai, cfg );
		}

		
		bool InvokeNode( Entity e, AIComponent ai, AIConfig cfg )
		{
			switch (ai.DMNode)
			{
				case DMNode.Idle:		return Idle( e, ai, cfg );
				case DMNode.Roaming:	return Roaming( e, ai, cfg );
			}

			return true;
		}

		
		void Enter( AIComponent ai, DMNode newNode, string reason = "" )
		{
			var oldNode	=	ai.DMNode;
			ai.DMNode	=	newNode;

			Log.Debug("{0} -> {1} : {2}", oldNode, newNode, reason );
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Routing :
		-----------------------------------------------------------------------------------------------*/

		void Move( float dt, Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var route	= ai.ActiveRoute;

			if (uc==null)
			{
				return;
			}

			if (ai.ActiveRoute!=null)
			{
				var origin	=	t.Position;
				var target	=	Vector3.Zero;
				var factor	=	0f;
				var status	=	AIUtils.FollowRoute( route, origin, 10, 7, 3, out target, out factor );
			
				if (status==Status.Failure) ai.Goal.Failed = true;
				if (status==Status.Success) ai.Goal.Succeed = true;

				var rate	=	dt * cfg.RotationRate;

				uc.RotateTo( origin, target, rate, 0 );
				uc.Move = factor * 0.9f;

				dr.DrawPoint( origin, 1, Color.Black, 3 );
				dr.DrawLine( origin, target, Color.Black, Color.Black, 5, 1 );

				for (int i=0; i<route.Length-1; i++)
				{
					dr.DrawLine( route[i], route[i+1], Color.Black, Color.Black, 2, 2 );
				}
			}
			else
			{
				uc.Move	=	0;
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

		float AssessTarget( Vector3 attackerPos, AITarget target )
		{
			float distance	=	Vector3.Distance( attackerPos, target.LastKnownPosition );
			float curve		=	AIUtils.Falloff( distance, 15 );

			return target.Visible ? curve : curve * target.ForgettingTimer.Fraction;
		}


		void SelectTarget( Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var pos		= t.Position;

			ai.Target	=	AIUtils.Select( (tt) => AssessTarget(pos,tt), ai.Targets );
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

					var error		=	uc.RotateTo( attackPov, aimPoint, rateYaw, ratePitch );

					dr.DrawPoint( aimPoint, 5.0f, Color.Red, 3 );

					if (error<0.1f && ai.Target.Visible)
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

		Aspect enemyAspect = new Aspect().Include<Transform,PlayerComponent,HealthComponent>();

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

					if (entity.HasLineOfSight( enemy, ref frustum, ref sphere, cfg.VisibilityRange ))
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
		 *	Decision making nodes :
		-----------------------------------------------------------------------------------------------*/

		bool Idle( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();

			if (ai.IdleTimer.IsElapsed)
			{
				var location	=	nav.GetReachablePointInRadius( transform.Position, cfg.RoamRadius );
				var route		=	nav.FindRoute( transform.Position, location );
				var timeout		=	20000;

				ai.Goal			=	new AIGoal( location, timeout );
				ai.ActiveRoute	=	route;

				Enter( ai, DMNode.Roaming, "idle timeout");
			}

			return true;
		}


		bool Roaming( Entity e, AIComponent ai, AIConfig cfg )
		{
			if (ai.Goal.Failed || ai.Goal.Succeed)
			{
				ai.IdleTimer	=	new Timer(cfg.IdleTimeout);
				ai.ActiveRoute	=	null;
				Enter( ai, DMNode.Idle, ai.Goal.Succeed ? "roam point reached" : "failed to reach roam point" );
			}

			return true;
		}
	}
}
