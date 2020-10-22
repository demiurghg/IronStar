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
	public class ProjectileComponent : IComponent
	{
		public Entity	Sender;
		public int		Damage;
		public float	Impulse;
		public Vector3	Direction;

		public string	ExplosionFX;
		public float	Velocity;
		public float	Radius;
		public float	LifeTime;
		public int		Steps = 0;

		public ProjectileComponent()
		{
		}

		public ProjectileComponent( float velocity, float radius, float lifetime, string explosionFX )
		{
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
		}

		public ProjectileComponent( float velocity, float radius, float lifetime, string explosionFX, int damage, float impulse )
		{
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
			ExplosionFX	=	explosionFX;
			Impulse		=	impulse;
			Damage		=	damage;
		}

		public void Load( GameState gs, Stream stream )	{}
		public void Save( GameState gs, Stream stream )	{}
	}
}
