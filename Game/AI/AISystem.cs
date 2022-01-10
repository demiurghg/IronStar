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
using Fusion.Core.Configuration;
using IronStar.SFX;

namespace IronStar.AI
{
	[ConfigClass]
	class AISystem : StatelessSystem<AIComponent>
	{
		[Config] public static bool DrawNavigation { get; set; } = false;
		[Config] public static bool DrawVisibility { get; set; } = false;
		[Config] public static bool LogDM { get; set; } = false;
		[Config] public static Difficulty Difficulty { get; set; } = Difficulty.Medium;
		[Config] public static bool SkipAttackDelay { get; set; } = false;

		readonly NavSystem nav;
		readonly PhysicsCore physics;
		readonly Aspect aiAspect;
		readonly AIConfig defaultConfig = new AIConfig();
		readonly AITokenPool tokenPool;

		public AISystem( PhysicsCore physics, NavSystem nav )
		{
			this.nav		=	nav;
			this.physics	=	physics;

			aiAspect	=	new Aspect()
						.Include<AIComponent,Transform>()
						;

			tokenPool = new AITokenPool( DifficultyUtils.GetTokenCount(Difficulty), DifficultyUtils.GetTokenTimeout(Difficulty) );
		}


		public override void Add( IGameState gs, Entity e )
		{
			base.Add( gs, e );

			var transform	=	e.GetComponent<Transform>();
			var ucc			=	e.GetComponent<UserCommandComponent>();

			if (transform!=null & ucc!=null)
			{
				var uc		=	UserCommand.FromTransform( transform );
				ucc.UpdateFromUserCommand( uc.Yaw, uc.Pitch, uc.Move, uc.Strafe, uc.Action );
			}
		}


		public override void Update( IGameState gs, GameTime gameTime )
		{
			base.Update( gs, gameTime );

			tokenPool.Update( gameTime );
		}


		protected override void Process( Entity entity, GameTime gameTime, AIComponent ai )
		{
			var cfg	=	defaultConfig;
			var dt	=	gameTime.ElapsedSec;
			var t	=	entity.GetComponent<Transform>();
			var uc	=	entity.GetComponent<UserCommandComponent>();

			if (entity.gs.Paused)
			{
				return;
			}

			if (ai.ThinkTimer.IsElapsed)
			{
				LookForEnemies( gameTime, entity, ai, cfg );
				DetectAttacker( gameTime, entity, ai, cfg );
				SelectTarget( entity, ai, cfg );
				SetAimError( ai, cfg );
				
				for (int i=0; i<10; i++)
				{
					if (InvokeNode( entity, ai, cfg )) break;
				}

				ai.ThinkTimer.SetND( cfg.ThinkTime );
			}

			ai.UpdateTimers( gameTime );


			var stun = Stun( dt, entity, ai, cfg );
			//	only prevent fire on stun
			Attack( dt, entity, ai, t, uc, cfg, false );
			Move( dt, entity, ai, cfg, false );
		}

		
		/*-----------------------------------------------------------------------------------------------
		 *	Movement :
		-----------------------------------------------------------------------------------------------*/

		void Move( float dt, Entity e, AIComponent ai, AIConfig cfg, bool stun )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var route	= ai.Route;

			if (uc==null)
			{
				return;
			}

			if (route!=null && !stun)
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

				if (DrawNavigation)
				{
					dr.DrawPoint( origin, 1, Color.Black, 3 );
					dr.DrawLine( origin, target, Color.Black, Color.Black, 5, 1 );
					dr.DrawLine( origin, origin + dir * 5, Color.Red, Color.Red, 5, 1 );

					for (int i=0; i<route.Count-1; i++)
					{
						dr.DrawLine( route[i], route[i+1], Color.Black, Color.Black, 2, 2 );
					}
				}
			}
			else
			{
				uc.Move		=	0;
				uc.Strafe	=	0;
			}
		}

		bool Stun( float dt, Entity e, AIComponent ai, AIConfig cfg )
		{
			var health = e.GetComponent<HealthComponent>();

			if (health!=null)
			{
				if (health.LastDamage>0 && health.Health>0)
				{
					var percentage = MathUtil.Clamp(100 * health.LastDamage / health.Health, 0, 100);
					
					Log.Debug("#{0} stunning {1}%", e.ID, percentage);
					
					var timeout = cfg.StunMaxTimeout * percentage / 100;

					if (AIUtils.RollTheDice(percentage/100.0f))
					{
						if (ai.StunTimer.Timeout<timeout)
						{
							ai.StunTimer.SetND( timeout );
						}
					}
				}
			}

			return !ai.StunTimer.IsElapsed;
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Shooting :
		-----------------------------------------------------------------------------------------------*/

		void SetAimError( AIComponent ai, AIConfig cfg )
		{
			ai.PrevAimError	=	ai.NextAimError;
			ai.NextAimError =	MathUtil.Random.UniformRadialDistribution( 0, cfg.Accuracy ) * new Vector3( 1, 0.5f, 1 );
		}

		float AssessTarget( AIComponent ai, Vector3 attackerPos, AITarget target, Entity attacker )
		{
			float distance	=	Vector3.Distance( attackerPos, target.LastKnownPosition );
			float curve		=	AIUtils.Falloff( distance, 15 );

			float visFactor		=	target.Visible ? 1 : target.ForgettingTimer.Fraction * 0.5f;
			float stickFactor	=	(ai.Target == target) ? 1.0f : 0.5f;
			float attackFactor	=	target.Entity == attacker ? 3 : 1;

			return curve * stickFactor * visFactor * attackFactor;
		}


		void SelectTarget( Entity e, AIComponent ai, AIConfig cfg )
		{
			var uc		= e.GetComponent<UserCommandComponent>();
			var t		= e.GetComponent<Transform>();
			var attacker= e.GetComponent<HealthComponent>()?.LastAttacker; 
			var dr		= e.gs.Game.RenderSystem.RenderWorld.Debug.Async;
			var pos		= t.Position;

			var newTarget	=	AIUtils.Select( (tt) => AssessTarget(ai,pos,tt,attacker), ai.Targets );

			//	reset aiming timer when target is changed
			if (ai.Target!=newTarget)
			{
				ai.Target	=	newTarget;
				ai.Target?.AimingTimer.Set( cfg.AimTime );
			}

			//	...also track visibility of current target
			//	and reset timer if target not visible
			if (ai.Target!=null && !ai.Target.Visible)
			{
				ai.Target.AimingTimer.Set( cfg.AimTime );
			}
		}


		void AcquireCombatToken( Entity e, AIComponent ai )
		{
			var team = e.GetComponent<TeamComponent>();

			if (team!=null)
			{
				if (!ai.Options.HasFlag(AIOptions.NoToken))
				{
					if (ai.CombatToken==null)
					{
						ai.CombatToken	=	tokenPool.Acquire(team.Team, e);
					}
				}
			}
			else
			{
				Log.Warning("Entity #{0} has no TeamComponent, token can not be acquired", e.ID );
			}
		}


		bool Attack( float dt, Entity e, AIComponent ai, Transform t, UserCommandComponent uc, AIConfig cfg, bool stun )
		{
			//	clear attack flag :
			uc.Action &= ~UserAction.Attack;

			if (ai.Target==null) return false;
			if (stun) return false;

			var dr = e.gs.Game.RenderSystem.RenderWorld.Debug.Async;

			Vector3 aimPoint, attackPov;
			Vector3 aimError = Vector3.Lerp( ai.PrevAimError, ai.NextAimError, 1 - ai.ThinkTimer.Fraction );

			dr.DrawPoint( ai.Target.LastKnownPosition, 1.0f, Color.Red, 3 );

			if (ai.Target.Visible)
			{
				ai.Target.AimingTimer.Update((int)(dt*1000));
			}

			bool aimReady = ai.Target.AimingTimer.IsElapsed || SkipAttackDelay;

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

					if (DrawVisibility)
					{
						dr.DrawPoint( aimPoint, 5.0f, Color.Red, 3 );
					}

					var token = (ai.CombatToken!=null) || ai.Options.HasFlag(AIOptions.NoToken);

					if (error<cfg.AccuracyThreshold && ai.Target.Visible && aimReady && ai.AllowFire && token)
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

		readonly Aspect enemyAspect = new Aspect().Include<Transform,HealthComponent>().Any<PlayerComponent,AIComponent>();
		readonly Aspect noiseAspect = new Aspect().Include<Transform,NoiseComponent>();

		void LookForEnemies( GameTime gameTime, Entity entity, AIComponent ai, AIConfig cfg )
		{
			var noTarget = IronStar.IsNoTarget;

			Vector3 pov;
			BoundingFrustum frustum;
			BoundingSphere sphere;

			var dr = entity.gs.Game.RenderSystem.RenderWorld.Debug.Async;

			AIUtils.ForgetTargetsAndResetVisibility( cfg.ThinkTime, ai ); 

			bool visibility = false;

			if (AIUtils.GetSensoricBoundingVolumes(entity, cfg, out pov, out frustum, out sphere))
			{
				foreach ( var enemy in entity.gs.QueryEntities( enemyAspect ) )
				{
					var health		=	enemy.GetComponent<HealthComponent>();
					var noise		=	enemy.GetComponent<NoiseComponent>();
					var transform	=	enemy.GetComponent<Transform>();
					var isEnemies	=	AIUtils.IsEnemies( entity, enemy );
					var isAlive		=	health.Health > 0;

					var noiseLevel	=	noise==null ? 0 : noise.Level;

					var hasLOS		=	entity.HasLineOfSight( enemy, ref frustum, cfg.VisibilityRange, noiseLevel );

					if (isEnemies && hasLOS && isAlive && !noTarget)
					{
						visibility = true;
						AIUtils.SpotTarget( entity, ai, cfg, enemy );
					}
				}

				if (DrawVisibility)
				{
					var color	=	visibility ? Color.Red : Color.Lime;
					dr.DrawFrustum( frustum, color, 0.02f, 2 );
					dr.DrawRing( Matrix.Translation(sphere.Center), sphere.Radius, color, 32, 2, 1 );
				}
			}
		}


		void DetectAttacker( GameTime gameTime, Entity entity, AIComponent ai, AIConfig cfg )
		{
			var health = entity.GetComponent<HealthComponent>();

			if ( health!=null )
			{
				var attacker = health.LastAttacker;

				if (attacker!=null)
				{
					if (AIUtils.IsEnemies( entity, attacker ))
					{
						var attackerTransform = attacker.GetComponent<Transform>();

						if (!ai.Targets.Any( t => t.Entity == attacker ))
						{
							if (attackerTransform!=null)
							{
								 ai.Targets.Add( new AITarget( attacker, attackerTransform.Position, cfg.TimeToForget ) );
							}
						}
					}
				}
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

		void EnterNode( Entity e, DMNode newNode, AIComponent ai, string reason )
		{
			var oldNode		=	ai.DMNode;
			ai.DMNode		=	newNode;	//	set new node
			ai.AllowFire	=	true;		//	reset fire prevention

			if ( ai.CombatToken!=null )
			{
				ai.CombatToken.Release();
				ai.CombatToken = null;
			}

			if (LogDM)
			{
				Log.Debug("#{0}: {1} -> {2} : {3}", e.ID, oldNode, newNode, reason );
			}
		}
					
		/*-----------------------------------------------------------------------------------------------
		 *	Decision making nodes :
		-----------------------------------------------------------------------------------------------*/

		void EnterDead( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			EnterNode( e, DMNode.Dead, ai, reason );
		}

		//-----------------------------------------------------------

		void EnterStand( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			ai.StandTimer.SetND( cfg.IdleTimeout );
			EnterNode( e, DMNode.Stand, ai, reason );
		}


		bool NodeStand( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

			if (ai.Target!=null)
			{
				EnterStandGaping( e, ai, cfg, "WTF?" );
				return false;
			}

			if (ai.StandTimer.IsElapsed)
			{
				if (ai.Options.HasFlag(AIOptions.Roaming))
				{
					EnterRoaming( e, ai, cfg, "stand timeout");
					return false;
				}
				else
				{
					EnterStand( e, ai, cfg, "continue..." );
					return false;
				}
			}

			return true;
		}

		//-----------------------------------------------------------

		void EnterRoaming( Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
			var transform	=	e.GetComponent<Transform>();
			var location	=	nav.GetReachablePointInRadius( transform.Position, cfg.RoamRadius );
			ai.Route		=	nav.FindRoute( transform.Position, location );

			EnterNode( e, DMNode.Roaming, ai, reason );
		}


		bool NodeRoaming( Entity e, AIComponent ai, AIConfig cfg )
		{
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

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
			EnterNode( e, DMNode.StandGaping, ai, reason );

			ai.AllowFire	=	false;	//	we
			ai.Route		=	null;
		}

		bool NodeStandGaping( Entity e, AIComponent ai, AIConfig cfg )
		{
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

			if (ai.GapeTimer.IsElapsed)
			{
				if (ai.Target!=null && (ai.Target.Visible || ai.Target.Confirmed))
				{
					if (true || !ai.Target.Confirmed)
					{
						AIUtils.BroadcastEnemyTarget( cfg, e, ai.Target.Entity );	
					}

					EnterCombatChase( e, ai, cfg, "target detected" );
					return false;
				}

				EnterStand( e, ai, cfg, "false alarm");
				ai.Targets.EraseEntity(ai.Target?.Entity);
				ai.Target = null;
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

			EnterNode(e, DMNode.CombatChase, ai, reason);
		}


		bool NodeCombatChase( Entity e, AIComponent ai, AIConfig cfg )
		{
			var transform	=	e.GetComponent<Transform>();
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

			if (ai.Target!=null)
			{
				if (ai.Target.Visible || ai.Options.HasFlag(AIOptions.Camper))
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
			EnterNode( e, DMNode.CombatAttack, ai, reason );
			ai.Route = null; // stop moving
			AcquireCombatToken(e, ai);
			ai.AttackTimer.SetND( cfg.AttackTime );
		}


		bool NodeCombatAttack( Entity e, AIComponent ai, AIConfig cfg )
		{
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

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
			var origin		=	e.GetComponent<Transform>().Position;
			var dst			=	nav.GetReachablePointInRadius( origin, cfg.CombatMoveRadius );
			ai.Route		=	nav.FindRoute( origin, dst );
			EnterNode( e, DMNode.CombatMove, ai, reason );

			AcquireCombatToken(e, ai);

			ai.AllowFire	=	AIUtils.RollTheDice( cfg.AttackWhileMoving );
		}


		bool NodeCombatMove( Entity e, AIComponent ai, AIConfig cfg ) 
		{
			var health		=	e.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0) 
			{
				EnterDead( e, ai, cfg, "health <= 0");
				return false;
			}

			if ( ai.Route==null || ai.Route.Status!=Status.InProgress || ai.Options.HasFlag(AIOptions.Camper))
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
