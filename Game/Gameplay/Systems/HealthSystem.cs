using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;

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


		readonly Aspect healthInventoryAspect	= new Aspect().Include<InventoryComponent,HealthComponent>();
		readonly Aspect powerupAspect			= new Aspect().Include<PowerupComponent>();
		readonly Aspect healthAspect			= new Aspect().Include<HealthComponent>();


		public static void ApplyDamage( Entity target, int damage )
		{
			var health = target?.GetComponent<HealthComponent>();
			health?.InflictDamage( damage );
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			UpdatePowerups( gs, gameTime );
			UpdateDamage( gs, gameTime );
		}


		void UpdatePowerups( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities( healthInventoryAspect );

			foreach ( var entity in entities )
			{
				var inventory	=	entity.GetComponent<InventoryComponent>();
				var health		=	entity.GetComponent<HealthComponent>();
				
				var powerupEntity		=	inventory.FindItem( gs, powerupAspect );
				var powerupComponent	=	powerupEntity?.GetComponent<PowerupComponent>();

				if (powerupComponent!=null)
				{
					health.Health	+=	powerupComponent.Health;
					health.Armor	+=	powerupComponent.Armor;

					inventory.RemoveItem( powerupEntity );
				}
			}
		}


		void UpdateDamage( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities(healthAspect);

			foreach ( var entity in entities )
			{
				var health	=	entity.GetComponent<HealthComponent>();
				var status	=	health.ApplyDamage();

				if (status==HealthStatus.JustDied)
				{
					gs.Execute( health.Action, entity );
				}
			}
		}
	}
}
