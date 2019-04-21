using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.Core;
using IronStar.Entities;

namespace IronStar.Items {
	public class Ammo : Item {

		readonly GameWorld world;

		

		public Ammo( uint id, short clsid, GameWorld world, AmmoFactory factory ) : base( id, clsid, factory )
		{
			this.world		=	world;
			this.Count		=	(short)factory.Count;
			this.MaxCount	=	(short)factory.MaxCount;
		}


		public override bool Pickup( Entity player )
		{
			var existingAmmo = world.Items.GetOwnedItemByClass( player.ID, ClassID ) as Ammo;

			if (existingAmmo==null) {
				Owner = player.ID;
				return true;
			} else {
				existingAmmo.Count += Count;
				existingAmmo.Count = Math.Min( existingAmmo.Count, existingAmmo.MaxCount );
				return true;
			}
		}



		public bool AddAmmo ( int add )
		{
			if (Count==MaxCount) {
				return false;
			} else {
				Count += (short)add;
				Count  = Math.Min( Count, MaxCount );
				return true;
			}
		}



		public bool	ConsumeAmmo ( int requested )
		{
			if (Count < requested) {
				return false;
			} else {
				Count -= (short)requested;
				return true;
			}
		}
	}
}
