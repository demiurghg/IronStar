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
				
				var powerup		=	inventory.FindItem( gs, powerupAspect )?.GetComponent<PowerupComponent>();

				if (powerup!=null)
				{
					health.Health	+=	powerup.Health;
					health.Armor	+=	powerup.Armor;
				}
			}
		}


		void UpdateDamage( GameState gs, GameTime gameTime )
		{
			var healthComponents = gs.QueryComponents<HealthComponent>();

			foreach ( var h in healthComponents )
			{
				h.ApplyDamage();
			}
		}
	}
}
