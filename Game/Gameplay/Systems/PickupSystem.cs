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

namespace IronStar.Gameplay.Systems
{
	public class PickupSystem : ISystem
	{
		public void Add( GameState gs, Entity e )  {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect itemAspect		=	new Aspect().Include<PickupComponent,TouchDetector,Transform>();
		readonly Aspect weaponAspect	=	new Aspect().Include<PickupComponent,WeaponComponent>().Exclude<AmmoComponent>();
		readonly Aspect ammoAspect		=	new Aspect().Include<PickupComponent,AmmoComponent,NameComponent>().Exclude<WeaponComponent>();
		readonly Aspect inventoryAspect	=	new Aspect().Include<InventoryComponent,Transform>();

		
		public PickupSystem()
		{
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var pickupEntities = gs.QueryEntities( itemAspect );

			foreach ( var pickupItem in pickupEntities )
			{
				var touchDetector = pickupItem.GetComponent<TouchDetector>();

				foreach ( var touchEntity in touchDetector )
				{
					if ( inventoryAspect.Accept(touchEntity) && PickItemUp( gs, touchEntity, pickupItem ) )
					{
						break;
					}
				}
			}
		}


		bool PickItemUp( GameState gs, Entity recipient, Entity pickupItem )
		{
			var inventory	=	recipient.GetComponent<InventoryComponent>();
			var transform	=	recipient.GetComponent<Transform>();
			var pickup		=	pickupItem.GetComponent<PickupComponent>();
			var name		=	pickupItem.GetComponent<NameComponent>()?.Name;
			var ammo		=	pickupItem.GetComponent<AmmoComponent>();
			var weapon		=	pickupItem.GetComponent<WeaponComponent>();

			if ( weaponAspect.Accept( pickupItem ) )
			{
				if ( !inventory.ContainsItem( gs, name ) )
				{
					inventory.AddItem( pickupItem.ID );
					inventory.SwitchWeapon( pickupItem.ID );
				}
				else 
				{
					return false;
				}
			}
			else if ( ammoAspect.Accept( pickupItem ) )
			{
				var existingAmmoEntity = inventory.FindItem( gs, name );

				if (existingAmmoEntity!=null)
				{
					existingAmmoEntity.GetComponent<AmmoComponent>().Count += ammo.Count;
					gs.Kill(pickupItem.ID);
				}
				else
				{
					inventory.AddItem( pickupItem.ID );
				}
			}
			else 
			{
				inventory.AddItem( pickupItem.ID );
			}

			FXPlayback.SpawnFX( gs, pickup.FXName, pickupItem );
			pickupItem.RemoveComponent<Transform>();
			Log.Message("Pickup: {0}", name ?? "");

			return true;
		}
	}
}
