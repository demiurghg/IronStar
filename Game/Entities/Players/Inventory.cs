using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;

namespace IronStar.Entities.Players {
	public class Inventory {

		readonly int[] inventory;

		/// <summary>
		/// 
		/// </summary>
		public Inventory()
		{
			int maxItem	=	Enum.GetValues(typeof(InventoryItem)).Cast<int>().Max();
			inventory	=	new int[maxItem+1];
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool TryConsume ( InventoryItem item, int amount )
		{
			if (inventory[(int)item]<amount) {
				return false;
			} else {
				inventory[(int)item] -= amount;
				return true;
			}
		}

	}
}
