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
		public void Add( GameState gs, Entity e )  {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		readonly Aspect itemAspect		=	new Aspect().Include<PickupComponent,TouchDetector,Transform>().Single<WeaponComponent,AmmoComponent>();
		readonly Aspect weaponAspect	=	new Aspect().Include<PickupComponent,TouchDetector,Transform,WeaponComponent>().Include<NameComponent>();
		readonly Aspect ammoAspect		=	new Aspect().Include<PickupComponent,TouchDetector,Transform,AmmoComponent>().Include<NameComponent>();
		readonly Aspect inventoryAspect	=	new Aspect().Include<PlayerComponent,InventoryComponent,Transform>();


		
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
			var pickup		=	pickupItem.GetComponent<PickupComponent>();

				
			if (   TryPickAsWeapon( gs, inventory, pickupItem )
				|| TryPickAsAmmo( gs, inventory, pickupItem )
			)
			{
				FXPlayback.SpawnFX( gs, pickup.FXName, pickupItem );
				pickupItem.RemoveComponent<Transform>();

				return true;
			}
			else
			{
				return false;
			}
		}



		bool TryPickAsWeapon( GameState gs, InventoryComponent inventory, Entity weaponEntity )
		{
			if ( weaponAspect.Accept( weaponEntity ) )
			{
				var name	=	weaponEntity.GetComponent<NameComponent>()?.Name;
				var weapon	=	weaponEntity.GetComponent<WeaponComponent>();

				var existingWeaponEntity = inventory.FindItem<WeaponComponent,NameComponent>( gs, (w,n) => n.Name==name );

				if ( existingWeaponEntity==null )
				{
					inventory.AddItem( weaponEntity );
					inventory.SwitchWeapon( weaponEntity );
				}
				else 
				{
					inventory.SwitchWeapon( existingWeaponEntity );
					gs.Kill( weaponEntity );
				}

				var ammoEntity = gs.Spawn( weapon.AmmoClass );
				
				if (ammoEntity!=null)
				{
					TryPickAsAmmo( gs, inventory, ammoEntity ); 
				}

				return true;
			}
			else
			{
				return false;
			}
		}


		bool TryPickAsAmmo( GameState gs, InventoryComponent inventory, Entity ammoEntity, bool forcePick = false )
		{
			if ( ammoAspect.Accept( ammoEntity ) )
			{
				var name = ammoEntity.GetComponent<NameComponent>().Name;
				var ammo = ammoEntity.GetComponent<AmmoComponent>();

				AmmoComponent existingAmmo;
				NameComponent existingName;

				var existingAmmoEntity = inventory.FindItem( gs, (a,n) => n.Name==name, out existingAmmo, out existingName );

				if ( existingAmmoEntity!=null )
				{
					if ( existingAmmo.Count < existingAmmo.Capacity || forcePick )
					{
						existingAmmo.Count = MathUtil.Clamp( existingAmmo.Count + ammo.Count, 0, existingAmmo.Capacity );
						gs.Kill( ammoEntity );
						return true;
					}
					else
					{
						return false;
					}
				}
				else
				{
					inventory.AddItem( ammoEntity );
					return true;
				}
			}
			else
			{
				return false;
			}
		}
	}
}
