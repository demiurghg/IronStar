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
using IronStar.Gameplay.Components;

namespace IronStar.AI
{
	class PerceptionSystem : StatelessSystem<BehaviorComponent>
	{
		public bool Enabled = true;
		readonly PhysicsCore physics;

		Entity player = null;
		bool playerAlive = false;
		DebugRender dr;

		public Entity Player
		{
			get { return player; }
		}


		public PerceptionSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}


		public override void Update( GameState gs, GameTime gameTime )
		{
			player		=	gs.GetPlayer();
			playerAlive	=	IsPlayerAlive( player );

			base.Update( gs, gameTime );
		}


		protected override void Process( Entity entity, GameTime gameTime, BehaviorComponent behavior )
		{
			Vector3 pov;
			BoundingFrustum frustum;
			BoundingSphere sphere;

			if (Enabled && !IronStar.IsNoTarget)
			{
				if (GetSensoricBoundingVolumes(entity, out pov, out frustum, out sphere))
				{
					bool hasLos = false;
					bool visibility = false;
					
					if (player!=null && playerAlive)
					{
						var playerPos	=	player.GetPOV();
						hasLos			=	physics.HasLineOfSight( pov, playerPos, entity, player );
						visibility		=	hasLos && ( frustum.Contains( playerPos ) == ContainmentType.Contains );
					}

					if (visibility)
					{
						behavior.LastSeenTarget	=	player;
					}

					if (!playerAlive)
					{
						behavior.LastSeenTarget	=	null;
					}

					entity.GetBlackboard().SetEntry( BehaviorSystem.KEY_TARGET_ENTITY, behavior.LastSeenTarget );

					var health = entity.GetComponent<HealthComponent>();
					if (health!=null)
					{
						if (health.LastAttacker!=null)
						{
							behavior.LastSeenTarget = health.LastAttacker;
							entity.GetBlackboard().SetEntry( BehaviorSystem.KEY_TARGET_ENTITY, health.LastAttacker );
						}
					}

					/*
					var color	=	visibility ? Color.Red : Color.Lime;
					dr.DrawFrustum( frustum, color, 0.02f, 2 );
					dr.DrawRing( Matrix.Translation(sphere.Center), sphere.Radius, Color.Green, 32, 2, 1 );
					*/
				}
			}
		}


		bool IsPlayerAlive(Entity player)
		{
			var health = player?.GetComponent<HealthComponent>();
			
			return (health==null) || (health.Health > 0);
		}

		/*-----------------------------------------------------------------------------------------------
		 *	Utilities :
		-----------------------------------------------------------------------------------------------*/

		bool GetSensoricBoundingVolumes( Entity npcEntity, out Vector3 pov, out BoundingFrustum frustum, out BoundingSphere sphere )
		{
			frustum	=	new BoundingFrustum();
			sphere	=	new BoundingSphere();
			pov		=	new Vector3();

			var cc			=	npcEntity.GetComponent<CharacterController>();
			var transform	=	npcEntity.GetComponent<Transform>();
			var behavior	=	npcEntity.GetComponent<BehaviorComponent>();
			var uc			=	npcEntity.GetComponent<UserCommandComponent>();

			if (cc==null || transform==null || transform==null || uc==null )
			{
				return false;
			}

			var head		=	uc.ComputePovTransform( transform.Position, cc.PovOffset );
			var view		=	Matrix.Invert( head );

			var proj		=	Matrix.PerspectiveFovRH( MathUtil.DegreesToRadians( behavior.VisibilityFov ), 2, 0.01f, behavior.VisibilityRange );

			pov				=	head.TranslationVector;
			frustum			=	new BoundingFrustum( view * proj );
			sphere			=	new BoundingSphere( transform.Position, behavior.HearingRange );

			return true;
		}
	}
}
