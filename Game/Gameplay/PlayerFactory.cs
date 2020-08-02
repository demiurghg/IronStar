using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.ECSPhysics;

namespace IronStar.Gameplay
{
	[EntityFactory("PLAYER")]
	public class PlayerFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PlayerController() );
			e.AddComponent( new CharacterController(6,4,2, 24,9, 20, 10, 2.2f) );
			e.AddComponent( new UserCommand2() );
			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}
}
