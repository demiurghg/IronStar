using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.ECSFactories;
using IronStar.ECSPhysics;
using IronStar.SFX;

namespace IronStar.Gameplay.Systems
{
	public class HealthSystem : ISystem
	{
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}


		readonly Aspect playerAspect	= PlayerFactory.PlayerAspect;
		readonly Aspect powerupAspect	= new Aspect().Include<PowerupComponent,PickupComponent,TouchDetector>();
		readonly Aspect healthAspect	= new Aspect().Include<HealthComponent>();


		public static void ApplyDamage( Entity target, int damage, ref SurfaceType surfaceType )
		{
			var health	=	target?.GetComponent<HealthComponent>();

			if (health!=null)
			{
				surfaceType = health.InflictDamage( damage );
			}
		}


		public static void ApplyDamage( Entity target, int damage )
		{
			var dummy = SurfaceType.Metal;
			ApplyDamage( target, damage, ref dummy );
		}




		public void Update( GameState gs, GameTime gameTime )
		{
			PickupPowerups( gs, gameTime );
			ApplyDamage( gs, gameTime );
		}


		void PickupPowerups( GameState gs, GameTime gameTime )
		{
			var powerupEntities	=	gs.QueryEntities( powerupAspect );
			var playerEntity	=	gs.QueryEntities( playerAspect ).LastOrDefault();
			var playerHealth	=	playerEntity?.GetComponent<HealthComponent>();

			if (playerHealth==null)
			{
				return;
			}

			foreach ( var powerupEntity in powerupEntities )
			{
				var touch		=	powerupEntity.GetComponent<TouchDetector>();
				var powerup		=	powerupEntity.GetComponent<PowerupComponent>();
				var pickup		=	powerupEntity.GetComponent<PickupComponent>();

				if (touch.Contains( playerEntity ))
				{
					bool containsHealth	=	powerup.Health > 0;
					bool containsArmor	=	powerup.Armor > 0;
					bool needHealth		=	playerHealth.Health < playerHealth.MaxHealth;
					bool needArmor		=	playerHealth.Armor  < playerHealth.MaxArmor;

					bool shouldPickup	=	(needHealth && containsHealth) || (needArmor && containsArmor);

					if (shouldPickup)
					{
						playerHealth.Health	=	Math.Min( playerHealth.Health + powerup.Health, playerHealth.MaxHealth );
						playerHealth.Armor	=	Math.Min( playerHealth.Armor  + powerup.Armor , playerHealth.MaxArmor );

						gs.Kill( powerupEntity );
						FXPlayback.SpawnFX( gs, pickup?.FXName, powerupEntity );
					}
				}
			}
		}


		void ApplyDamage( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(healthAspect);

			foreach ( var entity in entities )
			{
				var health	=	entity.GetComponent<HealthComponent>();
				var godMode	=	entity.ContainsComponent<PlayerComponent>() && IronStar.IsGodMode;

				var status	=	health.ApplyDamage(godMode);

				if (status==HealthStatus.JustDied)
				{
					gs.Execute( health.Action, entity );
				}
			}
		}
	}
}
