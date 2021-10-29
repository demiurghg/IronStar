using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Gameplay
{
	public enum WeaponState : byte 
	{
		Idle		,
		Warmup		,
		Cooldown	,
		Cooldown2	,
		Reload		,
		Overheat	,
		Drop		,
		Raise		,
		NoAmmo		,
		Inactive	,
		//Event		,
	}
}
