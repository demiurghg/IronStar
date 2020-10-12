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

namespace IronStar.Gameplay.Systems
{
	class ProjectileSystem : StatelessSystem<Transform,Velocity,ProjectileComponent>
	{
		readonly PhysicsCore	physics;
		readonly Random			rand;

		public ProjectileSystem( PhysicsCore physics )
		{
			this.rand		=	new Random();
			this.physics	=	physics;
		}

		public override void Add( GameState gs, Entity e ) {}

		protected override void Process( Entity entity, GameTime gameTime, Transform transform, Velocity velocity, ProjectileComponent projectile )
		{
			var gs = entity.gs;
			UpdateProjectile( gs, entity, transform, velocity, projectile, gameTime.ElapsedSec );
		}


		public void UpdateProjectile ( GameState gs, Entity entity, Transform transform, Velocity velocity, ProjectileComponent projectile, float elapsedTime )
		{
			var first	=	projectile.Steps == 0;

			var origin	=	transform.Position;
			var dir		=	projectile.Direction;
			var target	=	origin + dir * projectile.Velocity * (first ? 2 * elapsedTime : elapsedTime);

			projectile.LifeTime -= elapsedTime;
			projectile.Steps++;

			Vector3 hitNormal, hitPoint;
			Entity  hitEntity;

			var parent	=	gs.GetEntity( projectile.SenderID );

			if ( projectile.LifeTime <= 0 ) 
			{
				Explode( projectile.SenderID, null, origin, Vector3.Up, projectile );
				FXPlayback.SpawnFX( gs, projectile.ExplosionFX, 0, origin, Vector3.Up );
				gs.Kill( entity );
			}

			if ( physics.RayCastAgainstAll( origin, target, out hitNormal, out hitPoint, out hitEntity, parent ) ) 
			{
				//	inflict damage to hit object:
				PhysicsCore.ApplyImpulse( hitEntity, hitPoint, dir * projectile.Impulse );
				HealthSystem.ApplyDamage( hitEntity, projectile.Damage );

				Explode( projectile.SenderID, hitEntity, hitPoint, hitNormal, projectile );
				FXPlayback.AttachFX( gs, hitEntity, projectile.ExplosionFX, 0, hitPoint, hitNormal );

				transform.Position	=	hitPoint;
				velocity.Linear		=	projectile.Velocity * dir;

				gs.Kill( entity );
			} 
			else 
			{
				transform.Position	=	target;
				velocity.Linear		=	projectile.Velocity * dir;
			}
		}

		
		public void Explode ( uint attackerId, Entity ignore, Vector3 hitPoint, Vector3 hitNormal, ProjectileComponent projectile )
		{
			var radius	=	projectile.Radius;
			var damage	=	projectile.Damage;

			if (radius>0) 
			{
				var list = physics.WeaponOverlap( hitPoint, radius, ignore );

				foreach ( var e in list ) 
				{
					var t		=	e.GetComponent<Transform>();

					if (t==null) Log.Warning("Explode -- overlap with non-transform entity");

					var delta	=	t.Position - hitPoint;
					var dist	=	delta.Length() + 0.00001f;
					var ndir	=	delta / dist;
					var factor	=	MathUtil.Clamp((radius - dist) / radius, 0, 1);
					var imp		=	factor * projectile.Impulse;
					var impV	=	ndir * imp;
					var impP	=	t.Position + rand.UniformRadialDistribution(0.1f, 0.1f);
					var dmg		=	(short)( factor * damage );

					PhysicsCore.ApplyImpulse( e, impP, impV );
					HealthSystem.ApplyDamage( e, projectile.Damage );
				}
			}
		}
	}
}
