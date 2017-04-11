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

		int currentWeapon = 0;
		int nextWeapon = 0;


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
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		public void Update ( float elapsedTime )
		{
			foreach ( var item in items ) {
				item.Update( elapsedTime );
			}

			items.RemoveAll( item => (item is Powerup) && ((item as Powerup).IsExhausted()) );
		}



		/// <summary>
		/// Switches weapon in inventory
		/// </summary>
		public void SwitchWeapon ()
		{
			//	already switching weapon
			if (nextWeapon!=0) {
				return;
			}

			//	find next weapon in inventory
			var nextWeaponItem = items.FirstOrDefault( item => item is Weapon && item.ID != currentWeapon );

			//	no weapon - abort weapon switch
			if (nextWeaponItem==null) {
				nextWeapon = 0;
				return;
			}

			nextWeapon	=	nextWeaponItem.ID;
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Internal stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Item GetItem(int id) 
		{
			return items.SingleOrDefault( item => item.ID == id );
		}



	}
}
