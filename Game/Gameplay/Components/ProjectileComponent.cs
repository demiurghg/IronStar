using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay.Components
{
	public class ProjectileComponent : Component
	{
		public Vector3		Origin;
		public Quaternion	Rotation;
		public Vector3		Direction;
		public float		DeltaTime;

		public Entity	Attacker;
		public int		Damage;
		public float	Impulse;

		public string	ExplosionFX;
		public float	Velocity;
		public float	Radius;
		public float	LifeTime;

		public ProjectileComponent()
		{
		}

		/// <summary>
		/// Creates propelled projectile, like rocket or plasma ball
		/// </summary>
		public ProjectileComponent( Entity attacker, Vector3 origin, Quaternion rotation, Vector3 dir, float dt, float velocity, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Origin		=	origin;
			Direction	=	dir.Normalized();
			Rotation	=	rotation;
			DeltaTime	=	dt;
			Attacker	=	attacker;
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}

		/// <summary>
		/// Creates passive projectile, like grenade.
		/// Position is controlled by transform component
		/// </summary>
		public ProjectileComponent( Entity attacker, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Origin		=	Vector3.Zero;
			Direction	=	Vector3.Zero;
			DeltaTime	=	0;
			Attacker	=	attacker;
			Velocity	=	0;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}
	}
}
