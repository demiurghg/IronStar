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

		public ProjectileSystem( GameState gs, PhysicsCore physics, IParallelLooper looper ) : base(looper)
		{
			this.rand			=	new Random();
			this.physics		=	physics;
			this.weaponSystem	=	gs.GetService<WeaponSystem>();
		}


		protected override ProjectileController Create( Entity entity, Transform transform, ProjectileComponent projectile )
		{
			var position	=	MathConverter.Convert( transform.Position );
			var orient		=	MathConverter.Convert( transform.Rotation );
			var direction	=	MathConverter.Convert( transform.TransformMatrix.Forward );
			var velocity	=	direction * projectile.Velocity;

			var projectileController = new ProjectileController( position, orient, velocity, (bpe) => PhysicsCore.SkipEntityFilter( bpe, projectile.Sender ) );
			projectileController.CollisionDetected+=ProjectileController_CollisionDetected;
			projectileController.Tag = entity;

			physics.Add( projectileController );

			return projectileController;
		}

		
		private void ProjectileController_CollisionDetected( object controller, ProjectileController.CollisionDetectedEventArgs e )
		{
			var entity		=	(controller as ProjectileController).Tag as Entity;
			var direction	=	entity.GetComponent<Transform>().TransformMatrix.Forward;
			var projectile	=	entity.GetComponent<ProjectileComponent>();
			var location	=	MathConverter.Convert( e.Location );
			var normal		=	MathConverter.Convert( e.Normal );		
			var hitEntity	=	(e.HitObject as ConvexCollidable)?.Entity.Tag as Entity;

			entity.gs.Invoke( () => 
			{
				weaponSystem.InflictDamage( projectile.Sender, hitEntity, projectile.Damage, projectile.Impulse, location, direction, normal, projectile.ExplosionFX );
				weaponSystem.Explode( projectile.Sender, hitEntity, projectile.Damage, projectile.Impulse, projectile.Radius, location, normal, null );
				entity.Kill();
			});
		}

		
		protected override void Destroy( Entity entity, ProjectileController projectileController )
		{
			physics.Remove( projectileController );
		}

		
		protected override void Process( Entity entity, GameTime gameTime, ProjectileController controller, Transform transform, ProjectileComponent projectile )
		{
			lastDeltaTime = gameTime.ElapsedSec;

			if (!MathUtil.NearEqual(projectile.Velocity, 0))
			{
				PhysicsCore.UpdateTransformFromMotionState( controller.MotionState, transform );
			}

			projectile.LifeTime	-= gameTime.ElapsedSec;

			if (projectile.LifeTime<0)
			{
				weaponSystem.Explode( projectile.Sender, null, projectile.Damage, projectile.Impulse, projectile.Radius, transform.Position, Vector3.Up, projectile.ExplosionFX );
				entity.Kill();
			}
		}
	}
}
