using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using Fusion;

namespace IronStar.Gameplay.Systems
{
	class WeaponSystem : ISystem
	{
		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }


		Aspect weaponAspect			=	new Aspect().Include<WeaponComponent>();
		Aspect armedPlayerAspect	=	new Aspect().Include<InventoryComponent,PlayerComponent,UserCommandComponent>();

		
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
				var inventory	=	player.GetComponent<InventoryComponent>();
				var userCmd		=	player.GetComponent<UserCommandComponent>();

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
					UpdateWeaponFSM( gameTime, attack, inventory, activeItem );
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


		void UpdateWeaponFSM (GameTime gameTime, bool attack, InventoryComponent inventory, Entity weaponEntity )
		{
			var weapon	=	weaponEntity.GetComponent<WeaponComponent>();
			var timeout	=	weapon.Timer <= TimeSpan.Zero;

			/*if (weapon.State!=WeaponState.Idle)*/ Log.Message("...{0}", weapon.State.ToString());

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
						Fire(weapon);

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
		bool Fire ( WeaponComponent weapon )
		{
			if (weapon.IsBeamWeapon) 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireBeam( weapon );
				}
				return true;
			} 
			else 
			{
				for (int i=0; i<weapon.ProjectileCount; i++) 
				{
					FireProjectile( weapon );
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
		void FireBeam ( WeaponComponent weapon )
		{
			Log.Message("** FIRE BEAM **");
			/*var p = attacker.GetActualPOV();
			var q = attacker.Rotation;
			var d = -GetFireDirection(q);

			Vector3 hitNormal;
			Vector3 hitPoint;
			Entity  hitEntity;

			var r = world.RayCastAgainstAll( p, p + d * beamLength, out hitNormal, out hitPoint, out hitEntity, attacker );

			if (r) {

				world.SpawnFX( hitFX, 0, hitPoint, hitNormal );
				world.InflictDamage( hitEntity, attacker.ID, damage, DamageType.BulletHit, d * impulse, hitPoint );
			} else {
				hitPoint = p + d * beamLength;
			}

			//	run trail FX:
			var beamOrigin	 =	p;
			var beamVelocity =	hitPoint - p;
			world.SpawnFX( beamFX, 0, beamOrigin, beamVelocity, q );   */
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( WeaponComponent weapon )
		{
			Log.Message("** FIRE PROJECTILE **");

			/*var e = world.Spawn( projectile ) as Projectile;

			if (e==null) {
				Log.Warning("Unknown projectile class: {0}", projectile);
				return;
			}

			var p = attacker.GetActualPOV();
			var q = attacker.Rotation;
			var d = GetFireDirection(q);

			e.ParentID	=	attacker.ID;
			e.Teleport( p, q );

			e.HitDamage		=	damage;
			e.HitImpulse	=	impulse;

			e.FixServerLag(2/60.0f); */
		}



		/// <summary>
		/// Gets firing direction
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		/*
		Vector3 GetFireDirection ( Quaternion rotation )
		{ 
			var spreadVector	= GetSpreadVector( angularSpread );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix ).Normalized();
		}
		*/



		/// <summary>
		/// Gets radial spread vector
		/// </summary>
		/// <param name="spreadAngle"></param>
		/// <returns></returns>
		/*
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
		*/
	}
}
