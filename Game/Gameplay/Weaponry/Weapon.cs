using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.SFX2;

namespace IronStar.Gameplay.Weaponry
{
	public delegate Entity SpawnMethod( Vector3 position, Quaternion rotation, Vector3 direction, float dt, Entity attacker, float damage, float impulse );
	
	public class Weapon
	{
		public string		NiceName		=	null;

		public RenderModel	ViewRenderModel;

		public TimeSpan		TimeWarmup		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeCooldown	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeOverheat	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeReload		=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeDrop		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan		TimeRaise		=	TimeSpan.FromMilliseconds(350);
		public TimeSpan		TimeNoAmmo		=	TimeSpan.FromMilliseconds(250);

		public string		BeamHitFX		=	null;
		public string		BeamTrailFX		=	null;
		public string		MuzzleFX		=	null;

		public SpawnMethod	ProjectileSpawn	=	null;
		public int			ProjectileCount	=	1;

		public int			Damage			=	0;
		public float		Impulse			=	0;
		public float		MaxSpread		=	0;

		public SpreadMode	SpreadMode		=	SpreadMode.Const;
		public float		Spread			=	0;

		public AmmoType		AmmoType		=	AmmoType.Bullets;
		public int			AmmoConsumption	=	1;
		
		public bool			IsBeamWeapon { get { return ProjectileSpawn==null; } }



		public Weapon(string niceName)
		{
			NiceName	=	niceName;
		}


		public Weapon ViewModel(Color color, float intensityEv, float scale, string modelPath)
		{
			ViewRenderModel		=	new RenderModel( modelPath, scale, color, MathUtil.Exp2( intensityEv ), RMFlags.FirstPointView );

			return this;
		}


		public Weapon Projectile( int count, SpawnMethod spawn )
		{
			ProjectileCount	=	count;
			ProjectileSpawn	=	spawn;

			return this;
		}


		public Weapon Beam( int count, string trailFx, string hitFx )
		{
			ProjectileCount	=	count;
			ProjectileSpawn	=	null; // override

			BeamTrailFX		=	trailFx;
			BeamHitFX		=	hitFx;

			return this;
		}


		public Weapon Ammo( int consumption, AmmoType ammoType )
		{
			AmmoConsumption	=	consumption;
			AmmoType		=	ammoType;

			return this;
		}


		public Weapon Attack( int damage, float impulse, float spread, SpreadMode spreadMode, string muzzleFx )
		{
			Damage				=	damage;
			Impulse				=	impulse;
			MaxSpread			=	spread;
			MuzzleFX			=	muzzleFx;

			return this;
		}


		public Weapon Cooldown( int cooldown )
		{
			TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);

			return this;
		}
	}
}
