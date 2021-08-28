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

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		const float BEAM_RANGE			=	8192;
		const float SPREAD_INCREMENT	=	0.2f;
		const float SPREAD_FADEOUT		=	0.2f;

		Random rand = new Random();

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }
		public readonly PhysicsCore physics;
		public readonly FXPlayback fxPlayback;
		public readonly GameState gs;


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedEntityAspect	=	new Aspect().Include<InventoryComponent,UserCommandComponent,CharacterController>()
													.Include<KinematicState>();

		GameTime actualGameTime;


		public WeaponSystem( GameState gs, PhysicsCore physics, FXPlayback fxPlayback )
		{
			this.gs			=	gs;
			this.physics	=	physics;
			this.fxPlayback	=	fxPlayback;
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			actualGameTime = gameTime;

			int msecs = gameTime.Milliseconds;
			for (int i=0; i<msecs; i++)
			{
				UpdateInternal( gs, GameTime.MSec1 );
			}
		}

		
		void UpdateInternal( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities( armedEntityAspect );

			foreach ( var entity in entities )
			{
				var transform	=	entity.GetComponent<KinematicState>();
				var inventory	=	entity.GetComponent<InventoryComponent>();
				var userCmd		=	entity.GetComponent<UserCommandComponent>();
				var chctrl		=	entity.GetComponent<CharacterController>();
				var health		=	entity.GetComponent<HealthComponent>();

				var isAlive		=	health==null ? true : health.Health>0;

				var povTransform	=	userCmd.ComputePovTransform(transform.Position, chctrl.PovOffset);

				if (inventory.HasPendingWeapon && inventory.ActiveWeapon==null)
				{
					inventory.FinalizeWeaponSwitch();
				}

				//	tell inventory to switch to another weapon :
				SwitchWeapon( gs, userCmd, inventory );

				if (!isAlive)
				{
					inventory.SwitchWeapon(null);
					inventory.FinalizeWeaponSwitch();
				}
					
				var weaponEntity	=	inventory.ActiveWeapon;

				//	is active item weapon?
				if (weaponAspect.Accept(weaponEntity) && isAlive)
				{
					var weapon	= weaponEntity.GetComponent<WeaponComponent>();
					var attack	= userCmd.Action.HasFlag( UserAction.Attack );

					var ammo	= GetAmmo( gs, inventory, weapon );

					weapon.HudAmmo		=	ammo==null ? 0 : ammo.Count;
					weapon.HudAmmoMax	=	200;

					FadeSpread( gameTime, weapon );
					AdvanceWeaponTimer( gameTime, weaponEntity );
					UpdateWeaponFSM( gameTime, attack, povTransform, entity, inventory, weaponEntity );
				}
			}
		}


		bool SwitchWeapon( GameState gs, UserCommandComponent userCmd, InventoryComponent inventory )
		{
			if (userCmd.Weapon!=null)
			{
				foreach ( var e in inventory )
				{
					var n = e?.GetComponent<NameComponent>()?.Name;

					if (n==userCmd.Weapon)
					{
						inventory.SwitchWeapon(e);
						return true;
					}
				}
			}

			return false;
		}


		/*-----------------------------------------------------------------------------------------------
		 * Weapon state
		-----------------------------------------------------------------------------------------------*/
		
		void AdvanceWeaponTimer( GameTime gameTime, Entity weaponEntity )
		{
			var weapon	=	weaponEntity.GetComponent<WeaponComponent>();

			if ( weapon.Timer > TimeSpan.Zero ) 
			{
				weapon.Timer = weapon.Timer - gameTime.Elapsed;
			}
		}


		void FadeSpread( GameTime gameTime, WeaponComponent weapon )
		{
			if (weapon.SpreadMode==SpreadMode.Variable)
			{
				weapon.Spread *= (float)Math.Pow( SPREAD_FADEOUT, Math.Min(1, gameTime.ElapsedSec) );
			}
		}


		void UpdateWeaponFSM (GameTime gameTime, bool attack, Matrix povTransform, Entity attacker, InventoryComponent inventory, Entity weaponEntity )
		{
			var weapon	=	weaponEntity.GetComponent<WeaponComponent>();
			var timeout	=	weapon.Timer <= TimeSpan.Zero;
			var gs		=	weaponEntity.gs;

			switch (weapon.State) 
			{
				case WeaponState.Idle:	
					if (attack) 
					{
						if (TryConsumeAmmo(gs, inventory, weapon)) 
						{
							weapon.State =  WeaponState.Warmup;	
							weapon.Timer += weapon.TimeWarmup;
						} 
						else 
						{
							weapon.State =  WeaponState.NoAmmo;	
							weapon.Timer += weapon.TimeNoAmmo;
						}
					}
					if (inventory.HasPendingWeapon) 
					{
						weapon.State =  WeaponState.Drop;	
						weapon.Timer =  weapon.TimeDrop;
					}
					break;

				case WeaponState.Warmup:	
					if (timeout) 
					{
						Fire(actualGameTime, weapon, povTransform, attacker);

						weapon.Counter++;
						
						if ((weapon.Counter&1)==0) 
						{
							weapon.State = WeaponState.Cooldown;	
						} 
						else 
						{
							weapon.State = WeaponState.Cooldown2;	
						}

						weapon.Timer += weapon.TimeCooldown;
					}
					break;


				case WeaponState.Cooldown:	
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Cooldown2:	
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;	
					}
					break;

				case WeaponState.Reload:		
					break;

				case WeaponState.Overheat:		
					break;

				case WeaponState.Drop:	
					if (timeout) 
					{
						inventory.FinalizeWeaponSwitch();
						weapon.State = WeaponState.Inactive;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Raise:		
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.NoAmmo:		
					if (timeout) 
					{
						weapon.State = WeaponState.Idle;
						weapon.Timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Inactive:	
					if (inventory.ActiveWeapon == weaponEntity) 
					{
						weapon.State = WeaponState.Raise;
						weapon.Timer = weapon.TimeRaise;
					}	
					break;
			}
		}


		AmmoComponent GetAmmo( GameState gs, InventoryComponent inventory, WeaponComponent weapon )
		{
			AmmoComponent ammo;
			NameComponent name;
			
			inventory.FindItem<AmmoComponent,NameComponent>( gs, (a,n) => n.Name == weapon.AmmoClass, out ammo, out name );

			return ammo;
		}


		bool TryConsumeAmmo( GameState gs, InventoryComponent inventory, WeaponComponent weapon )
		{
			if (inventory.Flags.HasFlag(InventoryFlags.InfiniteAmmo))
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
			}
		}


		/// <summary>
		/// 
		/// </summary>
		bool Fire ( GameTime gameTime, WeaponComponent weapon, Matrix povTransform, Entity attacker )
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

		void FireBeam ( GameState gs, WeaponComponent weapon, Matrix povTransform, Entity attacker )
		{
			var p	=	povTransform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( povTransform );
			var d	=	-GetFireDirection( q, weapon.Spread );
			var ray	=	new Ray(p,d);

			physics.Raycast( ray, BEAM_RANGE, new BeamRaycastCallback( gs, ray, attacker, weapon ), RaycastOptions.SortResults );
		}


		class BeamRaycastCallback : IRaycastCallback
		{
			readonly Ray ray;
			readonly Entity attacker;
			readonly WeaponComponent weapon;
			readonly GameState gs;
			bool hitSomething = false;
			Vector3 hitLocation;

			public BeamRaycastCallback( GameState gs, Ray ray, Entity attacker, WeaponComponent weapon )
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

			public void End() 
			{
				//	run trail FX:
				var beamOrigin	 =	ray.Position;
				var beamVelocity =	hitLocation - ray.Position;
				var basis		=	MathUtil.ComputeAimedBasis( ray.Direction );

				SFX.FXPlayback.SpawnFX(	gs, weapon.BeamTrailFX, 0, beamOrigin, beamVelocity, Quaternion.RotationMatrix(basis) );
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
		[MethodImpl(MethodImplOptions.NoOptimization)]
		void FireProjectile ( GameState gs, GameTime gameTime, WeaponComponent weapon, Matrix povTransform, Entity attacker )
		{
			var dt	=	gameTime.ElapsedSec;
			var t	=	attacker.GetComponent<KinematicState>();
			var p	=	povTransform.TranslationVector;
			var q	=	Quaternion.RotationMatrix( povTransform );
			var d	=	-GetFireDirection( q, weapon.MaxSpread );

			//	create projectile without transform :
			var projectileEntity	=	gs.Spawn( weapon.ProjectileClass );
			var projectileComponent	=	projectileEntity.GetComponent<ProjectileComponent>();
				projectileComponent.Sender	=	attacker;
				projectileComponent.Impulse	=	weapon.Impulse;
				projectileComponent.Damage	=	weapon.Damage;

			//	estimate projectile position :
			var projectileVelocity	=	projectileComponent.Velocity;
			var projectileDistance	=	projectileVelocity * gameTime.ElapsedSec * 2; // magic number??

			var projectileRay		=	new Ray( p, d );

			//	create estimated projectile transform to add it if no collision is found :
			var kinematicState		=	new KinematicState( p + d * projectileDistance, q, 1 );

			//	run raycast query to find instant porjectile position OR run projectile simulation :
			var raycastCallback		=	new ProjectileRaycastCallback( gs, projectileRay, attacker, projectileEntity, kinematicState );

			physics.Raycast( projectileRay, projectileDistance, raycastCallback, RaycastOptions.SortResults );
		}


		class ProjectileRaycastCallback : IRaycastCallback
		{
			readonly Ray ray;
			readonly Entity attacker;
			readonly Entity projectile;
			readonly ProjectileComponent projectileComponent;
			readonly KinematicState kinematicState;
			readonly GameState gs;
			readonly WeaponSystem ws;
			bool hitSomething = false;

			[MethodImpl(MethodImplOptions.NoOptimization)]
			public ProjectileRaycastCallback( GameState gs, Ray ray, Entity attacker, Entity projectile, KinematicState ks )
			{
				this.ray		=	ray;
				this.ws			=	gs.GetService<WeaponSystem>();
				this.attacker	=	attacker;
				this.projectile	=	projectile;
				this.gs			=	gs;

				this.kinematicState	=	ks;
				projectileComponent	=	projectile.GetComponent<ProjectileComponent>();
			}

			public void Begin( int count ) {}

			[MethodImpl(MethodImplOptions.NoOptimization)]
			public bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic )
			{
				if (entity==attacker) return false;

				//	inflict damage to instantly hit by projectile entity :
				ws.InflictDamage( attacker, entity, projectileComponent.Damage, projectileComponent.Impulse, location, ray.Direction, normal, projectileComponent.ExplosionFX );
				ws.Explode( attacker, entity, projectileComponent.Damage, projectileComponent.Impulse, projectileComponent.Radius, location, normal, null );

				hitSomething = true;

				//	kill projectile, 
				//	we dont need it any more :
				projectile.Kill();

				return true;
			}

			[MethodImpl(MethodImplOptions.NoOptimization)]
			public void End() 
			{
				if (!hitSomething)
				{
					//	nothing hit, add transform and continue projectile simulation :
					projectile.AddComponent( kinematicState );
				}
			}
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
					SFX.FXPlayback.AttachFX( gs, target, hitFx, 0, location, normal );
				}
			}
			else
			{
				if (fx!=null)
				{
					FXPlayback.SpawnFX( gs, fx, 0, location, normal );
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
				FXPlayback.SpawnFX( gs, fx, 0, location, normal );
			}
		}


		class ExplodeOverlapCallback : IRaycastCallback
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

			public void End() {}
			
			public bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic )
			{
				if (entity!=null && entity!=target)
				{
					//	overlap test always gives result on the surface of the sphere...
					var transform	=	entity.GetComponent<KinematicState>();

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
