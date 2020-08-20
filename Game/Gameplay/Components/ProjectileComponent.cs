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
		public uint		SenderID;
		public int		Damage;
		public float	Impulse;
		public Vector3	Direction;

		public float	Velocity;
		public float	Radius;
		public float	LifeTime;

		public ProjectileComponent()
		{
		}

		public ProjectileComponent( float velocity, float radius, float lifetime )
		{
			Velocity	=	velocity;
			Radius		=	radius;
			LifeTime	=	lifetime;
		}

		public void Load( GameState gs, Stream stream )	{}
		public void Save( GameState gs, Stream stream )	{}
	}
}
