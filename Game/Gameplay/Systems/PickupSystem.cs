using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using IronStar.SFX;
using Fusion.Core.Mathematics;

namespace IronStar.Gameplay.Systems
{
	public class PickupSystem : ISystem
	{
		public void Add( IGameState gs, Entity e )  {}
		public void Remove( IGameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect pickupAspect	=	new Aspect().Include<PickupComponent,TouchDetector,PowerupComponent>();
		readonly Aspect recipientAspect	=	new Aspect().Include<PlayerComponent>().Any<HealthComponent,InventoryComponent>();


		public PickupSystem()
		{
		}


		public void Update( IGameState gs, GameTime gameTime )
		{
			var pickupEntities = gs.QueryEntities( pickupAspect );

			foreach ( var pickupItem in pickupEntities )
			{
				var touchDetector = pickupItem.GetComponent<TouchDetector>();

				foreach ( var touchEntity in touchDetector )
				{
					if (recipientAspect.Accept(touchEntity) )
					{
						if ( TryPickItemUp( gs, touchEntity, pickupItem ) )
						{
							break;
						}
					}
				}
			}
		}


		bool TryPickItemUp( IGameState gs, Entity recipient, Entity pickupItem )
		{
			var inventory	=	recipient.GetComponent<InventoryComponent>();
			var wpnState	=	recipient.GetComponent<WeaponStateComponent>();
			var health		=	recipient.GetComponent<HealthComponent>();
			var pickup		=	pickupItem.GetComponent<PickupComponent>();
			var powerup		=	pickupItem.GetComponent<PowerupComponent>();

			bool success	=	false;

			if (inventory!=null)
			{
				success |= inventory.TryGiveWeapon( powerup.Weapon, wpnState );
				success |= inventory.TryGiveAmmo( powerup.Ammo, powerup.AmmoCount );
			}

			if (health!=null)
			{
				success |= health.TryGiveArmor ( powerup.Armor  );
				success |= health.TryGiveHealth( powerup.Health );
			}

			if (success)
			{
				FXPlayback.SpawnFX( gs, pickup.FXName, pickupItem );
				pickupItem.Kill();
			}

			return success;
		}
	}
}
