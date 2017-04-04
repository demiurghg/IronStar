using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;
using IronStar.Items;

namespace IronStar.Entities {
	public class CharacterInventory {

		readonly Entity entity;
		readonly GameWorld world;
		readonly ContentManager content;
		readonly List<Item> items = new List<Item>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		public CharacterInventory ( Entity entity, GameWorld world, CharacterFactory factory )
		{
			this.entity		=	entity;
			this.world		=	world;
			this.content	=	world.Content;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int CountItems ( Item sameItem )
		{
			return items.Count( i => i.Name == sameItem.Name );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void AddItem ( Item item )
		{
			items.Add( item );
		}

		/// <summary>
		///// Attempt to put item to inventory.
		///// If items of given type is greater than MaxCount return false.
		///// </summary>
		///// <param name="item"></param>
		///// <returns></returns>
		//public bool TryGive ( Item item )
		//{
		//	int count = items.Count( it => it.GetType() == item.GetType() );

		//	item.

		//	if (count>item.MaxCount) {
		//		return false;
		//	} else {
		//		item.Pickup(
		//		items.Add( item );
		//		return true;
		//	}
		//}
	}
}
