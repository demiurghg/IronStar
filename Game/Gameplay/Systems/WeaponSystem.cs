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

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		const float BEAM_RANGE	=	8192;

		Random rand = new Random();

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }
		public readonly PhysicsCore physics;


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedPlayerAspect	=	new Aspect().Include<InventoryComponent,PlayerComponent,UserCommandComponent,CharacterController>()
													.Include<Transform>();


		public WeaponSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}

		
		public void Update( GameState gs, GameTime gameTime )
		{
			//	update player's weapon :
			UpdatePlayerWeapon( gs, gameTime );
		}


		void UpdatePlayerWeapon( GameState gs, GameTime gameTime )
		{
			var players = gs.QueryEntities( armedPlayerAspect );

			foreach ( var player in players )
			{
				var transform	=	player.GetComponent<Transform>();
				var inventory	=	player.GetComponent<InventoryComponent>();
				var userCmd		=	player.GetComponent<UserCommandComponent>();
				var chctrl		=	player.GetComponent<CharacterController>();

				var povTransform	=	userCmd.RotationMatrix * Matrix.Translation(transform.Position + chctrl.PovOffset);

				if (inventory.HasPendingWeapon && inventory.ActiveWeaponID==0)
				{
					inventory.FinalizeWeaponSwitch();
				}
					
				var activeItem	=	gs.GetEntity( inventory.ActiveWeaponID );

				//	is active item weapon?
				if (weaponAspect.Accept(activeItem))
				{
					var weapon = activeItem.GetComponent<WeaponComponent>();
					var attack = userCmd.Action.HasFlag( UserAction.Attack );

					AdvanceWeaponTimer( gameTime, activeItem );
					UpdateWeaponFSM( gameTime, attack, povTransform, player, inventory, activeItem );
				}
			}
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


		void UpdateWeaponFSM (GameTime gameTime, bool attack, Matrix povTransform, Entity attacker, InventoryComponent inventory, Entity weaponEntity )
		{
			var weapon	=	weaponEntity.GetComponent<WeaponComponent>();
			var timeout	=	weapon.Timer <= TimeSpan.Zero;

			switch (weapon.State) 
			{
				case WeaponState.Idle:	
					if (attack) 
					{
						if (TryConsumeAmmo(inventory, weapon)) 
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
						Fire(gameTime, weapon, povTransform, attacker);

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
					if (inventory.ActiveWeaponID == weaponEntity.ID) 
					{
						weapon.State = WeaponState.Raise;
						weapon.Timer = weapon.TimeRaise;
					}	
					break;
			}
		}


		bool TryConsumeAmmo( InventoryComponent inventory, WeaponComponent weapon )
		{
			return true;
		}


		/// <summary>
		/// 
		/// </summary>
		bool Fire ( GameTime gameTime, WeaponComponent weapon, Matrix povTransform, Entity attacker )
		{
			var gs = attacker.gs;

			if (weapon.IsBeamWeapon) 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireBeam( gs, weapon, povTransform, attacker );
				}
				return true;
			} 
			else 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireProjectile( gs, gameTime, weapon, povTransform, attacker );
				}
				return true;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="shooter"></param>
		/// <param name="world"></param>
		void FireBeam ( GameState gs, WeaponComponent weapon, Matrix povTransform, Entity attacker )
		{
			var p = povTransform.TranslationVector;
			var q = Quaternion.RotationMatrix( povTransform );
			var d = -GetFireDirection( q, weapon.Spread );

			Vector3 hitNormal;
			Vector3 hitPoint;
			Entity  hitEntity;

			var r = physics.RayCastAgainstAll( p, p + d * BEAM_RANGE, out hitNormal, out hitPoint, out hitEntity, attacker );

			if (r) 
			{
				SFX.FXPlayback.SpawnFX(	gs, weapon.BeamHitFX, 0, hitPoint, hitNormal );
				PhysicsCore.ApplyImpulse( hitEntity, hitPoint, d * weapon.Impulse );
				HealthSystem.ApplyDamage( hitEntity, weapon.Damage );
			} 
			else 
			{
				hitPoint = p + d * BEAM_RANGE;
			}

			//	run trail FX:
			var beamOrigin	 =	p;
			var beamVelocity =	hitPoint - p;
			SFX.FXPlayback.SpawnFX(	gs, weapon.BeamTrailFX, 0, beamOrigin, beamVelocity, d );
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( GameState gs, GameTime gameTime, WeaponComponent weapon, Matrix povTransform, Entity attacker )
		{
			var v = attacker.GetComponent<Velocity>();
			var p = povTransform.TranslationVector + v.Linear * gameTime.ElapsedSec;
			var q = Quaternion.RotationMatrix( povTransform );
			var d = -GetFireDirection( q, weapon.Spread );

			var e = gs.Spawn( weapon.ProjectileClass );

			var projectile	=	e.GetComponent<ProjectileComponent>();
			var transform	=	e.GetComponent<Transform>();

			projectile.Damage		=	weapon.Damage;
			projectile.Impulse		=	weapon.Impulse;
			projectile.SenderID		=	attacker.ID;
			projectile.Direction	=	d;

			transform.Position	=	povTransform.TranslationVector;
			transform.Scaling	=	Vector3.One;
			transform.Rotation	=	q;
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
