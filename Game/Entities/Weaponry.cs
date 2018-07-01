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
	public static class Weaponry {

		static Random rand = new Random();


		static Vector3 AttackPos( Entity attacker, Vector3 origin, Vector3 rotation )
		{
			return Vector3.TransformCoordinate( offset, attacker.GetWorldMatrix(0) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		static void FirePlasma( GameWorld world, Entity attacker, Vector3 origin, Quaternion rotation )
		{
			var e = world.Spawn( "plasma" );
			e.ParentID	=	attacker.ID;
			e.Teleport( origin, rotation );
			(e as Projectile).FixServerLag();

			world.SpawnFX( "MZBlaster",	attacker.ID, origin );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="attacker"></param>
		/// <param name="cooldown"></param>
		static void FireRocket( GameWorld world, Entity attacker )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var origin = AttackPos(attacker);

			var e = world.Spawn( "rocket", attacker.ID, origin, attacker.Rotation );

			world.SpawnFX( "MZRocketLauncher",	attacker.ID, origin );

			attacker.WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		static void FireBullet ( GameWorld world, Entity attacker, int damage, float impulse, float spread )
		{
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

			attacker.WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		static void FireShot ( GameWorld world, Entity attacker, int damage, int count, float impulse, float spread )
		{
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

			attacker.WeaponCooldown += cooldown;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="damage"></param>
		static void FireRail ( GameWorld world, Entity attacker, int damage, float impulse )
		{
			if (!TryConsumeAmmo( attacker, 1 )) {
				return;
			}

			var view	=	Matrix.RotationQuaternion( attacker.Rotation );
			Vector3 n,p;
			Entity e;

			var direction	=	view.Forward;
			var origin		=	AttackPos( attacker );

			if (world.RayCastAgainstAll( origin, origin + direction * 200, out n, out p, out e, attacker )) {

				//world.SpawnFX( "PlayerDeathMeat", attacker.ID, p, n );
				world.SpawnFX( "RailHit",		0,					p, n );
				world.SpawnFX( "RailMuzzle",	attacker.ID, origin, n );
				world.SpawnFX( "RailTrail",		attacker.ID, origin, p - origin, attacker.Rotation );

				world.InflictDamage( e, attacker.ID, (short)damage, view.Forward * impulse, p, DamageType.RailHit );

			} else {
				world.SpawnFX( "RailMuzzle",	attacker.ID, origin, n );
				world.SpawnFX( "RailTrail",		attacker.ID, origin, direction * 200, attacker.Rotation );
			}

			attacker.WeaponCooldown += cooldown;
		}


	}
}