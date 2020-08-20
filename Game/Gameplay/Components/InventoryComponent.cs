using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class InventoryComponent : IComponent, IEnumerable<uint>
	{
		uint activeWeaponID = 0;
		uint pendingWeaponID = 0;
		readonly List<uint> itemIDs = new List<uint>();


		public uint ActiveWeaponID { get { return activeWeaponID; } }
		public bool HasPendingWeapon { get { return pendingWeaponID!=0; } }


		public bool SwitchWeapon( uint id )
		{
			if (itemIDs.Contains(id) && activeWeaponID!=id) 
			{
				pendingWeaponID	=	id;
				return true;
			}
			else return false;
		}


		public void FinalizeWeaponSwitch()
		{
			activeWeaponID	=	pendingWeaponID;
			pendingWeaponID	=	0;
		}


		public bool AddItem( uint id )
		{
			if (!itemIDs.Contains(id)) 
			{
				itemIDs.Add(id);
				return true;
			} 
			else
			{
				Log.Warning("InventoryComponent : attempt to put the same entity twice");
				return false;
			}
		}


		public bool RemoveItem( uint id )
		{
			if (itemIDs.Contains(id)) 
			{
				itemIDs.Remove(id);
				return true;
			} 
			else
			{
				Log.Warning("InventoryComponent : attempt to remove non-existing entity");
				return false;
			}
		}


		public bool RemoveItem( Entity item )
		{
			if (item==null) return false;
			return RemoveItem( item.ID );
		}


		public Entity FindItem ( GameState gs, Aspect itemAspect )
		{
			foreach ( var itemId in itemIDs )
			{
				var e = gs.GetEntity( itemId );

				if (itemAspect.Accept(e))
				{
					return e;
				}
			}

			return null;
		}


		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}

		public IEnumerator<uint> GetEnumerator()
		{
			return ( (IEnumerable<uint>)itemIDs ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<uint>)itemIDs ).GetEnumerator();
		}
	}
}
