using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.ECS
{
	class SpawnData
	{
		public readonly Entity Entity;
		public readonly IComponent[] Components;

		public SpawnData( Entity entity, params IComponent[] components )
		{
			Entity		=	entity;
			Components	=	components.ToArray();
		}
	}
}
