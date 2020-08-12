using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;

namespace IronStar.ECSFactories
{
	[EntityFactory("PLAYER")]
	public class PlayerFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PlayerComponent() );
			e.AddComponent( new CharacterController(6,4,2, 24,9, 20, 10, 2.2f) );
			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			e.AddComponent( new InventoryComponent() );

			return e;
		}
	}
}
