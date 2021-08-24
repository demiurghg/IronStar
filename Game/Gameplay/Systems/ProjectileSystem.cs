using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.ECSPhysics;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Core.Extensions;
using IronStar.SFX;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;

namespace IronStar.Gameplay.Systems
{
	class ProjectileSystem : ProcessingSystem<ProjectileController,KinematicState,ProjectileComponent>
	{
		readonly PhysicsCore	physics;
		readonly Random			rand;
		float lastDeltaTime = 0;

		public ProjectileSystem( PhysicsCore physics )
		{
			this.rand		=	new Random();
			this.physics	=	physics;
		}



		protected override ProjectileController Create( Entity entity, KinematicState transform, ProjectileComponent projectile )
		{
			var position	=	MathConverter.Convert( transform.Position );
			var direction	=	MathConverter.Convert( transform.TransformMatrix.Forward );
			var velocity	=	projectile.Velocity;

			var projectileController = new ProjectileController( position, direction, velocity, lastDeltaTime, (bpe) => PhysicsCore.SkipEntityFilter( bpe, projectile.Sender ) );
			projectileController.CollisionDetected+=ProjectileController_CollisionDetected;
			projectileController.Tag = entity;

			physics.Add( projectileController );

			return projectileController;
		}

		private void ProjectileController_CollisionDetected( object controller, ProjectileController.CollisionDetectedEventArgs e )
		{
			var entity		=	(controller as ProjectileController).Tag as Entity;
			var location	=	MathConverter.Convert( e.Location );
			var normal		=	MathConverter.Convert( e.Normal );		
			var hitEntity	=	(e.HitObject as ConvexCollidable)?.Entity.Tag as Entity;
			entity.gs.Invoke( () => Explode( entity, hitEntity, location, normal ) );
		}

		protected override void Destroy( Entity entity, ProjectileController projectileController )
		{
			physics.Remove( projectileController );
		}

		protected override void Process( Entity entity, GameTime gameTime, ProjectileController controller, KinematicState transform, ProjectileComponent projectile )
		{
			lastDeltaTime = gameTime.ElapsedSec;

			transform.Position			=	MathConverter.Convert( controller.Position );
			transform.LinearVelocity	=	MathConverter.Convert( controller.LinearVelocity );

			projectile.LifeTime	-= gameTime.ElapsedSec;

			if (projectile.LifeTime<0)
			{
				Explode( entity, null, transform.Position, Vector3.Up );
			}
		}


		/*protected override void Process( Entity entity, GameTime gameTime, KinematicState transform, ProjectileComponent projectile )
		{
			var gs = entity.gs;
			UpdateProjectile( gs, entity, transform, projectile, gameTime.ElapsedSec );
		}



		public void UpdateProjectile ( GameState gs, Entity entity, KinematicState transform, ProjectileComponent projectile, float elapsedTime )
		{
			var first	=	projectile.Steps == 0;

			var origin	=	transform.Position;
			var dir		=	projectile.Direction;
			var target	=	origin + dir * projectile.Velocity * (first ? 2 * elapsedTime : elapsedTime);

			projectile.LifeTime -= elapsedTime;
			projectile.Steps++;

			Vector3 hitNormal, hitPoint;
			Entity  hitEntity;

			var parent	=	projectile.Sender;

			if ( projectile.LifeTime <= 0 ) 
			{
				Explode( projectile.Sender, null, origin, Vector3.Up, projectile );
				FXPlayback.SpawnFX( gs, projectile.ExplosionFX, 0, origin, Vector3.Up );
				gs.Kill( entity );
			}

			if ( physics.RayCastAgainstAll( origin, target, out hitNormal, out hitPoint, out hitEntity, parent ) ) 
			{
				//	inflict damage to hit object:
				physics.ApplyImpulse( hitEntity, hitPoint, dir * projectile.Impulse );
				HealthSystem.ApplyDamage( hitEntity, projectile.Damage, projectile.Sender );

				Explode( projectile.Sender, hitEntity, hitPoint, hitNormal, projectile );
				FXPlayback.AttachFX( gs, hitEntity, projectile.ExplosionFX, 0, hitPoint, hitNormal );

				transform.Position			=	hitPoint;
				transform.LinearVelocity	=	projectile.Velocity * dir;

				gs.Kill( entity );
			} 
			else 
			{
				transform.Position			=	target;
				transform.LinearVelocity	=	projectile.Velocity * dir;
			}
		} */

		
		public void Explode ( Entity projectileEntity, Entity hitEntity, Vector3 hitPoint, Vector3 hitNormal )
		{
			Log.Message("EXPLOSION : {0} {1} {2}", projectileEntity, hitEntity, hitPoint );
			
			var gs			=	projectileEntity.gs;
			var projectile	=	projectileEntity.GetComponent<ProjectileComponent>();

			//	kill entity
			gs.Kill( projectileEntity );

			var attacker=	projectile.Sender;
			var ignore	=	projectile.Sender;
			var radius	=	projectile.Radius;
			var damage	=	projectile.Damage;
			var dir		=	projectile.Direction;

			//	play FX on explostion 
			//	and add damage on directly hit target:
			if (hitEntity!=null)
			{
				physics.ApplyImpulse( hitEntity, hitPoint, dir * projectile.Impulse );
				HealthSystem.ApplyDamage( hitEntity, projectile.Damage, projectile.Sender );
				FXPlayback.AttachFX( gs, hitEntity, projectile.ExplosionFX, 0, hitPoint, hitNormal );
			}
			else
			{
				FXPlayback.SpawnFX( gs, projectile.ExplosionFX, 0, hitPoint, hitNormal );
			}

			//	make splash damage :
			if (radius>0) 
			{
				var list = physics.WeaponOverlap( hitPoint, radius, ignore );

				foreach ( var e in list ) 
				{
					var t		=	e.GetComponent<KinematicState>();

					if (t==null) Log.Warning("Explode -- overlap with non-transform entity");

					var delta	=	t.Position - hitPoint;
					var dist	=	delta.Length() + 0.00001f;
					var ndir	=	delta / dist;
					var factor	=	MathUtil.Clamp((radius - dist) / radius, 0, 1);
					var imp		=	factor * projectile.Impulse;
					var impV	=	ndir * imp;
					var impP	=	t.Position + rand.UniformRadialDistribution(0.1f, 0.1f);
					var dmg		=	(short)( factor * damage );

					physics.ApplyImpulse( e, impP, impV );
					HealthSystem.ApplyDamage( e, projectile.Damage, attacker );
				}
			}
		}
	}
}
