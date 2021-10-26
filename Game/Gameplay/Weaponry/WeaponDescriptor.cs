using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay.Weaponry
{
	public class WeaponDescriptor
	{
		public readonly TimeSpan	TimeWarmup			=	TimeSpan.FromMilliseconds(0);
		public readonly TimeSpan	TimeCooldown		=	TimeSpan.FromMilliseconds(0);
		public readonly TimeSpan	TimeOverheat		=	TimeSpan.FromMilliseconds(0);
		public readonly TimeSpan	TimeReload			=	TimeSpan.FromMilliseconds(0);
		public readonly TimeSpan	TimeDrop			=	TimeSpan.FromMilliseconds(350);
		public readonly TimeSpan	TimeRaise			=	TimeSpan.FromMilliseconds(350);
		public readonly TimeSpan	TimeNoAmmo			=	TimeSpan.FromMilliseconds(250);

		public readonly string		BeamHitFX			=	null;
		public readonly string		BeamTrailFX			=	null;
		public readonly string		MuzzleFX			=	null;

		public readonly IFactory	ProjectileFactory	=	null;
		public readonly int			ProjectileCount		=	1;

		public readonly int			Damage				=	0;
		public readonly float		Impulse				=	0;
		public readonly float		MaxSpread			=	0;

		public readonly SpreadMode	SpreadMode			=	SpreadMode.Const;
		public readonly float		Spread				=	0;

		public readonly string		AmmoClass			=	"";
		public readonly int			AmmoConsumption		=	1;
		
		public bool		IsBeamWeapon { get { return ProjectileFactory==null; } }


		public WeaponDescriptor( int damage, float impulse, int count, float spread, SpreadMode spreadMode, int cooldown, string ammo, string trailFx, string hitFx, string muzzleFx )
		{
			Damage				=	damage;
			Impulse				=	impulse;
			MaxSpread			=	spread;
			TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			AmmoClass			=	ammo;
			BeamTrailFX			=	trailFx;
			BeamHitFX			=	hitFx;
			MuzzleFX			=	muzzleFx;
			ProjectileFactory	=	null;
			ProjectileCount		=	count;
			SpreadMode			=	spreadMode;
		}

		
		public WeaponDescriptor( int damage, float impulse, int cooldown, IFactory projectileFactory, string ammo, string muzzleFx )
		{
			Damage				=	damage;
			Impulse				=	impulse;
			MaxSpread			=	0;
			TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			AmmoClass			=	ammo;
			BeamTrailFX			=	null;
			BeamHitFX			=	null;
			MuzzleFX			=	muzzleFx;
			ProjectileFactory	=	projectileFactory;
			ProjectileCount		=	1;
		}
	}
}
