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
	class ProjectileSystem : ProcessingSystem<ProjectileController,ProjectileComponent>
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


		protected override ProjectileController Create( Entity entity, ProjectileComponent projectile )
		{
			var gs			=	entity.gs;
			var position	=	projectile.Origin;
			var orient		=	projectile.Rotation;
			var direction	=	projectile.Direction;
			var velocity	=	direction * projectile.Velocity;
			var attacker	=	projectile.Attacker;
			var projRay		=	new Ray( position, direction );
			var traceDist	=	projectile.Velocity * lastDeltaTime * 2;
			var targetPos	=	position + direction * traceDist;

			var transform	=	entity.GetComponent<Transform>();

			//	no transoform is specified
			//	search by raycasting against the world
			if (transform==null)
			{
				var raycastCallback	=	new ProjectileRaycastCallback( attacker );
				var raycastResult	=	physics.Raycast( projRay, traceDist, raycastCallback, RaycastOptions.SortResults );

				if (raycastResult!=null)
				{
					 InflictDamageAndDestroyProjectile( entity, projectile, raycastResult.HitEntity, raycastResult.Location, direction, raycastResult.Normal );
					 return null;
				}
				else
				{
					transform	 = new Transform( targetPos, orient, 1.0f );
					entity.AddComponent( transform );
				}
			}

			var bpPosition	=	MathConverter.Convert( transform.Position );
			var bpRotation	=	MathConverter.Convert( transform.Rotation );
			var bpVelocity	=	MathConverter.Convert( velocity );

			var projectileController = new ProjectileController( bpPosition, bpRotation, bpVelocity, (bpe) => PhysicsCore.SkipEntityFilter( bpe, projectile.Attacker ) );
				projectileController.CollisionDetected+=ProjectileController_CollisionDetected;
				projectileController.Tag = entity;

			physics.Add( projectileController );

			return projectileController;
		}


		private void InflictDamageAndDestroyProjectile( Entity entity, ProjectileComponent projectile, Entity hitEntity, Vector3 location, Vector3 direction, Vector3 normal )
		{
			weaponSystem.InflictDamage( projectile.Attacker, hitEntity, projectile.Damage, projectile.Impulse, location, direction, normal, projectile.ExplosionFX );
			weaponSystem.Explode( projectile.Attacker, hitEntity, projectile.Damage, projectile.Impulse, projectile.Radius, location, normal, null );
			entity.Kill();
		}

		
		private void ProjectileController_CollisionDetected( object controller, ProjectileController.CollisionDetectedEventArgs e )
		{
			var entity		=	(controller as ProjectileController).Tag as Entity;
			var projectile	=	entity.GetComponent<ProjectileComponent>();
			var direction	=	projectile.Direction;
			var location	=	MathConverter.Convert( e.Location );
			var normal		=	MathConverter.Convert( e.Normal );		
			var hitEntity	=	(e.HitObject as ConvexCollidable)?.Entity.Tag as Entity;

			InflictDamageAndDestroyProjectile( entity, projectile, hitEntity, location, direction, normal );
		}

		
		protected override void Destroy( Entity entity, ProjectileController projectileController )
		{
			if (projectileController!=null)
			{
				physics.Remove( projectileController );
			}
		}

		public override void Update( IGameState gs, GameTime gameTime )
		{
			lastDeltaTime	=	gameTime.ElapsedSec;
			base.Update( gs, gameTime );
		}


		protected override void Process( Entity entity, GameTime gameTime, ProjectileController controller, ProjectileComponent projectile )
		{
			var transform	=	entity.GetComponent<Transform>();

			if (controller!=null && transform!=null)
			{
				if (!MathUtil.NearEqual(projectile.Velocity, 0))
				{
					PhysicsCore.UpdateTransformFromMotionState( controller.MotionState, transform );
				}

				projectile.LifeTime	-= gameTime.ElapsedSec;

				if (projectile.LifeTime<0)
				{
					InflictDamageAndDestroyProjectile( entity, projectile, null, transform.Position, transform.TransformMatrix.Forward, Vector3.Up );
				}
			}
		}


		class ProjectileRaycastCallback : IRaycastCallback<HitData>
		{
			readonly Entity attacker;
			HitData hitData;

			public ProjectileRaycastCallback( Entity attacker )
			{
				this.attacker	=	attacker;
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
			}

			public HitData End() 
			{
				return hitData;;
			}
		}
	}
}
