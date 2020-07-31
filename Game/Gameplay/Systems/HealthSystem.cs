using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems
{
	public class HealthSystem : ISystem
	{
		public Aspect GetAspect()
		{
			return Aspect.Empty();
		}


		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}


		public void Update( GameState gs, GameTime gameTime )
		{
			var healthComponents = gs.QueryComponents<HealthComponent>();

			foreach ( var h in healthComponents )
			{
				h.ApplyDamage();
			}
		}
	}
}
