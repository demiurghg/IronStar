﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {

	public enum InventoryItem : byte {
		Health,
		Armor,
		Ammo_Bullets,
		Ammo_Shells,
		Ammo_Cells,
		Ammo_Rockets,
		Ammo_Grenades,
		Ammo_Slugs,
		Key_Red,
		Key_Yellow,
		Key_Blue,
		PowerCube,
	}


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
		Crouching			=	0x0008,
	}

	public enum DamageType {
		BulletHit,
		RailHit,
		RocketExplosion,
	}

	public enum EntityFX {
		None,
		Highlight,
	}
}
