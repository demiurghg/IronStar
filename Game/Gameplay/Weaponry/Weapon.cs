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
	public struct AttackData
	{
		public Entity		Attacker;
		public float		DeltaTime;
		public Vector3		Origin;
		public Vector3		Direction;
		public Quaternion	Rotation;
		public int			Damage;
		public float		Impulse;
	}

	public delegate Entity SpawnMethod( GameState gs, AttackData attackData );
	
	public class Weapon
	{
		public string		NiceName		{ get; private set; }	=	null;
		public RenderModel	FPVRenderModel	{ get; private set; }	=	null;

		public TimeSpan		TimeWarmup		{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeCooldown	{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeOverheat	{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeReload		{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeDrop		{ get; private set; }	=	TimeSpan.FromMilliseconds(350);
		public TimeSpan		TimeRaise		{ get; private set; }	=	TimeSpan.FromMilliseconds(350);
		public TimeSpan		TimeNoAmmo		{ get; private set; }	=	TimeSpan.FromMilliseconds(250);

		public string		BeamHitFX		{ get; private set; }	=	null;
		public string		BeamTrailFX		{ get; private set; }	=	null;
		public string		MuzzleFX		{ get; private set; }	=	null;

		public SpawnMethod	ProjectileSpawn	{ get; private set; }	=	null;
		public int			ProjectileCount	{ get; private set; }	=	1;

		public int			Damage			{ get; private set; }	=	0;
		public float		Impulse			{ get; private set; }	=	0;
		public float		MaxSpread		{ get; private set; }	=	0;

		public SpreadMode	SpreadMode		{ get; private set; }	=	SpreadMode.Const;
		public float		Spread			{ get; private set; }	=	0;

		public AmmoType		AmmoType		{ get; private set; }	=	AmmoType.Bullets;
		public int			AmmoConsumption	{ get; private set; }=	1;
		
		public bool			IsBeamWeapon { get { return ProjectileSpawn==null; } }



		public Weapon(string niceName)
		{
			NiceName	=	niceName;
		}


		public Weapon ViewModel(Color color, float intensityEv, float scale, string modelPath)
		{
			FPVRenderModel		=	new RenderModel( modelPath, scale, color, MathUtil.Exp2( intensityEv ), RMFlags.FirstPointView );

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
			Damage		=	damage;
			Impulse		=	impulse;
			MaxSpread	=	spread;
			MuzzleFX	=	muzzleFx;
			SpreadMode	=	spreadMode;

			return this;
		}


		public Weapon Cooldown( int cooldown )
		{
			TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);

			return this;
		}
	}
}
