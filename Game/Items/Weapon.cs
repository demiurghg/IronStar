﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using IronStar.Entities;
using Fusion.Core.Shell;

namespace IronStar.Items {

	public partial class Weapon : Item {

		const int TIME_NO_AMMO = 250;
		
		readonly Random rand = new Random();
		readonly GameWorld world;

		readonly bool	beamWeapon;
		readonly float	beamLength;
		readonly string projectile;
		readonly int	damage;
		readonly float	impulse;
		readonly int	projectileCount;
		readonly float	angularSpread;

		readonly TimeSpan	timeWarmup	;
		readonly TimeSpan	timeCooldown;
		readonly TimeSpan	timeOverheat;
		readonly TimeSpan	timeReload	;
		readonly TimeSpan	timeDrop	;
		readonly TimeSpan	timeRaise	;
		readonly TimeSpan	timeNoAmmo	;
		readonly string hitFX;
		readonly string ammoName;
		readonly short  ammoClsId;
		readonly int    ammoConsume;
		readonly string viewModel;
		readonly string beamFX;

		public short AmmoClassID { get { return ammoClsId; } }

		TimeSpan timer;
		WeaponState state;
		bool rqAttack;
		uint rqNextWeapon;
		int counter;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="factory"></param>
		public Weapon( uint id, short clsid, GameWorld world, WeaponFactory factory ) : base(id, clsid, factory)
		{
			this.world		=	world;
				
			beamWeapon		=	factory.BeamWeapon		;
			beamLength		=	factory.BeamLength		;
			projectile		=	factory.Projectile		;
			damage			=	factory.Damage			;
			impulse			=	factory.Impulse			;
			projectileCount	=	factory.ProjectileCount	;
			angularSpread	=	factory.AngularSpread	;

			timeWarmup		=	TimeSpan.FromMilliseconds( factory.WarmupTime	);
			timeCooldown	=	TimeSpan.FromMilliseconds( factory.CooldownTime	);
			timeOverheat	=	TimeSpan.FromMilliseconds( factory.OverheatTime	);
			timeReload		=	TimeSpan.FromMilliseconds( factory.ReloadTime	);
			timeDrop		=	TimeSpan.FromMilliseconds( factory.DropTime		);
			timeRaise		=	TimeSpan.FromMilliseconds( factory.RaiseTime	);
			timeNoAmmo		=	TimeSpan.FromMilliseconds( TIME_NO_AMMO );
			
			hitFX			=	factory.HitFX			;
			beamFX			=	factory.BeamFX			;
			ammoName		=	factory.AmmoItem		;
			ammoClsId		=	world.Atoms[ ammoName ]	;
			ammoConsume		=	factory.AmmoConsumption ;

			viewModel		=	factory.ViewModel		;

		}


		public override bool Pickup( Entity player )
		{
			var existingWeapon = world.Items.GetOwnedItemByClass( player.ID, ClassID );

			if (existingWeapon==null) {

				Owner = player.ID;
				return true;
			
			} else {

				return false;

			}
		}


		public override bool Switch(Entity target, uint nextItem)
		{
			rqNextWeapon = nextItem;
			return true;
		}


		public override bool Attack(Entity attacker)
		{
			rqAttack = true;
			return true;
		}


		public override void Update(GameTime gameTime)
		{
			if ( timer > TimeSpan.Zero ) {
				timer = timer - gameTime.Elapsed;
			} else {
				//timer = TimeSpan.Zero;
			}

			var entity = world.Entities.GetEntity(Owner);
			if (entity==null) {
				return;
			}

			//	update FSM twice to 
			//	bypass zero time states:
			UpdateFSM( gameTime, entity );
			UpdateFSM( gameTime, entity );
			UpdateFSM( gameTime, entity );

			rqAttack = false;

			//	update animation state :
			//	actually, even dropped weapon could perform attack!!! :)
			//entity.SetState( EntityState.Weapon_States, false );
			bool visible = !(state==WeaponState.Inactive);

			if (entity.ItemID==ID) {
				entity.WeaponState = state;
				entity.ModelFpv	=	world.Atoms[viewModel];
			}
		}



		/// <summary>
		/// Gets player owned ammo or null.
		/// </summary>
		/// <returns></returns>
		public Ammo GetPlayerAmmo ()
		{
			return world.Items.GetOwnedItemByClass( Owner, ammoName ) as Ammo;
		}


		bool ConsumeAmmo ()
		{
			var ammoItem = world.Items.GetOwnedItemByClass( Owner, ammoName ) as Ammo;

			if (ammoItem==null) {
				return false;
			}

			return ammoItem.ConsumeAmmo( ammoConsume );
		}



		void UpdateFSM (GameTime gameTime, Entity entity)
		{
			bool timeout = timer <= TimeSpan.Zero;

			switch (state) {
				case WeaponState.Idle:	
					if (rqAttack) {
						if (ConsumeAmmo()) {
							state =  WeaponState.Warmup;	
							timer += timeWarmup;
						} else {
							state =  WeaponState.NoAmmo;	
							timer += timeNoAmmo;
						}
					}
					if (rqNextWeapon!=0) {
						state =  WeaponState.Drop;	
						timer =  timeDrop;
					}
					break;

				case WeaponState.Warmup:	
					if (timeout) {
						Fire(entity);

						counter++;
						Log.Message("{0}", counter);
						if ((counter&1)==0) {
							state = WeaponState.Cooldown;	
						} else {
							state = WeaponState.Cooldown2;	
						}

						timer += timeCooldown;
					}
					break;


				case WeaponState.Cooldown:	
					if (timeout) {
						state = WeaponState.Idle;	
					}
					break;

				case WeaponState.Cooldown2:	
					if (timeout) {
						state = WeaponState.Idle;	
					}
					break;

				case WeaponState.Reload:		
					break;

				case WeaponState.Overheat:		
					break;

				case WeaponState.Drop:	
					if (timeout) {
						entity.ItemID = rqNextWeapon;
						rqNextWeapon  = 0;
						state = WeaponState.Inactive;
						timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Raise:		
					if (timeout) {
						state = WeaponState.Idle;
						timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.NoAmmo:		
					if (timeout) {
						state = WeaponState.Idle;
						timer = TimeSpan.Zero;
					}
					break;

				case WeaponState.Inactive:	
					if (entity.ItemID == ID) {
						state = WeaponState.Raise;
						timer = timeRaise;
					}	
					break;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual bool Fire ( Entity attacker )
		{
			if (beamWeapon) {

				for (int i=0; i<projectileCount; i++) {
					FireBeam( attacker, world );
				}

				return true;

			} else {

				for (int i=0; i<projectileCount; i++) {
					FireProjectile( attacker, world );
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
		void FireBeam ( Entity attacker, GameWorld world )
		{
			var p = attacker.GetActualPOV();
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
			world.SpawnFX( beamFX, 0, beamOrigin, beamVelocity, q );
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( Entity attacker, GameWorld world )
		{
			var e = world.Spawn( projectile ) as Projectile;

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

			e.FixServerLag(2/60.0f);

			//world.SpawnFX( "MZBlaster",	attacker.ID, origin );
		}



		/// <summary>
		/// Gets firing direction
		/// </summary>
		/// <param name="rotation"></param>
		/// <returns></returns>
		Vector3 GetFireDirection ( Quaternion rotation )
		{ 
			var spreadVector	= GetSpreadVector( angularSpread );
			var rotationMatrix	= Matrix.RotationQuaternion( rotation );
			return Vector3.TransformNormal( spreadVector, rotationMatrix ).Normalized();
		}



		/// <summary>
		/// Gets radial spread vector
		/// </summary>
		/// <param name="spreadAngle"></param>
		/// <returns></returns>
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

