using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Gameplay
{
	public enum WeaponState : byte 
	{
		Inactive	=	0x00,
		Idle		=	0x01,
		Warmup		=	0x02,
		Cooldown	=	0x03,
		Cooldown2	=	0x04,
		Reload		=	0x05,
		Overheat	=	0x06,
		Drop		=	0x07,
		Raise		=	0x08,
		NoAmmo		=	0x09,
		Event		=	0x80,
	}
}
