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

		readonly GameWorld world;
		readonly ContentManager content;
		readonly Dictionary<string,int> inventory;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		public CharacterInventory ( GameWorld world, CharacterFactory factory )
		{
			this.world		=	world;
			this.content	=	world.Content;
				
			inventory	=	new Dictionary<string, int>();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool Contains ( string name )
		{
			int count;
			if (inventory.TryGetValue(name, out count)) {
				if (count>0) {
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool SetActiveItem ( string name )
		{
			if (!Contains(name)) {
				return false;
			}	
			return false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		ItemFactory GetFactory( string name )
		{
			return content.Load<ItemFactory>(@"items\" + name);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="count"></param>
		public bool TryGive ( string name, int count )
		{
			var factory = GetFactory(name);

			if (factory==null) {
				Log.Warning("Unknown item {0}", name);
				return false;
			}

			if (!inventory.ContainsKey(name)) {
				inventory.Add( name, 0 );
			}

			int oldCount = inventory[ name ];
			int maxCount = factory.MaxInventoryCount;

			if (oldCount>=maxCount) {
				return false;
			} else {
				inventory[ name ] = Math.Min( oldCount + count, maxCount );
				return true;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public bool TryConsume ( string name, int count )
		{
			int oldCount;

			if (inventory.TryGetValue( name, out oldCount )) {

				if (oldCount<count) {
					return false;
				} else {
					oldCount -= count;
					inventory[name] = oldCount;
					return true;
				}
				
			} else {
				return false;
			}
		}

	}
}
