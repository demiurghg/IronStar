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
	public class AISystem : StatelessSystem<AIComponent>
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

			InitUtilityAI();
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
				
				RunUtilityAI( entity, ai, cfg );

				ai.ThinkTimer.SetND( cfg.ThinkTime );
			}

			ai.UpdateTimers( gameTime );


			var stun = Stun( dt, entity, ai, cfg );
			//	only prevent fire on stun
			Attack( dt, entity, ai, t, uc, cfg, stun );
			Move( dt, entity, ai, cfg, stun );
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

			if (stun)
			{
				uc.Action	|=	UserAction.GestureStun;
			}
			else
			{
				uc.Action	&=	~UserAction.GestureStun;
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

					//	unalerted NPCs get 100$ stun
					if (ai.Target==null)
					{
						percentage = 100;
					}
					
					Log.Debug("#{0} stunning {1}%", e.ID, percentage);
					
					var timeout = cfg.StunTimeout;

					if (AIUtils.RollTheDice(percentage/100.0f))
					{
						ai.StunTimer.Set( timeout );
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


		void ReleaseCombatToken( Entity e, AIComponent ai )
		{
			var token = ai.CombatToken;
			if (token!=null)
			{
				token.Release();
				ai.CombatToken = null;
			}
		}


		bool Attack( float dt, Entity e, AIComponent ai, Transform t, UserCommandComponent uc, AIConfig cfg, bool stun )
		{
			//	clear attack flag :
			uc.Action &= ~UserAction.Attack;

			if (ai.Target==null) return false;
			if (stun) 
			{
				ai.Target = null;
				return false;
			}

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
		 *	Utility AI :
		-----------------------------------------------------------------------------------------------*/

		DMNode[] nodes;
		Dictionary<DMNode,float> score;

		void InitUtilityAI()
		{
			nodes	=	Enum.GetValues(typeof(DMNode)).Cast<DMNode>().ToArray();
			score	=	nodes.ToDictionary( n1 => n1, n1 => 0f );
		}


		void RunUtilityAI( Entity e, AIComponent ai, AIConfig cfg )
		{
			var origin			=	e.GetComponent<Transform>().Position;
			var team			=	e.GetComponent<TeamComponent>().Team;
			var health			=	e.GetComponent<HealthComponent>();
			var tokens			=	tokenPool.IsTokenAvailable( team );
			var timeout			=	ai.ActionTimer.IsElapsed;
			var hasTarget		=	ai.Target != null;
			var targetVisible	=	ai.Target!=null && ai.Target.Visible;
			var targetDist		=	ai.Target==null ? 999999 : Vector3.Distance( origin, ai.Target.LastKnownPosition );
			var proximityFactor	=	1 / (1 + targetDist * targetDist);
			var routeCompleted	=	ai.Route==null ? true : ai.Route.Status!=Status.InProgress;
			var unalerted		=	ai.DMNode==DMNode.Stand || ai.DMNode==DMNode.Roaming;

			score[ DMNode.Dead	]			=	( health.Health > 0 ? 0 : 999999 )
											;

			score[ DMNode.Stand	]			=	( hasTarget ? -200 : 0 )
											+	( ai.DMNode==DMNode.Stand && !timeout ? 100 : 1 )
											+	( ai.DMNode==DMNode.StandGaping && !targetVisible ? 100 : 1 )
											;

			score[ DMNode.Roaming	]		=	( hasTarget ? -200 : 0 )
											+	( ai.DMNode==DMNode.Roaming && !routeCompleted ? 100 : 1 )
											+	( ai.Options.HasFlag(AIOptions.Roaming) ? 0 : -200 )
											;

			//	get back to unalerted state if target is not visible again
			//	stay gaping for more concrete time
			score[ DMNode.StandGaping ]		=	( (unalerted && ai.Target!=null) ? 200 : 0 )
											+	ai.DMNode==DMNode.StandGaping ? ai.ActionTimer.Fraction * 100 : 0
											;

			score[ DMNode.CombatChase ]		=	( hasTarget && !targetVisible ? 100 : 0 )
											+	ai.DMNode==DMNode.CombatChase ? ai.ActionTimer.Fraction * 100 : 0
											+	( ai.Options.HasFlag(AIOptions.Camper) ? -500 : 0 )
											;

			score[ DMNode.CombatAttack ]	=	( hasTarget && targetVisible ? 100 : 0 )
											+	( ai.DMNode==DMNode.CombatAttack ? ai.ActionTimer.Fraction * 100 : 0 )
											+	( (!tokens) ? -100 : 0 )
											;

			score[ DMNode.CombatMove ]		=	( hasTarget && targetVisible ? 100 : 0 )
											+	ai.DMNode==DMNode.CombatMove ? ai.ActionTimer.Fraction * 100 : 0
											+	( ai.Options.HasFlag(AIOptions.Camper) ? -500 : 0 )
											+	( tokens ? 100 : 0 )
											+	proximityFactor * 100
											;

			var nextNode	=	AIUtils.Select(	node => Math.Max(0, score[node]), nodes );

			if (LogDM)
			{
				Log.Debug("----------------------");
				foreach ( var scorePair in score )
				{
					Log.Debug("{0,20} = {1} {2}", scorePair.Key, scorePair.Value, scorePair.Key==nextNode ? "<<------" : "");
				}
			}

			if (ai.DMNode!=nextNode)
			{
				ai.DMNode	=	nextNode;
				ReleaseCombatToken( e, ai );
				InvokeNode( true, e, ai, cfg );
			}
			else
			{
				//Log.Debug("continue : {0}", ai.DMNode);
				InvokeNode( false, e, ai, cfg );
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Decision making utils :
		-----------------------------------------------------------------------------------------------*/

		void InvokeNode( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			switch (ai.DMNode)
			{
				case DMNode.Dead:			break;
				case DMNode.Stand:			NodeStand		( init, e, ai, cfg ); break;
				case DMNode.Roaming:		NodeRoaming		( init, e, ai, cfg ); break;
				case DMNode.StandGaping:	NodeStandGaping	( init, e, ai, cfg ); break;
				case DMNode.CombatChase:	NodeCombatChase	( init, e, ai, cfg ); break;
				case DMNode.CombatAttack:	NodeCombatAttack( init, e, ai, cfg ); break;	
				case DMNode.CombatMove:		NodeCombatMove	( init, e, ai, cfg ); break;
			}
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Decision making nodes :
		-----------------------------------------------------------------------------------------------*/

		void NodeDead( bool init, Entity e, AIComponent ai, AIConfig cfg, string reason )
		{
		}

		//-----------------------------------------------------------

		void NodeStand( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			if (init)
			{
				ai.AllowFire	=	false;
				ai.ActionTimer.SetND( cfg.IdleTimeout );
				ai.ThinkTimer.SetND( cfg.GapeTimeout );
			}
		}

		//-----------------------------------------------------------

		void NodeRoaming( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			if (init)
			{
				ai.AllowFire	=	false;
				var transform	=	e.GetComponent<Transform>();
				var location	=	nav.GetReachablePointInRadius( transform.Position, cfg.RoamRadius );
				ai.Route		=	nav.FindRoute( transform.Position, location );
			}
		}

		//-----------------------------------------------------------

		bool NodeStandGaping( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			if (init)
			{
				ai.AllowFire	=	false;
				ai.Route		=	null;
			}

			return true;
		}

		//-----------------------------------------------------------

		void NodeCombatChase( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			if (init)
			{
				var origin		=	e.GetComponent<Transform>().Position;

				ai.AllowFire	=	true;
				ai.Route		=	nav.FindRoute( origin, ai.Target.LastKnownPosition );

				if (ai.Route==null)
				{
					var randPoint	=	nav.GetReachablePointInRadius( origin, cfg.RoamRadius );
					ai.Route		=	nav.FindRoute( origin, randPoint );
				}
			}
		}

		//-----------------------------------------------------------

		void NodeCombatAttack( bool init, Entity e, AIComponent ai, AIConfig cfg )
		{
			if (init)
			{
				ai.Route		=	null; // stop moving

				AcquireCombatToken( e, ai );
				ai.AllowFire	=	true;
			}
		}

		//-----------------------------------------------------------

		void NodeCombatMove( bool init, Entity e, AIComponent ai, AIConfig cfg ) 
		{
			if (init)
			{
				var origin		=	e.GetComponent<Transform>().Position;
				var dst			=	nav.GetReachablePointInRadius( origin, cfg.CombatMoveRadius );
				ai.Route		=	nav.FindRoute( origin, dst );

				AcquireCombatToken( e, ai );
				ai.AllowFire	=	true;
			}
		}
	}
}
