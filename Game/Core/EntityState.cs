using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {

	public enum WeaponState : byte {
		Inactive	=	0x00,
		Idle		=	0x01,
		Warmup		=	0x02,
		Cooldown	=	0x03,
		Reload		=	0x04,
		Overheat	=	0x05,
		Drop		=	0x06,
		Raise		=	0x07,
		NoAmmo		=	0x08,
		Event		=	0x80,
	}


	[Flags]
	public enum EntityState : int {

		Crouching		=	0x00000001,
		Zooming			=	0x00000002,
		HasTraction		=	0x00000004,

		StrafeLeft		=	0x00000010,
		StrafeRight		=	0x00000020,
		TurnLeft		=	0x00000040,
		TurnRight		=	0x00000080,

		Weapon_States	=	0x000FFE00,	//	mask for all weapon relates states (weapon_event is not included!)
		Weapon_Event	=	0x00000100,	//	weapon event flag, toggled on each weapon state change

		Weapon_Inactive	=	0x00000200,
		Weapon_Reserve0	=	0x00000400,
		Weapon_Reserve1	=	0x00000800,
		Weapon_Idle		=	0x00001000,
		Weapon_Warmup	=	0x00002000,
		Weapon_Cooldown	=	0x00004000,
		Weapon_Reload	=	0x00008000,
		Weapon_Overheat	=	0x00010000,
		Weapon_Drop		=	0x00020000,
		Weapon_Raise	=	0x00040000,
		Weapon_NoAmmo	=	0x00080000,


	}
}
