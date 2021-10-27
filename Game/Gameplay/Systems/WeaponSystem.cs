﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using Fusion;
using IronStar.ECSPhysics;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.AI;
using System.Runtime.CompilerServices;
using IronStar.SFX;
using BEPUutilities.Threading;
using IronStar.ECSFactories;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		const float BEAM_RANGE			=	8192;
		const float SPREAD_INCREMENT	=	0.2f;
		const float SPREAD_FADEOUT		=	0.2f;

		Random rand = new Random();

		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }
		public readonly PhysicsCore physics;
		public readonly FXPlayback fxPlayback;
		public readonly IGameState gs;


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedEntityAspect	=	new Aspect().Include<InventoryComponent,WeaponStateComponent,UserCommandComponent,CharacterController>()
													.Include<Transform>();

		GameTime actualGameTime;


		public WeaponSystem( IGameState gs, PhysicsCore physics, FXPlayback fxPlayback )
		{
			this.gs			=	gs;
			this.physics	=	physics;
			this.fxPlayback	=	fxPlayback;
		}


		public void Update( IGameState gs, GameTime gameTime )
		{
			actualGameTime	=	gameTime;

			var entities	=	gs.QueryEntities( armedEntityAspect );
			int msecs		=	gameTime.Milliseconds;
			var msec		=	GameTime.MSec1;

			foreach ( var entity in entities )
			{
				var transform	=	entity.GetComponent<Transform>();
				var inventory	=	entity.GetComponent<InventoryComponent>();
				var wpnState	=	entity.GetComponent<WeaponStateComponent>();
				var userCmd		=	entity.GetComponent<UserCommandComponent>();
				var chctrl		=	entity.GetComponent<CharacterController>();
				var health		=	entity.GetComponent<HealthComponent>();
				var bob			=	entity.GetComponent<BobbingComponent>();
				var isAlive		=	health == null ? true : health.Health > 0;

				for (int i=0; i<msecs; i++)
				{
					UpdateArmedEntity( gs, entity, msec, transform, inventory, wpnState, userCmd, chctrl, isAlive, bob );
				}
			}
		}


		void UpdateArmedEntity( IGameState gs, Entity entity, GameTime gameTime, Transform transform, InventoryComponent inventory, WeaponStateComponent wpnState, UserCommandComponent userCmd, CharacterController chctrl, bool isAlive, BobbingComponent bob )
		{
			var povTransform	=	GameUtil.ComputePovTransform( userCmd, transform, chctrl, bob );

			if (inventory.HasPendingWeapon && inventory.ActiveWeapon==null)
			{
				inventory.FinalizeWeaponSwitch();
			}

			//	tell inventory to switch to another weapon :
			SwitchWeapon( gs, userCmd, inventory, wpnState );

			if (!isAlive)
			{
				/*wpnState.SwitchWeapon(inventory, WeaponType.None);
				wpnState.FinalizeWeaponSwitch();*/
			}
			else
			{			
				var attack	= userCmd.Action.HasFlag( UserAction.Attack );
				var weapon	= Arsenal.Get( wpnState.ActiveWeapon );;

				FadeSpread( gameTime, weapon, wpnState );
				AdvanceWeaponTimer( gameTime, weapon, wpnState );
				UpdateWeaponFSM( gameTime, attack, povTransform, entity, inventory, weapon, wpnState );
			}
		}

		
		bool SwitchWeapon( IGameState gs, UserCommandComponent userCmd, InventoryComponent inventory, WeaponStateComponent wpnState )
		{
			if (userCmd.Weapon!=WeaponType.None)
			{
				#warning CHECK INVENTORY HAS GIVEN WEAPON
				if (true /* INVENTORY HAS GIVEN WEAPON */)
				{
					if (wpnState.ActiveWeapon!=userCmd.Weapon)
					{
						wpnState.PendingWeapon	=	userCmd.Weapon;
						return true;
					}
				}
			}

			return false;
		}


		//	#TODO #REFACTOR -- move to GameUtil
		/*-----------------------------------------------------------------------------------------------
		 * Weapon state
		-----------------------------------------------------------------------------------------------*/
		
		void AdvanceWeaponTimer( GameTime gameTime, Weapon weapon, WeaponStateComponent state )
		{
			if ( state.Timer > TimeSpan.Zero ) 
			{
				state.Timer = state.Timer - gameTime.Elapsed;
			}
		}


		void FadeSpread( GameTime gameTime, Weapon weapon, WeaponStateComponent state  )
		{
			if (weapon.SpreadMode==SpreadMode.Variable)
			{
				state.Spread *= (float)Math.Pow( SPREAD_FADEOUT, Math.Min(1, gameTime.ElapsedSec) );
			}
		}


		void UpdateWeaponFSM (GameTime gameTime, bool attack, Matrix povTransform, Entity attacker, InventoryComponent inventory, Weapon weapon, WeaponStateComponent state )
		{
			var timeout	=	state.Timer <= TimeSpan.Zero;

			switch (state.State) 
			{
				case WeaponState.Idle:	
					if (attack) 
					{
						if (TryConsumeAmmo(gs, inventory, weapon)) 
						{
							state.State =  WeaponState.Warmup;	
							state.Timer += weapon.TimeWarmup;
						} 
						else 
						{
							state.State =  WeaponState.NoAmmo;	
							state.Timer += weapon.TimeNoAmmo;
						}
					}
					if (state.HasPengingWeapon) 
					{
						state.State =  WeaponState.Drop;	
						state.Timer =  weapon.TimeDrop;
					}
					break;

				case WeaponState.Warmup:	
					if (timeout) 
					{
						Fire(actualGameTime, weapon, povTransform, attacker);

						state.Counter++;
						
						if ((state.Counter&1)==0) 
						{
							state.State = WeaponState.Cooldown;	
						} 
						else 
						{
							state.State = WeaponState.Cooldown2;	
						}

						state.Timer += weapon.TimeCooldown;
					}
					break;


				case WeaponState.Cooldown:	
					if (timeout) 
					{
						state.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Cooldown2:	
					if (timeout) 
					{
						state.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Reload:		
					break;

				case WeaponState.Overheat:		
					break;

				case WeaponState.Drop:	
					if (timeout) 
					{
						state.ActiveWeapon	=	state.PendingWeapon;
						state.PendingWeapon	=	WeaponType.None;
						state.State			=	WeaponState.Raise;
						state.Timer			=	TimeSpan.Zero;
					}
					break;

				case WeaponState.Raise:		
					if (timeout) 
					{
						state.State = WeaponState.Idle;
						state.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.NoAmmo:		
					if (timeout) 
					{
						state.State = WeaponState.Idle;
						state.Timer = TimeSpan.Zero;
					}
					break;
			}
		}


		AmmoComponent GetAmmo( IGameState gs, InventoryComponent inventory, WeaponComponent weapon )
		{
			AmmoComponent ammo;
			NameComponent name;
			
			inventory.FindItem<AmmoComponent,NameComponent>( gs, (a,n) => n.Name == weapon.AmmoClass, out ammo, out name );

			return ammo;
		}


		bool TryConsumeAmmo( IGameState gs, InventoryComponent inventory, Weapon weapon )
		{
			return true;
			
			#warning TRY_CONSUME_AMMO_!_!_!
			/*if (inventory.Flags.HasFlag(InventoryFlags.InfiniteAmmo))
			{
				return true;
			}

			var ammo		=	GetAmmo( gs, inventory, weapon );

			if (ammo==null) 
			{
				return false;
			}

			if (ammo.Count >= weapon.AmmoConsumption)
			{
				ammo.Count -= weapon.AmmoConsumption;
				return true;
			}
			else
			{
				return false;
			}			*/
		}


		/// <summary>
		/// 
		/// </summary>
		bool Fire ( GameTime gameTime, Weapon weapon, Matrix povTransform, Entity attacker )
		{
			var gs = attacker.gs;

			if (weapon.SpreadMode==SpreadMode.Const) weapon.Spread = weapon.MaxSpread;

			if (weapon.IsBeamWeapon) 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireBeam( gs, weapon, povTransform, attacker );
				}
			} 
			else 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireProjectile( gs, gameTime, weapon, povTransform, attacker );
				}
			}

			if (weapon.SpreadMode==SpreadMode.Variable)
			{
				weapon.Spread	+=	weapon.MaxSpread * SPREAD_INCREMENT;
				weapon.Spread	=	MathUtil.Clamp( weapon.Spread, 0, weapon.MaxSpread );
			}

			return true;
		}


		/*-----------------------------------------------------------------------------------------
		 *	Beam weapon :
		-----------------------------------------------------------------------------------------*/

		void FireBeam ( GameState gs, Weapon weapon, Matrix povTransform, Entity attacker )
		{
			var p	=	povTransform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( povTransform );
			var d	=	-GetFireDirection( q, weapon.Spread );
			var ray	=	new Ray(p,d);

			physics.Raycast( ray, BEAM_RANGE, new BeamRaycastCallback( gs, ray, attacker, weapon ), RaycastOptions.SortResults );
		}


		class BeamRaycastCallback : IRaycastCallback<bool>
		{
			readonly Ray ray;
			readonly Entity attacker;
			readonly Weapon weapon;
			readonly GameState gs;
			bool hitSomething = false;
			Vector3 hitLocation;

			public BeamRaycastCallback( GameState gs, Ray ray, Entity attacker, Weapon weapon )
			{
				this.ray		=	ray;
				this.attacker	=	attacker;
				this.weapon		=	weapon;
				this.gs			=	gs;
				hitSomething	=	false;
				hitLocation		=	ray.Position + ray.Direction * BEAM_RANGE / 16;
			}

			public void Begin( int count ) {}

			public bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic )
			{
				if (entity==attacker) return false;

				hitSomething = true;
				hitLocation	 = location;

				var ws = gs.GetService<WeaponSystem>();

				ws.InflictDamage( attacker, entity, weapon.Damage, weapon.Impulse, location, ray.Direction, normal, weapon.BeamHitFX );

				return true;
			}

			public bool End() 
			{
				//	run trail FX:
				var beamOrigin	 =	ray.Position;
				var beamVelocity =	hitLocation - ray.Position;
				var basis		=	MathUtil.ComputeAimedBasis( ray.Direction );

				SFX.FXPlayback.SpawnFX(	gs, weapon.BeamTrailFX, beamOrigin, beamVelocity, Quaternion.RotationMatrix(basis) );

				return true;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Projectile weapon :
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( GameState gs, GameTime gameTime, Weapon weapon, Matrix povTransform, Entity attacker )
		{
			var dt	=	gameTime.ElapsedSec;
			var t	=	attacker.GetComponent<Transform>();
			var p	=	povTransform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( povTransform );
			var d	=	-GetFireDirection( q, weapon.MaxSpread );

			weapon.ProjectileSpawn( p, q, d, dt, attacker, weapon.Damage, weapon.Impulse );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Damage and explosions :
		-----------------------------------------------------------------------------------------*/

		public void InflictDamage( Entity attacker, Entity target, int damage, float impulse, Vector3 location, Vector3 direction, Vector3 normal, string fx )
		{
			if (target!=null)
			{
				direction.Normalize();
				physics.ApplyImpulse( target, location, direction * impulse );
				HealthSystem.ApplyDamage( target, damage, attacker );

				if (fx!=null)
				{
					var material	=	MaterialComponent.GetMaterial( target );
					var hitFx		=	GetHitFXName( fx, material ); 
					SFX.FXPlayback.AttachFX( gs, target, hitFx, location, normal );
				}
			}
			else
			{
				if (fx!=null)
				{
					FXPlayback.SpawnFX( gs, fx, location, normal );
				}
			}
		}

		
		public void Explode ( Entity attacker, Entity target, int damage, float impulse, float radius, Vector3 location, Vector3 normal, string fx )
		{
			//	make splash damage :
			if (radius>0) 
			{
				var overlapCallback = new ExplodeOverlapCallback( this, attacker, target, location, damage, impulse, radius );
				physics.Overlap( location, radius, overlapCallback );
			}

			if (fx!=null)
			{
				FXPlayback.SpawnFX( gs, fx, location, normal );
			}
		}


		class ExplodeOverlapCallback : IRaycastCallback<bool>
		{
			Vector3	origin;
			int		damage;
			float	impulse;
			float	radius;
			Entity	attacker;
			Entity	target;
			WeaponSystem ws;

			public ExplodeOverlapCallback( WeaponSystem ws, Entity attacker, Entity target, Vector3 origin, int damage, float impulse, float radius )
			{
				this.ws			=	ws;
				this.attacker	=	attacker;
				this.target		=	target	;
				this.origin		=	origin	;
				this.damage		=	damage	;
				this.impulse	=	impulse	;
				this.radius		=	radius	;
			}

			public void Begin( int count ) {}

			public bool End() { return false; }
			
			public bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic )
			{
				if (entity!=null && entity!=target)
				{
					//	overlap test always gives result on the surface of the sphere...
					var transform	=	entity.GetComponent<Transform>();

					//	sometimes transform is null
					if (transform!=null)
					{
						location	=	transform.Position;
						var delta	=	location - origin;
						var dist	=	delta.Length() + 0.00001f;
						var ndir	=	delta / dist;
						var factor	=	MathUtil.Clamp((radius - dist) / radius, 0, 1);
						var imp		=	factor * impulse;
						var loc		=	location + MathUtil.Random.UniformRadialDistribution(0.3f, 0.3f);
						var dmg		=	(short)( factor * damage );

						ws.InflictDamage( attacker, entity, dmg, imp, loc, ndir, -ndir, null );
					}
				}

				return false;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Weapon utils :
		-----------------------------------------------------------------------------------------*/

		static string GetHitFXName( string fxName, MaterialType surface )
		{
			if (fxName=="bulletHit") 
			{
				switch (surface)
				{
					case MaterialType.Metal	: return "bulletHit_metal";
					case MaterialType.Sand	: return "bulletHit_metal";
					case MaterialType.Rock	: return "bulletHit_metal";
					case MaterialType.Flesh	: return "bulletHit_flesh";
					default: return fxName;
				}
			}
			else if (fxName=="shotgunHit") 
			{
				switch (surface)
				{
					case MaterialType.Metal	: return "shotgunHit_metal";
					case MaterialType.Sand	: return "shotgunHit_metal";
					case MaterialType.Rock	: return "shotgunHit_metal";
					case MaterialType.Flesh	: return "shotgunHit_flesh";
					default: return fxName;
				}
			}
			else
			{
				return fxName;
			}
		}


		Vector3 GetFireDirection ( Quaternion rotation, float spreadAngle )
		{ 
			var spreadVector	= GetSpreadVector( spreadAngle );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix ).Normalized();
		}


		Vector3 GetSpreadVector ( float spreadAngle )
		{
			var randomAngle	 = MathUtil.DegreesToRadians( spreadAngle );

			var theta	=	rand.NextFloat( 0, MathUtil.TwoPi );
			var tMin	=	(float)Math.Cos( randomAngle );
			var t		=	rand.NextFloat( tMin, 1 );
			var phi		=	(float)Math.Acos( t );
			var sinPhi	=	(float)Math.Sin( phi );

			var x		=	(float)( Math.Sin( phi ) * Math.Cos( theta ) );
			var y		=	(float)( Math.Sin( phi ) * Math.Sin( theta ) );
			var z		=	-(float)( Math.Cos( phi ) ); // because forward is -Z

			return new Vector3( x, y, -z );
		}
	}
}
