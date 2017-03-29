using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {


	[Flags]
	public enum UserCtrlFlags : int {

		None			=	0,

		Forward			=	1 << 0,
		Backward		=	1 << 1,
		StrafeLeft		=	1 << 2,
		StrafeRight		=	1 << 3,
		Crouch			=	1 << 4,
		Jump			=	1 << 5,
		Zoom			=	1 << 6,
		Attack			=	1 << 7,

		Use				=	1 << 8,

		SwitchWeapon	=	1 << 16,
		ReloadWeapon	=	1 << 17,
		ThrowGrenade	=	1 << 18,
		MeleeAtack		=	1 << 19,

        DrawDebugGrid   =   1 << 20
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



	public enum WeaponType : byte {
		None,
		Machinegun,
		Shotgun,
		Plasmagun,
		RocketLauncher,
		GaussRifle,
	}



	public enum Inventory : byte {

		Health,
		Armor,
		Countdown,

		Machinegun,
		Shotgun,
		SuperShotgun,
		GrenadeLauncher,
		RocketLauncher,
		HyperBlaster,
		Chaingun,
		Railgun,
		BFG,
		
		Bullets,
		Shells,
		Grenades,
		Rockets,
		Cells,
		Slugs,

		WeaponCooldown,
		QuadDamage,

		Max,
	}



	public enum DamageType {
		BulletHit,
		RailHit,
		RocketExplosion,
	}
}
