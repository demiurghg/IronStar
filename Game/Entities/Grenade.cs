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
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;

namespace IronStar.Entities {

	public class Grenade : EntityController {

		Random rand = new Random();

		readonly float	velocity;
		readonly float	hitImpulse;
		readonly short	hitDamage;
		readonly float	hitRadius;
		readonly string	explosionFX;
		readonly string	trailFX;

		readonly short  model;
		float	lifeTime;

		readonly float totalLifeTime;

		readonly short trailFXAtom;
		readonly Space space;
		GameWorld world;

		readonly Cylinder capsule;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public Grenade ( Entity entity, GameWorld world, GrenadeFactory factory ) : base(entity,world)
		{
			this.space	=	world.PhysSpace;
			this.world	=	world;

			var atoms	=	world.Atoms;

			totalLifeTime		=	factory.DetonationTime;

			this.velocity		=	factory.Velocity	;	
			this.hitImpulse		=	factory.ExplosionImpulse	;	
			this.hitDamage		=	factory.ExplosionDamage	;	
			this.lifeTime		=	factory.DetonationTime	;	
			this.hitRadius      =   factory.ExplosionRadius   ;   
			this.explosionFX	=	factory.ExplosionFX	;
			this.trailFX		=	factory.TrailFX		;

			this.model			=	atoms[ factory.Model ];

			trailFXAtom			=	atoms[ trailFX ]; 

			var pos				=	MathConverter.Convert( entity.Position );
			var p1				=	pos + BEPUutilities.Vector3.Forward * factory.ShapeLength / 2;
			var p2				=	pos + BEPUutilities.Vector3.Backward * factory.ShapeLength / 2;
			var dir				=	MathConverter.Convert( Matrix.RotationQuaternion( entity.Rotation ).Forward );
			//capsule				=	new Capsule( p1, p2, factory.ShapeRadius, factory.Mass );
			capsule				=	new Cylinder( pos, factory.ShapeLength, factory.ShapeRadius, factory.Mass );
			
			capsule.LinearVelocity		=	dir * factory.Velocity + BEPUutilities.Vector3.Up * factory.Velocity * 0.5f;
			capsule.AngularVelocity		=	new BEPUutilities.Vector3(1,2,3);
			capsule.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			capsule.Tag			=	entity;

			space.Add( capsule );

			//	step projectile forward compensating server latency
			UpdateProjectile( entity, 1.0f / Game.GameServer.TargetFrameRate );
		}


		public override void Reset()
		{
			lifeTime = totalLifeTime;
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( float elapsedTime )
		{
			UpdateProjectile( Entity, elapsedTime );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="projEntity"></param>
		/// <param name="projectile"></param>
		public void UpdateProjectile ( Entity projEntity, float elapsedTime )
		{
			var e = Entity;

			e.Model				=	model;

			e.Position			=	MathConverter.Convert( capsule.Position ); 
			e.Rotation			=	MathConverter.Convert( capsule.Orientation ); 
			e.LinearVelocity	=	MathConverter.Convert( capsule.LinearVelocity );
			e.AngularVelocity	=	MathConverter.Convert( capsule.AngularVelocity );

			projEntity.Sfx	=	trailFXAtom;

			lifeTime -= elapsedTime;

			var parent	=	world.GetEntity( projEntity.ParentID );

			if ( lifeTime <= 0 ) {
				world.Kill( projEntity.ID );
				Explode( explosionFX, projEntity.ID, null, e.Position, Vector3.Up, hitRadius, hitDamage, hitImpulse, DamageType.RocketExplosion );
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetID"></param>
		/// <param name="attackerID"></param>
		/// <param name="damage"></param>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		/// <param name="damageType"></param>
		public override bool Damage ( uint targetID, uint attackerID, short damage, Vector3 kickImpulse, Vector3 kickPoint, DamageType damageType )
		{
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			capsule.ApplyImpulse( p, i );

			return false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="origin"></param>
		/// <param name="damage"></param>
		/// <param name="impulse"></param>
		/// <param name="damageType"></param>
		public void Explode ( string sfxName, uint attacker, Entity ignore, Vector3 hitPoint, Vector3 hitNormal, float radius, short damage, float impulse, DamageType damageType )
		{
			if (radius>0) {
				var list = world.WeaponOverlap( hitPoint, radius, ignore );

				foreach ( var e in list ) {
					var delta	= e.Position - hitPoint;
					var dist	= delta.Length() + 0.00001f;
					var ndir	= delta / dist;
					var factor	= MathUtil.Clamp((radius - dist) / radius, 0, 1);
					var imp		= factor * impulse;
					var impV	= ndir * imp;
					var impP	= e.Position + rand.UniformRadialDistribution(0.1f, 0.1f);
					var dmg		= (short)( factor * damage );

					world.InflictDamage( e, attacker, dmg, impV, impP, DamageType.RocketExplosion );
				}
			}

			world.SpawnFX( sfxName, attacker, hitPoint, hitNormal );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public override void Killed ()
		{
			space.Remove( capsule );
		}
	}
}
