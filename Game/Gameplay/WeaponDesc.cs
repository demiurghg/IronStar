using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Collection;
using IronStar.SFX2;

namespace IronStar.Gameplay
{
	public sealed class WeaponDesc
	{
		public TimeSpan	TimeWarmup		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeCooldown	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeOverheat	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeReload		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan	TimeDrop		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeRaise		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan	TimeNoAmmo		=	TimeSpan.FromMilliseconds(250);

		public string	BeamHitFX		=	null;
		public string	BeamTrailFX		=	null;
		public string	MuzzleFX		=	null;

		public string	ProjectileClass	=	null;
		public int		ProjectileCount	=	1;
		public int		Damage			=	0;
		public float	Impulse			=	0;
		public float	Spread			=	0;

		public AmmoType	AmmoType		=	AmmoType.Bullets;
		public int		AmmoConsumption	=	1;
		
		public bool		IsBeamWeapon { get { return ProjectileClass==null; } }


		readonly static EnumArray<WeaponType,WeaponDesc>	descs	=	new EnumArray<WeaponType,WeaponDesc>();
		readonly static EnumArray<WeaponType,RenderModel>	models	=	new EnumArray<WeaponType,RenderModel>();


		static WeaponDesc()
		{
			descs[ WeaponType.Machinegun	 ] = BeamWeapon			(   7,   5, 1, 2.0f,	50,	AmmoType.Bullets, "*trail_bullet", "machinegunHit", "machinegunMuzzle" ) );
			descs[ WeaponType.Machinegun2	 ] = BeamWeapon			(   5,  30, 1, 1.0f,	50,	AmmoType.Bullets, "*trail_bullet", "machinegunHit", "machinegunMuzzle" ) );
			descs[ WeaponType.Shotgun		 ] = BeamWeapon			(  10,   1, 10, 3.0f,	750,	AmmoType.Shells, null, "shotgunHit", "shotgunMuzzle" ) );
			descs[ WeaponType.Plasmagun		 ] = ProjectileWeapon	(  10,   5, 50, "PLASMA", AmmoType.Cells, "plasmaMuzzle" ) );
			descs[ WeaponType.RocketLauncher ] = ProjectileWeapon	( 100,  15, 1500, "ROCKET", AmmoType.Rockets, "rocketMuzzle" ) );
			descs[ WeaponType.Railgun		 ] = BeamWeapon			( 100, 250, 1, 0,	1500,	AmmoType.Slugs, "*trail_gauss", "railHit", "railMuzzle" ) );
		}


		public static WeaponDesc BeamWeapon( int damage, float impulse, int count, float spread, int cooldown, AmmoType ammo, string trailFx, string hitFx, string muzzleFx )
		{
			var weapon = new WeaponDesc();

			weapon.Damage			=	damage;
			weapon.Impulse			=	impulse;
			weapon.Spread			=	spread;
			weapon.TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			weapon.AmmoType			=	ammo;
			weapon.BeamTrailFX		=	trailFx;
			weapon.BeamHitFX		=	hitFx;
			weapon.MuzzleFX			=	muzzleFx;
			weapon.ProjectileClass	=	null;
			weapon.ProjectileCount	=	count;

			return weapon;
		}

		
		public static WeaponDesc ProjectileWeapon( int damage, float impulse, int cooldown, string projectile, AmmoType ammo, string muzzleFx )
		{
			var weapon = new WeaponDesc();

			weapon.Damage			=	damage;
			weapon.Impulse			=	impulse;
			weapon.Spread			=	0;
			weapon.TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			weapon.AmmoType		=	ammo;
			weapon.BeamTrailFX		=	null;
			weapon.BeamHitFX		=	null;
			weapon.MuzzleFX			=	muzzleFx;
			weapon.ProjectileClass	=	projectile;
			weapon.ProjectileCount	=	1;

			return weapon;
		}
	}
}
