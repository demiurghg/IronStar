using System;
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
using Fusion.Core;
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

		readonly Random rand = new Random();
		readonly GameWorld world;

		readonly bool	beamWeapon;
		readonly float	beamLength;
		readonly string projectile;
		readonly int	damage;
		readonly float	impulse;
		readonly int	projectileCount;
		readonly float	angularSpread;

		readonly int	cooldown;
		readonly string hitFX;
		readonly string ammoItem;

		int timer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="factory"></param>
		public Weapon( GameWorld world, WeaponFactory factory )
		{
			this.world		=	world;
				
			beamWeapon		=	factory.BeamWeapon		;
			beamLength		=	factory.BeamLength		;
			projectile		=	factory.Projectile		;
			damage			=	factory.Damage			;
			impulse			=	factory.Impulse			;
			projectileCount	=	factory.ProjectileCount	;
			angularSpread	=	factory.AngularSpread	;

			cooldown		=	factory.Cooldown		;
			hitFX			=	factory.HitFX			;
			ammoItem		=	factory.AmmoItem		;

		}


		public override bool Attack(IShooter shooter, Entity attacker)
		{
			if (timer<=0) {
				if (Fire(shooter, attacker)) {
					timer = cooldown;
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}


		public override void Update(GameTime gameTime, Entity entity)
		{
			if (timer>0) {
				timer -= gameTime.Milliseconds;
			} else {
				timer = 0;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		protected virtual bool Fire ( IShooter shooter, Entity attacker )
		{
			if (beamWeapon) {

				for (int i=0; i<projectileCount; i++) {
					FireBeam( attacker, shooter, world );
				}

				return true;

			} else {

				for (int i=0; i<projectileCount; i++) {
					FireProjectile( attacker, shooter, world );
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
		void FireBeam ( Entity attacker, IShooter shooter, GameWorld world )
		{
			var p = shooter.GetActualPOV();
			var q = attacker.Rotation;
			var d = -GetFireDirection(q);

			Vector3 hitNormal;
			Vector3 hitPoint;
			Entity  hitEntity;

			var r = world.RayCastAgainstAll( p, p + d * beamLength, out hitNormal, out hitPoint, out hitEntity, attacker );

			if (r) {
				world.SpawnFX( hitFX, 0, hitPoint, hitNormal );
				world.InflictDamage( hitEntity, attacker.ID, damage, DamageType.BulletHit, d * impulse, hitPoint );
			}
		}



		/// <summary>
		/// Fires projectile
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="world"></param>
		/// <param name="origin"></param>
		void FireProjectile ( Entity attacker, IShooter shooter, GameWorld world )
		{
			var e = world.Spawn( projectile ) as Projectile;

			if (e==null) {
				Log.Warning("Unknown projectile class: {0}", projectile);
				return;
			}

			var p = shooter.GetActualPOV();
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

