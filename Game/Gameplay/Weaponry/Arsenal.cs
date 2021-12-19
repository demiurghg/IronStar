using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSFactories;

namespace IronStar.Gameplay.Weaponry
{
	static class Arsenal
	{
		readonly static Weapon	machinegun;
		readonly static Weapon	machinegun2;
		readonly static Weapon	shotgun;
		readonly static Weapon	plasmagun;
		readonly static Weapon	rlauncher;
		readonly static Weapon	railgun;

		readonly static Ammo	bullets		=	new Ammo(200, "BULLETS"	);
		readonly static Ammo	shells		=	new Ammo(200, "SHELLS"	);
		readonly static Ammo	cells		=	new Ammo(200, "CELLS"	);
		readonly static Ammo	slugs		=	new Ammo( 50, "SLUGS"	);
		readonly static Ammo	rockets		=	new Ammo( 50, "ROCKETS"	);
		readonly static Ammo	grenades	=	new Ammo( 50, "GRENADES");

		public static readonly Color ColorMachinegun		=	new Color( 250, 80, 20 ); 
		public static readonly Color ColorShotgun			=	new Color( 250, 80, 20 ); 
		public static readonly Color ColorRocketLauncher	=	new Color( 250, 80, 20 ); 
		public static readonly Color ColorRailgun			=	new Color( 107, 136, 255 );
		public static readonly Color ColorPlasmagun			=	new Color( 107, 136, 255 );

		const float ColorIntensity	=	3.0f;

		const float IMPULSE_LIGHT	=	50.0f;
		const float IMPULSE_MEDIUM	=	200.0f;
		const float IMPULSE_HEAVY	=	500.0f;

		static Arsenal()
		{
			machinegun	=	new Weapon("MACHINEGUN")
								.ViewModel	( ColorMachinegun, ColorIntensity, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_view")
								.Ammo		( 1, AmmoType.Bullets )
								.Cooldown	( 100 )
								.Attack		( 7, IMPULSE_LIGHT, 2.0f, SpreadMode.Variable, "machinegunMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			machinegun2	=	new Weapon("MACHINEGUN2")
								.ViewModel	( ColorMachinegun, ColorIntensity, 0.04f, "scenes\\weapon2\\battle_rifle\\battle_rifle_view")
								.Ammo		( 1, AmmoType.Bullets )
								.Cooldown	( 100 )
								.Attack		( 5, IMPULSE_LIGHT, 1.0f, SpreadMode.Variable, "machinegunMuzzle" )
								.Beam		( 1, "*trail_bullet", "bulletHit" )
								;

			shotgun		=	new Weapon("SHOTGUN")
								.ViewModel	( ColorShotgun, ColorIntensity, 0.03f, "scenes\\weapon2\\canister_rifle\\canister_rifle_view")
								.Ammo		( 1, AmmoType.Shells )
								.Cooldown	( 750 )
								.Attack		( 10, IMPULSE_MEDIUM/10, 3.0f, SpreadMode.Const, "shotgunMuzzle" )
								.Beam		( 10, null, "shotgunHit" )
								;

			plasmagun		=	new Weapon("PLASMAGUN")
								.ViewModel	( ColorPlasmagun, ColorIntensity, 0.03f, "scenes\\weapon2\\plasma_rifle\\plasma_rifle_view")
								.Ammo		( 1, AmmoType.Cells )
								.Cooldown	( 100 )
								.Attack		( 7, IMPULSE_LIGHT, 0.0f, SpreadMode.Const, "plasmaMuzzle" )
								.Projectile	( 1, (gs,ad)=> gs.Spawn( new PlasmaFactory(ad) ) )
								;

			rlauncher		=	new Weapon("ROCKET_LAUNCHER")
								.ViewModel	( ColorRocketLauncher, ColorIntensity, 0.03f, "scenes\\weapon2\\rocket_launcher\\rocket_launcher_view")
								.Ammo		( 1, AmmoType.Rockets )
								.Cooldown	( 750 )
								.Attack		( 100, IMPULSE_HEAVY, 0.0f, SpreadMode.Const, "rocketMuzzle" )
								.Projectile	( 1, (gs,ad)=> gs.Spawn( new RocketFactory(ad) ) )
								;

			railgun			=	new Weapon("RAILGUN")
								.ViewModel	( ColorRailgun, ColorIntensity, 0.03f, "scenes\\weapon2\\gauss_rifle\\gauss_rifle_view")
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
				default: return null;
			}
		}


		public static Ammo Get( AmmoType ammoType )
		{
			switch (ammoType)
			{
				case AmmoType.Bullets:	return bullets	;
				case AmmoType.Shells:	return shells	;
				case AmmoType.Cells:	return cells	;
				case AmmoType.Slugs:	return slugs	;
				case AmmoType.Rockets:	return rockets	;
				case AmmoType.Grenades:	return grenades	;
				default: 
					Log.Warning("Bad ammo type " + ammoType.ToString());
					return null;
			}
		}
	}
}
