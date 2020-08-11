using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.Gameplay.Systems
{
	class ProjectileSystem : ISystem
	{
		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }


		public void Update( GameState gs, GameTime gameTime )
		{
			
		}
	}
}
