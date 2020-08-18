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

		readonly Aspect itemAspect;

		
		public PickupSystem()
		{
			itemAspect	=	new Aspect().Include<PickupComponent,TouchDetector>();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var pickupEntities = gs.QueryEntities( itemAspect );

			foreach ( var pickupItem in pickupEntities )
			{
				var touchDetector = pickupItem.GetComponent<TouchDetector>();

				foreach ( var touchEntity in touchDetector )
				{
					if ( PickItemUp( gs, touchEntity, pickupItem ) )
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

			//	recipient has no inventory, skip:
			if (inventory==null)
			{
				return false;
			}

			//	no trnasform, no pickup item :
			if (transform==null || pickup==null)
			{
				Log.Warning("Item {0} can not be picked up by {1}", pickupItem.ID, recipient.ID );
				return false;
			}

			pickupItem.RemoveComponent<Transform>();

			inventory.AddItem( pickupItem.ID );

			FXPlayback.SpawnFX( gs, pickup.FXName, 0, transform.Position );

			Log.Message("Pickup: {0}", pickupItem);

			//
			//	activate weapon :
			//
			var weapon		=	pickupItem.GetComponent<WeaponComponent>();

			if (weapon!=null)
			{
				inventory.SwitchWeapon( pickupItem.ID );
			}


			return true;
		}
	}
}
