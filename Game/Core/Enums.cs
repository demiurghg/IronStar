using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {


	[Flags]
	public enum UserAction : byte {

		None			=	0x00,
		Zoom			=	0x01,
		Attack			=	0x02,
		Use				=	0x04,
		SwitchWeapon	=	0x08,
		ReloadWeapon	=	0x10,
		ThrowGrenade	=	0x20,
		MeleeAtack		=	0x40,
		Jump			=	0x80,
	}	



	[Flags]
	public enum EntityState : short {
		None				=	0x0000,
		HasTraction			=	0x0001,
		CameraEntity		=	0x0002,
		UseInlineModel		=	0x0004,
		Crouching			=	0x0008,
		PrimaryWeapon		=	0x0010,
		SecondaryWeapon		=	0x0020,
	}



	public enum Inventory : byte {

		Health,
		Armor,

		Weapon_Machinegun,
		Weapon_Shotgun,
		Weapon_GaussRifle,
		Weapon_RocketLauncher,
		Weapon_Plasamgun,

		Ammo_Bullets,
		Ammo_Shells,
		Ammo_Slugs,
		Ammo_Rockets,
		Ammo_Cells,

		Ammo_Grenades,

		Key_XXX,
		Powerup_XXX,

		Max,
	}



	public enum DamageType {
		BulletHit,
		RailHit,
		RocketExplosion,
	}
}
