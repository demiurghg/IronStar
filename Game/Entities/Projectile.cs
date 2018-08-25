﻿using System;
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
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;

namespace IronStar.Entities {

	public class Projectile : Entity {

		Random rand = new Random();

		public int HitDamage {
			get { return hitDamage; }
			set { hitDamage = value; }
		}

		public float HitImpulse {
			get { return hitImpulse; }
			set { hitImpulse = value; }
		}

		float	hitImpulse;
		int		hitDamage;
		readonly float	velocity;
		readonly float	hitRadius;
		readonly string	explosionFX;
		readonly string	trailFX;

		float	lifeTime;

		readonly float totalLifeTime;

		readonly short trailFXAtom;
		readonly Space space;
		GameWorld world;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public Projectile ( uint id, short clsid, GameWorld world, ProjectileFactory factory ) : base(id,clsid,world,factory)
		{
			this.space	=	world.PhysSpace;
			this.world	=	world;

			var atoms	=	world.Atoms;

			totalLifeTime		=	factory.LifeTime;

			this.velocity		=	factory.Velocity	;	
			this.hitImpulse		=	factory.Impulse	;	
			this.hitDamage		=	factory.Damage	;	
			this.lifeTime		=	factory.LifeTime	;	
			this.hitRadius      =   factory.Radius   ;   
			this.explosionFX	=	factory.ExplosionFX	;
			this.trailFX		=	factory.TrailFX		;

			trailFXAtom			=	atoms[ trailFX ]; 

			Sfx	=	trailFXAtom;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			base.Update(gameTime);

			UpdateProjectile( gameTime.ElapsedSec );
		}



		public void FixServerLag ( float dt )
		{
			//float deltaTime = 1.0f / World.Game.GetService<GameServer>().TargetFrameRate;
			UpdateProjectile( dt );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="projEntity"></param>
		/// <param name="projectile"></param>
		public void UpdateProjectile ( float elapsedTime )
		{
			var origin	=	Position;
			var dir		=	Matrix.RotationQuaternion( Rotation ).Forward;
			var target	=	origin + dir * velocity * elapsedTime;

			lifeTime -= elapsedTime;

			Vector3 hitNormal, hitPoint;
			Entity  hitEntity;

			var parent	=	world.GetEntity( ParentID );


			if ( lifeTime <= 0 ) {
				world.Kill( ID );
			}

			if ( world.RayCastAgainstAll( origin, target, out hitNormal, out hitPoint, out hitEntity, parent ) ) {

				//	inflict damage to hit object:
				world.InflictDamage( hitEntity, ParentID, hitDamage, DamageType.RocketExplosion, dir * hitImpulse, hitPoint );

				Explode( explosionFX, ParentID, hitEntity, hitPoint, hitNormal, hitRadius, hitDamage, hitImpulse, DamageType.RocketExplosion );

				Position		=	hitPoint;
				LinearVelocity	=	dir.Normalized() * velocity;

				world.Kill( ID );

			} else {
				Position		=	target;
				LinearVelocity	=	dir.Normalized() * velocity;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="origin"></param>
		/// <param name="damage"></param>
		/// <param name="impulse"></param>
		/// <param name="damageType"></param>
		public void Explode ( string sfxName, uint attackerId, Entity ignore, Vector3 hitPoint, Vector3 hitNormal, float radius, int damage, float impulse, DamageType damageType )
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

					world.InflictDamage( e, attackerId, dmg, DamageType.RocketExplosion, impV, impP );
				}
			}

			world.SpawnFX( sfxName, 0, hitPoint, hitNormal );
		}
	}
}
