using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;

namespace IronStar.Gameplay.Weaponry
{
	static class Arsenal
	{
		readonly static Weapon machinegun;
		readonly static Weapon machinegun2;
		readonly static Weapon shotgun;
		readonly static Weapon plasmagun;
		readonly static Weapon rlauncher;
		readonly static Weapon railgun;

		const float IMPULSE_LIGHT	=	5.0f;
		const float IMPULSE_MEDIUM	=	200.0f;
		const float IMPULSE_HEAVY	=	500.0f;

		static Arsenal()
		{
			machinegun	=	new Weapon("MACHINEGUN")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_view")
								.Ammo		( 1, AmmoType.Bullets )
								.Cooldown	( 100 )
								.Attack		( 7, IMPULSE_LIGHT, 2.0f, SpreadMode.Variable, "machinegunMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			machinegun2	=	new Weapon("MACHINEGUN2")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\battle_rifle\\battle_rifle_view")
								.Ammo		( 1, AmmoType.Bullets )
								.Cooldown	( 100 )
								.Attack		( 5, IMPULSE_LIGHT, 1.0f, SpreadMode.Variable, "machinegunMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			shotgun		=	new Weapon("SHOTGUN")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\canister_rifle\\canister_rifle_view")
								.Ammo		( 1, AmmoType.Shells )
								.Cooldown	( 750 )
								.Attack		( 7, IMPULSE_MEDIUM, 3.0f, SpreadMode.Const, "shotgunMuzzle" )
								.Beam		( 1, null, "shotgunHit" )
								;

			plasmagun		=	new Weapon("PLASMAGUN")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_view")
								.Ammo		( 1, AmmoType.Cells )
								.Cooldown	( 100 )
								.Attack		( 7, IMPULSE_LIGHT, 0.0f, SpreadMode.Const, "plasmaMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			rlauncher		=	new Weapon("ROCKET_LAUNCHER")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_view")
								.Ammo		( 1, AmmoType.Cells )
								.Cooldown	( 100 )
								.Attack		( 7, IMPULSE_HEAVY, 0.0f, SpreadMode.Const, "machinegunMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			railgun			=	new Weapon("RAILGUN")
								.ViewModel	( Color.Orange, 3, 0.03f, "scenes\\weapon2\\gauss_rifle\\gauss_rifle_view")
								.Ammo		( 1, AmmoType.Slugs )
								.Cooldown	( 1500 )
								.Attack		( 100, IMPULSE_HEAVY, 0, SpreadMode.Const, "railMuzzle" )
								.Beam		( 1, "*trail_gauss", "railHit" )
								;
		}


		public static Weapon Get( WeaponType weaponType )
		{
			switch (weaponType)
			{
				case WeaponType.Machinegun:		return machinegun;
				case WeaponType.Machinegun2:	return machinegun2;
				case WeaponType.Shotgun:		return shotgun;
				case WeaponType.Plasmagun:		return plasmagun;
				case WeaponType.RocketLauncher:	return rlauncher;
				case WeaponType.Railgun:		return railgun;
				default: 
					Log.Warning("Bad weapon type " + weaponType.ToString());
					return machinegun;
					break;
			}
		}
	}
}
