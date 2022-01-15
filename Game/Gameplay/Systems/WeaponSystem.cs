using System;
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

			gs.ForceRefresh();
		}


		void UpdateArmedEntity( IGameState gs, Entity entity, GameTime gameTime, Transform transform, InventoryComponent inventory, WeaponStateComponent wpnState, UserCommandComponent userCmd, CharacterController chctrl, bool isAlive, BobbingComponent bob )
		{
			var povTransform	=	GameUtil.ComputePovTransform( userCmd, transform, chctrl, bob );

			SwitchWeapon( gs, userCmd, inventory, wpnState );

			//	#TODO #WAEPON #GUI -- drop weapon a little while using In-Game GUIs.
			//	#TODO #WAEPON #GUI -- drop weapon when close to wall, but immediatly aim on fire.
			var hide	= userCmd.Action.HasFlag( UserAction.HideWeapon );
			var attack	= userCmd.Action.HasFlag( UserAction.Attack ) && isAlive;
			var throwG	= userCmd.Action.HasFlag( UserAction.ThrowGrenade ) && isAlive;
			var weapon	= Arsenal.Get( wpnState.ActiveWeapon );

			FadeSpread( gameTime, weapon, wpnState );
			AdvanceWeaponTimer( gameTime, weapon, wpnState );
			UpdateWeaponFSM( gameTime, attack, throwG, povTransform, entity, inventory, weapon, wpnState, !isAlive );
		}

		
		bool SwitchWeapon( IGameState gs, UserCommandComponent userCmd, InventoryComponent inventory, WeaponStateComponent wpnState )
		{
			if (userCmd.Weapon!=WeaponType.None)
			{
				if (inventory.HasWeapon(userCmd.Weapon))
				{
					return wpnState.TrySwitchWeapon(userCmd.Weapon);
				}
			}
			else
			{
				var currentWeapon	=	wpnState.PendingWeapon==WeaponType.None ? wpnState.ActiveWeapon : wpnState.PendingWeapon;
				var nextWeapon		=	currentWeapon;

				var next = userCmd.Action.HasFlag(UserAction.WeaponNext);
				var prev = userCmd.Action.HasFlag(UserAction.WeaponPrev);

				if (next || prev)
				{
					do 
					{
						if (next) nextWeapon = Arsenal.Next( nextWeapon );
						if (prev) nextWeapon = Arsenal.Prev( nextWeapon );

						if (WeaponExistsAndHasAmmo(nextWeapon, inventory))
						{
							wpnState.TrySwitchWeapon(nextWeapon);
							break;
						}
					}
					while (nextWeapon!=currentWeapon);
				}
			}

			return false;
		}


		void AutoswitchWeapon( WeaponStateComponent wpnState, InventoryComponent inventory )
		{
			foreach ( var weapon in Misc.GetEnumValues<WeaponType>().Reverse() )
			{
				if (WeaponExistsAndHasAmmo(weapon, inventory))
				{
					wpnState.TrySwitchWeapon(weapon);
					return;
				}
			}
		}

		
		bool WeaponExistsAndHasAmmo( WeaponType weapon, InventoryComponent inventory )
		{
			if (inventory.HasWeapon(weapon))
			{
				var wpnDesc = Arsenal.Get(weapon);

				if (inventory.HasAmmo(wpnDesc.AmmoType))
				{
					return true;
				}
			}

			return false;
		}

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
			if (weapon!=null)
			{
				if (weapon.SpreadMode==SpreadMode.Variable)
				{
					state.Spread *= (float)Math.Pow( SPREAD_FADEOUT, Math.Min(1, gameTime.ElapsedSec) );
				}
			}
		}


		void UpdateWeaponFSM (GameTime gameTime, bool attack, bool throwG, Matrix povTransform, Entity attacker, InventoryComponent inventory, Weapon weapon, WeaponStateComponent state, bool dead )
		{
			var timeout	=	state.Timer <= TimeSpan.Zero;
			var armed	=	weapon != null;

			switch (state.State) 
			{
				case WeaponState.Idle:	
					if (attack && armed) 
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
					if (throwG && armed)
					{
						if (TryConsumeAmmo(gs, inventory, Arsenal.HandGrenade))
						{
							state.State =  WeaponState.PullGrenade;	
							state.Timer += Arsenal.HandGrenade.TimeWarmup;
						}
					}
					if (state.HasPengingWeapon || dead) 
					{
						state.State =  WeaponState.Drop;	
						state.Timer =  armed ? weapon.TimeDrop : TimeSpan.Zero;
					}
					break;

				case WeaponState.PullGrenade:	
					if (timeout) 
					{
						var grenadeThrowTransform = Matrix.Translation(-0.3f,0,0) * povTransform;
						Fire(actualGameTime, Arsenal.HandGrenade, state, grenadeThrowTransform, attacker);

						state.State =	(((state.Counter++)&1)==0) ? WeaponState.Throw : WeaponState.Throw2;
						state.Timer +=	Arsenal.HandGrenade.TimeCooldown;
					}
					break;

				case WeaponState.Warmup:	
					if (timeout) 
					{
						Fire(actualGameTime, weapon, state, povTransform, attacker);

						state.State =	(((state.Counter++)&1)==0) ? WeaponState.Cooldown : WeaponState.Cooldown2;
						state.Timer +=	weapon.TimeCooldown;
					}
					break;

				case WeaponState.Cooldown:	
				case WeaponState.Cooldown2:	
				case WeaponState.Throw:	
				case WeaponState.Throw2:	
					//	fast switch
					if (state.HasPengingWeapon || dead) 
					{
						state.State =  WeaponState.Drop;	
						state.Timer =  armed ? weapon.TimeDrop : TimeSpan.Zero;
					}
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
						state.State	=	WeaponState.Inactive;
						state.Timer =	TimeSpan.Zero;
						state.ActiveWeapon = WeaponType.None;
					}
					break;

				case WeaponState.Inactive:		
					if (state.HasPengingWeapon) 
					{
						var pendingWeapon	=	Arsenal.Get( state.PendingWeapon );
						state.ActiveWeapon	=	state.PendingWeapon;
						state.PendingWeapon	=	WeaponType.None;
						state.State			=	WeaponState.Raise;
						state.Timer			=	pendingWeapon.TimeRaise;
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
						if (PlayerInput.AutoSwitch)
						{
							AutoswitchWeapon( state, inventory );
						}
					}
					break;
			}
		}


		bool TryConsumeAmmo( IGameState gs, InventoryComponent inventory, Weapon weapon )
		{
			return inventory.TryConsumeAmmo( weapon.AmmoType, weapon.AmmoConsumption );
		}


		/// <summary>
		/// 
		/// </summary>
		bool Fire ( GameTime gameTime, Weapon weapon, WeaponStateComponent state, Matrix povTransform, Entity attacker )
		{
			var gs = attacker.gs;

			attacker.GetComponent<NoiseComponent>()?.MakeNoise( weapon.NoiseLevel );

			if (weapon.SpreadMode==SpreadMode.Const) state.Spread = weapon.MaxSpread;

			if (weapon.IsBeamWeapon) 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireBeam( gs, weapon, state, povTransform, attacker );
				}
			} 
			else 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireProjectile( gs, gameTime, weapon, state, povTransform, attacker );
				}
			}

			if (weapon.SpreadMode==SpreadMode.Variable)
			{
				state.Spread	+=	weapon.MaxSpread * SPREAD_INCREMENT;
				state.Spread	=	MathUtil.Clamp( state.Spread, 0, weapon.MaxSpread );
			}

			return true;
		}


		/*-----------------------------------------------------------------------------------------
		 *	Beam weapon :
		-----------------------------------------------------------------------------------------*/

		void FireBeam ( GameState gs, Weapon weapon, WeaponStateComponent state, Matrix povTransform, Entity attacker )
		{
			//Log.Debug("SPREAD: {0}", state.Spread);

			var p	=	povTransform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( povTransform );
			var d	=	-GetFireDirection( q, state.Spread );
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
		void FireProjectile ( IGameState gs, GameTime gameTime, Weapon weapon, WeaponStateComponent state, Matrix povTransform, Entity attacker )
		{
			var attackData	=	new AttackData();
			var transform	=	attacker.GetComponent<Transform>();
			
			attackData.Attacker		=	attacker;
			attackData.DeltaTime	=	gameTime.ElapsedSec;
			attackData.Origin		=	povTransform.TranslationVector;
			attackData.Rotation		=	Quaternion.RotationMatrix( povTransform );
			attackData.Direction	=	-GetFireDirection( attackData.Rotation, state.Spread );
			attackData.Impulse		=	weapon.Impulse;
			attackData.Damage		=	weapon.Damage;

			weapon.ProjectileSpawn( gs, attackData );
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
						var factor	=	MathUtil.Clamp(2 * (radius - dist) / radius, 0, 1);
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
					default: return "bulletHit_metal";
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
