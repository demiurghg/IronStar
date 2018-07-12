using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Items;
using Fusion.Core.Mathematics;
using Fusion.Core;
using IronStar.Core;

namespace IronStar.Entities.Players {
	public class Inventory : HashSet<Item> {

		public Item CurrentItem { get; set; }
		public Item PendingItem { get; set; }
	
		public void Update ( GameTime gameTime, Entity entity )
		{
			foreach ( var item in this ) {
				item.Update(gameTime, entity);
			}

			RemoveWhere( item=>item.IsDepleted() );
		}

	}
}
