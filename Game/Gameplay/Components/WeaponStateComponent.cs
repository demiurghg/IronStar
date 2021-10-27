using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay.Components
{
	public class WeaponStateComponent : Component
	{
		public WeaponState	State	=	WeaponState.Idle;
		public TimeSpan		Timer	=	TimeSpan.Zero;
		public float		Spread	=	0;
		public int			Counter	=	0;

		public WeaponType	ActiveWeapon	=	WeaponType.Machinegun;
		public WeaponType	PendingWeapon	=	WeaponType.None;

		public bool HasPengingWeapon { get { return PendingWeapon!=WeaponType.None; } }
	}
}
