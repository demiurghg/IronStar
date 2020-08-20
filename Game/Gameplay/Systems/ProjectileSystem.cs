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

namespace IronStar.Gameplay.Systems
{
	class ProjectileSystem : StatelessSystem<Transform,Velocity,ProjectileComponent>
	{
		readonly PhysicsCore	physics;

		public ProjectileSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}

		protected override void Process( Entity entity, GameTime gameTime, Transform transform, Velocity velocity, ProjectileComponent projectile )
		{
			var gs = entity.gs;
			UpdateProjectile( gs, entity, transform, velocity, projectile, gameTime.ElapsedSec );
		}


		public void UpdateProjectile ( GameState gs, Entity entity, Transform transform, Velocity velocity, ProjectileComponent projectile, float elapsedTime )
		{
			var origin	=	transform.Position;
			var dir		=	projectile.Direction;
			var target	=	origin + dir * projectile.Velocity * elapsedTime;

			projectile.LifeTime -= elapsedTime;

			Vector3 hitNormal, hitPoint;
			Entity  hitEntity;

			var parent	=	gs.GetEntity( projectile.SenderID );

			if ( projectile.LifeTime <= 0 ) 
			{
				gs.Kill( entity.ID );
			}

			if ( physics.RayCastAgainstAll( origin, target, out hitNormal, out hitPoint, out hitEntity, parent ) ) 
			{
				//	inflict damage to hit object:
				PhysicsCore.ApplyImpulse( hitEntity, hitPoint, dir * projectile.Impulse );
				HealthSystem.ApplyDamage( hitEntity, projectile.Damage );

				//world.InflictDamage( hitEntity, ParentID, hitDamage, DamageType.RocketExplosion, dir * hitImpulse, hitPoint );

				//Explode( explosionFX, ParentID, hitEntity, hitPoint, hitNormal, hitRadius, hitDamage, hitImpulse, DamageType.RocketExplosion );

				transform.Position	=	hitPoint;
				velocity.Linear		=	projectile.Velocity * dir;

				gs.Kill( entity.ID );
			} 
			else 
			{
				transform.Position	=	target;
				velocity.Linear		=	projectile.Velocity * dir;
			}
		}

	}
}
