using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay.Components
{
	public class PowerupComponent : Component
	{
		public int Health			=	0;
		public int Armor			=	0;

		public WeaponType Weapon	=	WeaponType.None;
		public AmmoType Ammo		=	AmmoType.Bullets;
		public int AmmoCount		=	0;

		public PowerupComponent()
		{
		}

		public PowerupComponent( int health, int armor )
		{
			if (health<0) throw new ArgumentOutOfRangeException("health < 0");
			if (armor<0)  throw new ArgumentOutOfRangeException("armor < 0");

			Health	=	health;
			Armor	=	armor;
		}

		public PowerupComponent( WeaponType weapon, AmmoType ammo, int count )
		{
			Weapon		=	weapon;
			Ammo		=	ammo;
			AmmoCount	=	count;
		}

		public PowerupComponent( AmmoType ammo, int count )
		{
			Ammo		=	ammo;
			AmmoCount	=	count;
		}
	}
}
