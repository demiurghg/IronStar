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

		public short WeaponCooldown	=	0;



		Vector3 AttackPos ( Entity e )
		{
			var c = e.Controller as Character;
			var m = Matrix.RotationQuaternion(e.Rotation);
			return e.PointOfView + m.Right * 0.1f + m.Down * 0.1f + m.Forward * 0.3f;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		void FirePlasma( GameWorld world, Entity attacker, short cooldown )
		{
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

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
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

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
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

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
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

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
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

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
			//if (!TryConsumeAmmo( attacker, 1 )) {
			//	return;
			//}

			var view	=	Matrix.RotationQuaternion( attacker.Rotation );
			Vector3 n,p;
			Entity e;

			var direction	=	view.Forward;
			var origin		=	AttackPos( attacker );

			world.InflictDamage( attacker, 0, 0, view.Backward * impulse, origin, DamageType.RailHit );

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
