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
using BEPUutilities.Threading;

namespace IronStar.Gameplay.Systems
{
	class ProjectileSystem : ProcessingSystem<ProjectileController,Transform,ProjectileComponent>
	{
		readonly PhysicsCore	physics;
		readonly Random			rand;
		readonly WeaponSystem	weaponSystem;
		float lastDeltaTime = 0;

		class HitData
		{
			public HitData( Vector3 location, Vector3 normal, Entity entity )
			{
				Location	=	location;
				Normal		=	normal;
				HitEntity	=	entity;
			}
			public Vector3 Location;
			public Vector3 Normal;
			public Entity HitEntity;
		}

		public ProjectileSystem( GameState gs, PhysicsCore physics )
		{
			this.rand			=	new Random();
			this.physics		=	physics;
			this.weaponSystem	=	gs.GetService<WeaponSystem>();
		}


		protected override ProjectileController Create( Entity entity, Transform transform, ProjectileComponent projectile )
		{
			var gs			=	entity.gs;
			var position	=	MathConverter.Convert( transform.Position );
			var orient		=	MathConverter.Convert( transform.Rotation );
			var direction	=	MathConverter.Convert( transform.TransformMatrix.Forward );
			var velocity	=	direction * projectile.Velocity;
			var attacker	=	projectile.Attacker;
			var projRay		=	new BEPUutilities.Ray( position, direction );
			var traceDist	=	projectile.Velocity * lastDeltaTime;
			var origin		=	position + direction * traceDist;

			var raycastCallback		=	new ProjectileRaycastCallback( gs, MathConverter.Convert(projRay), attacker, entity, transform );

			var hitData				=	physics.Raycast( MathConverter.Convert(projRay), traceDist, raycastCallback, RaycastOptions.SortResults );

			if (hitData!=null)
			{
				 InflictDamage( entity, projectile, hitData.HitEntity, hitData.Location, MathConverter.Convert(direction), hitData.Normal );
				 return null;
			}
			else
			{
				var projectileController = new ProjectileController( origin, orient, velocity, (bpe) => PhysicsCore.SkipEntityFilter( bpe, projectile.Attacker ) );
				projectileController.CollisionDetected+=ProjectileController_CollisionDetected;
				projectileController.Tag = entity;

				physics.Add( projectileController );
				return projectileController;
			}
		}


		private void InflictDamage( Entity entity, ProjectileComponent projectile, Entity hitEntity, Vector3 location, Vector3 direction, Vector3 normal )
		{
			weaponSystem.InflictDamage( projectile.Attacker, hitEntity, projectile.Damage, projectile.Impulse, location, direction, normal, projectile.ExplosionFX );
			weaponSystem.Explode( projectile.Attacker, hitEntity, projectile.Damage, projectile.Impulse, projectile.Radius, location, normal, null );
			entity.Kill();
		}

		
		private void ProjectileController_CollisionDetected( object controller, ProjectileController.CollisionDetectedEventArgs e )
		{
			var entity		=	(controller as ProjectileController).Tag as Entity;
			var direction	=	entity.GetComponent<Transform>().TransformMatrix.Forward;
			var projectile	=	entity.GetComponent<ProjectileComponent>();
			var location	=	MathConverter.Convert( e.Location );
			var normal		=	MathConverter.Convert( e.Normal );		
			var hitEntity	=	(e.HitObject as ConvexCollidable)?.Entity.Tag as Entity;

			InflictDamage( entity, projectile, hitEntity, location, direction, normal );
		}

		
		protected override void Destroy( Entity entity, ProjectileController projectileController )
		{
			if (projectileController!=null)
			{
				physics.Remove( projectileController );
			}
		}

		
		protected override void Process( Entity entity, GameTime gameTime, ProjectileController controller, Transform transform, ProjectileComponent projectile )
		{
			lastDeltaTime = gameTime.ElapsedSec;

			if (controller!=null)
			{
				if (!MathUtil.NearEqual(projectile.Velocity, 0))
				{
					PhysicsCore.UpdateTransformFromMotionState( controller.MotionState, transform );
				}

				projectile.LifeTime	-= gameTime.ElapsedSec;

				if (projectile.LifeTime<0)
				{
					InflictDamage( entity, projectile, null, transform.Position, transform.TransformMatrix.Forward, Vector3.Up );
				}
			}
		}


		class ProjectileRaycastCallback : IRaycastCallback<HitData>
		{
			readonly Ray ray;
			readonly Entity attacker;
			readonly Entity projectile;
			readonly ProjectileComponent projectileComponent;
			readonly Transform kinematicState;
			readonly GameState gs;
			readonly WeaponSystem ws;
			HitData hitData = null;

			public ProjectileRaycastCallback( GameState gs, Ray ray, Entity attacker, Entity projectile, Transform ks )
			{
				this.ray		=	ray;
				this.ws			=	gs.GetService<WeaponSystem>();
				this.attacker	=	attacker;
				this.projectile	=	projectile;
				this.gs			=	gs;

				this.kinematicState	=	ks;
				projectileComponent	=	projectile.GetComponent<ProjectileComponent>();
			}

			public void Begin( int count ) {}

			public bool RayHit( int index, Entity entity, Vector3 location, Vector3 normal, bool isStatic )
			{
				if (entity==attacker) 
				{
					return false;
				}
				else
				{
					hitData	=	new HitData( location, normal, isStatic ? null : entity );
					return true;
				}

				//	inflict damage to instantly hit by projectile entity :
				/*ws.InflictDamage( attacker, entity, projectileComponent.Damage, projectileComponent.Impulse, location, ray.Direction, normal, projectileComponent.ExplosionFX );
				ws.Explode( attacker, entity, projectileComponent.Damage, projectileComponent.Impulse, projectileComponent.Radius, location, normal, null );

				hitSomething = true;

				//	kill projectile, 
				//	we dont need it any more :
				projectile.Kill();	 */

				return true;
			}

			public HitData End() 
			{
				return hitData;;
			}
		}
	}
}
