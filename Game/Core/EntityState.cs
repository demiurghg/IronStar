using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {

	public enum AnimState : byte {

		NoAnimation,

		Weapon_Idle,
		Weapon_Warmup,
		Weapon_Recoil,
		Weapon_Cooldown,
		Weapon_Reload,
		Weapon_Overheat,
		Weapon_Drop,
		Weapon_Raise,
		Weapon_NoAmmo,
	}

	[Flags]
	public enum EntityState : int {

		Crouching	=	1 <<  0,
		Zooming		=	1 <<  1,
		HasTraction	=	1 <<  2,

	}
}
