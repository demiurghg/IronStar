using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Gameplay
{
	[EntityFactory("PLAYER")]
	public class PlayerFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PlayerController() );

			return e;
		}
	}
}
