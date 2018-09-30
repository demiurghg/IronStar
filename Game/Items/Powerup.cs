using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.Core;
using IronStar.Entities;
using IronStar.Entities.Players;

namespace IronStar.Items {
	public class Powerup : Item {

		readonly GameWorld world;

		readonly int bonusHealth;
		readonly int bonusArmor;
		

		public Powerup( uint id, short clsid, GameWorld world, PowerupFactory factory ) : base( id, clsid )
		{
			this.world			=	world;

			this.bonusArmor		=	factory.ArmorBonus;
			this.bonusHealth	=	factory.HealthBonus;
		}


		public override bool Pickup( Entity other )
		{
			var player = other as Player;

			if ( bonusHealth>0 && player.Health<100 || bonusArmor>0 && player.Armor<100 ) {

				player.Health	=	Math.Min( player.Health	+ bonusHealth, 100 );
				player.Armor	=	Math.Min( player.Armor	+ bonusArmor , 100 );

				return true;

			} else {

				return false;

			}
		}
	}
}
