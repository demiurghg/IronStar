﻿using System;
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

		int count;
		readonly int maxCount;
		readonly GameWorld world;


		public int Count {
			get {
				return count;
			}
		}
		

		public int MaxCount {
			get {
				return maxCount;
			}
		}
		

		public Ammo( uint id, short clsid, GameWorld world, AmmoFactory factory ) : base( id, clsid, factory )
		{
			this.world		=	world;
			this.count		=	factory.Count;
			this.maxCount	=	factory.MaxCount;
		}


		public override bool Pickup( Entity player )
		{
			var existingAmmo = world.Items.GetOwnedItemByClass( player.ID, ClassID ) as Ammo;

			if (existingAmmo==null) {
				Owner = player.ID;
				return true;
			} else {
				existingAmmo.count += count;
				existingAmmo.count = Math.Min( existingAmmo.count, existingAmmo.maxCount );
				return true;
			}
		}



		public bool AddAmmo ( int add )
		{
			if (count==maxCount) {
				return false;
			} else {
				count += add;
				count  = Math.Min( count, maxCount );
				return true;
			}
		}



		public bool	ConsumeAmmo ( int requested )
		{
			if (count < requested) {
				return false;
			} else {
				count -= requested;
				return true;
			}
		}


		public override void Write( BinaryWriter writer )
		{
			base.Write( writer );

			writer.Write( count );
		}


		public override void Read( BinaryReader reader )
		{
			base.Read( reader );

			count	=	reader.ReadInt32();
		}
	}
}