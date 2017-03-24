using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;


namespace IronStar.Entities {
	public partial class Character : EntityController {

		Random rand = new Random();

		//	Inventory
		public WeaponType Weapon1	=	WeaponType.None;
		public WeaponType Weapon2	=	WeaponType.None;
		public short Health			=	100;
		public short Armor			=	0;
		public short WeaponCooldown	=	0;
		public short WeaponAmmo1	=	0;
		public short WeaponAmmo2	=	0;
		public short Grenades		=	0;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		bool TryConsume ( ref short source, short count )
		{
			if (source - count < 0) {
				return false;
			} else {
				source -= count;
				return true;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		bool TryConsumeAmmo ( Entity entity, short count )
		{
			if (entity.State.HasFlag( EntityState.PrimaryWeapon )) {
				return TryConsume( ref WeaponAmmo1, count );
			}
			if (entity.State.HasFlag( EntityState.SecondaryWeapon )) {
				return TryConsume( ref WeaponAmmo2, count );
			}
			return false;
		}



		/// <summary>
		/// 
		/// </summary>
		public void SwitchWeapon ()
		{
			if (Entity.State.HasFlag( EntityState.PrimaryWeapon )) {

				Entity.State &= ~EntityState.PrimaryWeapon;
				Entity.State |= EntityState.SecondaryWeapon;
				Log.Verbose("...switched to secondary weapon");

			} else if (Entity.State.HasFlag( EntityState.SecondaryWeapon )) {

				Entity.State &= ~EntityState.SecondaryWeapon;
				Entity.State |= EntityState.PrimaryWeapon;
				Log.Verbose("...switched to primary weapon");

			} else {
				Log.Verbose("...no weapon");
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="deltaTime"></param>
		void UpdateWeaponState ( Entity entity, short deltaTime )
		{
			var attack		=	entity.UserCtrlFlags.HasFlag( UserCtrlFlags.Attack );
			var grenade		=	entity.UserCtrlFlags.HasFlag( UserCtrlFlags.ThrowGrenade );

			//	weapon is too hot :
			if (WeaponCooldown>0) {
				WeaponCooldown -= deltaTime;
				return;
			}

			var world = World;


			var weapon	= WeaponType.None;

			if ( entity.State.HasFlag( EntityState.PrimaryWeapon ) ) {
				weapon = Weapon1; 
			} else if ( entity.State.HasFlag( EntityState.SecondaryWeapon ) ) {
				weapon = Weapon2;
			}


			if (grenade) {
				FireGrenade( world, entity, 500 );
				return;
			}


			if (attack) {
				switch (weapon) {
					case WeaponType.Machinegun		:	FireBullet( world, entity, 5, 5, 100, 0.03f ); break;
					case WeaponType.Shotgun			:	FireShot( world, entity, 10,10, 5.0f, 1000, 0.12f); break;
					case WeaponType.Plasmagun		:	FirePlasma(world, entity, 100); break;
					case WeaponType.RocketLauncher	:	FireRocket(world, entity, 800); break;
					case WeaponType.GaussRifle		:	FireRail(world, entity, 100, 100, 1500); break;
					default: 
						break;
				}
				return;
			}
		}



		Vector3 AttackPos ( Entity e )
		{
			var c = e.Controller as Character;
			var m = Matrix.RotationQuaternion(e.Rotation);
			return c.GetPOV() + m.Right * 0.1f + m.Down * 0.1f + m.Forward * 0.3f;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		void FirePlasma( GameWorld world, Entity attacker, short cooldown )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var origin = AttackPos(attacker);

			var e = world.Spawn( "plasma", attacker.ID, origin, attacker.Rotation );

			world.SpawnFX( "plasma_muzzle",	attacker.ID, origin );

			WeaponCooldown += cooldown;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		void FireRocket( GameWorld world, Entity attacker, short cooldown )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var origin = AttackPos(attacker);

			var e = world.Spawn( "rocket", attacker.ID, origin, attacker.Rotation );

			world.SpawnFX( "rocket_muzzle",	attacker.ID, origin );

			WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		void FireGrenade( GameWorld world, Entity attacker, short cooldown )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var origin = AttackPos(attacker);

			var e = world.Spawn( "grenade", attacker.ID, origin, attacker.Rotation );

			world.SpawnFX( "grenade_muzzle",	attacker.ID, origin );

			WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		void FireBullet ( GameWorld world, Entity attacker, int damage, float impulse, short cooldown, float spread )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var view	=	Matrix.RotationQuaternion( attacker.Rotation );
			Vector3 n,p;
			Entity e;

			var direction	=	view.Forward + rand.UniformRadialDistribution(0, spread);
			var origin		=	AttackPos( attacker );

			if (world.RayCastAgainstAll( origin, origin + direction * 400, out n, out p, out e, attacker )) {

				world.SpawnFX( "bullet_hit",	0, p, n );
				//world.SpawnFX( "MZMachinegun",	attacker.ID, origin, n );

				world.InflictDamage( e, attacker.ID, (short)damage, view.Forward * impulse, p, DamageType.BulletHit );

			} else {
				world.SpawnFX( "MZMachinegun",	0, origin, n );
			}

			WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		void FireShot ( GameWorld world, Entity attacker, int damage, int count, float impulse, short cooldown, float spread )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var view	=	Matrix.RotationQuaternion( attacker.Rotation );
			Vector3 n,p;
			Entity e;

			var origin		=	AttackPos( attacker );

			world.SpawnFX( "MZShotgun",	attacker.ID, origin );

			for (int i=0; i<count; i++) {
				
				var direction	=	view.Forward + rand.UniformRadialDistribution(0, spread);

				if (world.RayCastAgainstAll( origin, origin + direction * 400, out n, out p, out e, attacker )) {

					world.SpawnFX( "bullet_hit_shot",	0, p, n );

					world.InflictDamage( e, attacker.ID, (short)damage, view.Forward * impulse, p, DamageType.BulletHit );

				} 
			}

			WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		void FireRail ( GameWorld world, Entity attacker, int damage, float impulse, short cooldown )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var view	=	Matrix.RotationQuaternion( attacker.Rotation );
			Vector3 n,p;
			Entity e;

			var direction	=	view.Forward;
			var origin		=	AttackPos( attacker );

			this.controller.Body.ApplyImpulse( controller.Body.Position, MathConverter.Convert( view.Backward * impulse / 2 ) );

			if (world.RayCastAgainstAll( origin, origin + direction * 200, out n, out p, out e, attacker )) {

				//world.SpawnFX( "PlayerDeathMeat", attacker.ID, p, n );
				world.SpawnFX( "rail_hit",		0,					p, n );
				world.SpawnFX( "rail_muzzle",	attacker.ID, origin, n );
				world.SpawnFX( "rail_trail",	attacker.ID, origin, p - origin, attacker.Rotation );

				world.InflictDamage( e, attacker.ID, (short)damage, view.Forward * impulse, p, DamageType.RailHit );

			} else {
				world.SpawnFX( "rail_muzzle",	attacker.ID, origin, n );
				world.SpawnFX( "rail_trail",	attacker.ID, origin, direction * 200, attacker.Rotation );
			}

			WeaponCooldown += cooldown;
		}

	}
}
