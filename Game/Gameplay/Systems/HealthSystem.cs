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


		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		readonly Aspect playerAspect	= PlayerFactory.PlayerAspect;
		readonly Aspect powerupAspect	= new Aspect().Include<PowerupComponent,PickupComponent,TouchDetector>();
		readonly Aspect healthAspect	= new Aspect().Include<HealthComponent>();


		public static void ApplyDamage( Entity target, int damage, Entity attacker )
		{
			var health	=	target?.GetComponent<HealthComponent>();
			health?.InflictDamage( damage, attacker );
		}


		public void Update( IGameState gs, GameTime gameTime )
		{
			//PickupPowerups( gs, gameTime );
			ApplyDamage( gs, gameTime );
		}


		void ApplyDamage( IGameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(healthAspect);

			foreach ( var entity in entities )
			{
				var isPlayer	=	entity.GetComponent<PlayerComponent>()!=null;

				var health	=	entity.GetComponent<HealthComponent>();
				var godMode	=	entity.ContainsComponent<PlayerComponent>() && IronStar.IsGodMode;

				health.ApplyDamage(godMode, gameTime.Milliseconds);
			}
		}


	}
}
