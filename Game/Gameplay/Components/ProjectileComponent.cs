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

		public ProjectileComponent( Entity attacker, float velocity, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Attacker	=	attacker;
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}
	}
}
