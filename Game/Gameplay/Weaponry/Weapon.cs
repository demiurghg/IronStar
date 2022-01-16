using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.Gameplay.Components;
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

	public delegate Entity SpawnMethod( IGameState gs, AttackData attackData );
	
	public class Weapon
	{
		public string		NiceName		{ get; private set; }	=	null;
		public RenderModel	FPVRenderModel	{ get; private set; }	=	null;

		public TimeSpan		TimeWarmup		{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeCooldown	{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeOverheat	{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeReload		{ get; private set; }	=	TimeSpan.FromMilliseconds(0);
		public TimeSpan		TimeDrop		{ get; private set; }	=	TimeSpan.FromMilliseconds(250);
		public TimeSpan		TimeRaise		{ get; private set; }	=	TimeSpan.FromMilliseconds(150);
		public TimeSpan		TimeNoAmmo		{ get; private set; }	=	TimeSpan.FromMilliseconds(250);

		public string		BeamHitFX		{ get; private set; }	=	null;
		public string		BeamTrailFX		{ get; private set; }	=	null;
		public string		MuzzleFX		{ get; private set; }	=	null;

		public SpawnMethod	ProjectileSpawn	{ get; private set; }	=	null;
		public int			ProjectileCount	{ get; private set; }	=	1;

		public int			Damage			{ get; private set; }	=	0;
		public float		Impulse			{ get; private set; }	=	0;
		public float		NoiseLevel		{ get; private set; }	=	0;

		public SpreadMode	SpreadMode			{ get; private set; }	=	SpreadMode.Const;
		public float		MaxSpread			{ get; private set; }	=	0;
		public float		MaxSpreadShots		{ get; private set; }	=	0;
		public float		SpreadRecoveryTime	{ get; private set; }	=	0;

		public AmmoType		AmmoType		{ get; private set; }	=	AmmoType.Bullets;
		public int			AmmoConsumption	{ get; private set; }=	1;
		
		public bool			IsBeamWeapon { get { return ProjectileSpawn==null; } }


		/*-----------------------------------------------------------------------------------------
		 *	Copnstruction methods :
		-----------------------------------------------------------------------------------------*/

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


		public Weapon Attack( int damage, float impulse, string muzzleFx )
		{
			Damage		=	damage;
			Impulse		=	impulse;
			MuzzleFX	=	muzzleFx;

			return this;
		}


		public Weapon Spread( float spread )
		{
			MaxSpread			=	spread;
			SpreadMode			=	SpreadMode.Const;
			MaxSpreadShots		=	1;
			SpreadRecoveryTime	=	0;
			return this;
		}


		public Weapon Spread( float spread, float maxShots, float recoveryTime )
		{
			MaxSpread			=	spread;
			SpreadMode			=	SpreadMode.Variable;
			MaxSpreadShots		=	maxShots;
			SpreadRecoveryTime	=	recoveryTime;
			return this;
		}


		public Weapon Cooldown( int cooldown )
		{
			TimeCooldown		=	TimeSpan.FromMilliseconds(cooldown);
			return this;
		}


		public Weapon Warmup( int cooldown )
		{
			TimeWarmup		=	TimeSpan.FromMilliseconds(cooldown);
			return this;
		}


		public Weapon Noise( float noise )
		{
			NoiseLevel	=	noise;
			return this;
		}


		/*-----------------------------------------------------------------------------------------
		 *	Utility methods :
		-----------------------------------------------------------------------------------------*/

		public void ResetSpread( WeaponStateComponent state )
		{
			if ( SpreadMode==SpreadMode.Const)
			{
				state.Spread	=	MaxSpread;
			}
			else if ( SpreadMode==SpreadMode.Variable)
			{
				state.Spread	=	0;
			}
			else
			{
				state.Spread = 0;
			}
		}

		public void IncreaseSpread( WeaponStateComponent state )
		{
			if ( SpreadMode==SpreadMode.Const)
			{
				state.Spread	=	MaxSpread;
			}
			else if ( SpreadMode==SpreadMode.Variable)
			{
				var addition	=	MaxSpread / SpreadRecoveryTime * (float)TimeCooldown.TotalSeconds;
				var increment	=	MaxSpread / MaxSpreadShots;
				state.Spread	+=	increment + addition;
				state.Spread	=	MathUtil.Clamp( state.Spread, 0, MaxSpread + addition );
			}
			else
			{
				state.Spread = 0;
			}
		}


		public void DecreaseSpread( GameTime gameTime, WeaponStateComponent state )
		{
			if ( SpreadMode==SpreadMode.Const)
			{
				state.Spread	=	MaxSpread;
			}
			else if (SpreadMode==SpreadMode.Variable)
			{								
				if (state.Spread>0)
				{
					float dt = gameTime.ElapsedSec;
					state.Spread = Math.Max( 0, state.Spread - dt / SpreadRecoveryTime * MaxSpread );
				}
			}
			else
			{
				state.Spread = 0;
			}
		}
	}
}
