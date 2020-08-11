using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class WeaponComponent : IComponent
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

		public string	AmmoClass		=	"";
		public int		AmmoConsumption	=	1;
		

		public void Load( GameState gs, Stream stream )	{}
		public void Save( GameState gs, Stream stream )	{}

		public static WeaponComponent BeamWeapon( int damage, float impulse, int count, float spread, int cooldown, string ammo, string trailFx, string hitFx, string muzzleFx )
		{
			var weapon = new WeaponComponent();

			weapon.Damage			=	damage;
			weapon.Impulse			=	impulse;
			weapon.Spread			=	spread;
			weapon.TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			weapon.AmmoClass		=	ammo;
			weapon.BeamTrailFX		=	trailFx;
			weapon.BeamHitFX		=	hitFx;
			weapon.MuzzleFX			=	muzzleFx;
			weapon.ProjectileClass	=	null;
			weapon.ProjectileCount	=	count;

			return weapon;
		}

		
		public static WeaponComponent ProjectileWeapon( int damage, float impulse, int cooldown, string projectile, string ammo, string muzzleFx )
		{
			var weapon = new WeaponComponent();

			weapon.Damage			=	damage;
			weapon.Impulse			=	impulse;
			weapon.Spread			=	0;
			weapon.TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			weapon.AmmoClass		=	ammo;
			weapon.BeamTrailFX		=	null;
			weapon.BeamHitFX		=	null;
			weapon.MuzzleFX			=	muzzleFx;
			weapon.ProjectileClass	=	projectile;
			weapon.ProjectileCount	=	1;

			return weapon;
		}
	}
}
