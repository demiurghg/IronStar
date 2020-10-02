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
		int bullets;
		int shells;
		int cells;
		int slugs;
		int rockets;
		int grenades;

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


		public int GetAmmo( AmmoType ammo )
		{
			switch (ammo)
			{
				case AmmoType.Bullets	: return bullets;
				case AmmoType.Shells	: return shells	;
				case AmmoType.Cells		: return cells	;
				case AmmoType.Slugs		: return slugs	;
				case AmmoType.Rockets	: return rockets;
				case AmmoType.Grenades	: return grenades;
			}
			return 0;
		}


		public int GetMaxAmmo( AmmoType ammo )
		{
			switch (ammo)
			{
				case AmmoType.Bullets	: return 200;
				case AmmoType.Shells	: return 50;
				case AmmoType.Cells		: return 200;
				case AmmoType.Slugs		: return 50;
				case AmmoType.Rockets	: return 50;
				case AmmoType.Grenades	: return 50;
			}
			return 0;
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


		public bool ContainsItem( GameState gs, string name )
		{
			return FindItem(gs, name) != null;
		}


		public bool ContainsItem( GameState gs, Aspect itemAspect )
		{
			return FindItem(gs, itemAspect) != null;
		}


		public Entity FindItem ( GameState gs, string name )
		{
			foreach ( var itemId in itemIDs )
			{
				var e = gs.GetEntity( itemId );
				var n = e?.GetComponent<NameComponent>()?.Name;

				if (n==name)
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
