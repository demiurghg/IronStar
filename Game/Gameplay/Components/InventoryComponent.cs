using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class InventoryComponent : IComponent
	{
		public uint ActiveWeaponID = 0;

		readonly List<uint> itemIDs = new List<uint>();

		public bool AddItem( uint id )
		{
			if (!itemIDs.Contains(id)) 
			{
				itemIDs.Add(id);
				return true;
			} 
			else
			{
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


		public void Added( GameState gs, Entity entity ) {}
		public void Removed( GameState gs ) {}
		public void Load( GameState gs, Stream stream ) {}
		public void Save( GameState gs, Stream stream ) {}
	}
}
