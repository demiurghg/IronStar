using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.Gameplay
{
	public class PlayerSystem : ISystem
	{
		public void Update( GameState gs, GameTime gameTime )
		{
			UpdatePlayerSpawn( gs );
		}


		public void UpdatePlayerSpawn ( GameState gs )
		{
			var ps = gs.QueryComponents<PlayerStart>().FirstOrDefault();

			if (ps==null)
			{
				throw new GameException("Missing PlayerStart entity component");
			}

			if (!ps.PlayerSpawned)
			{
				ps.PlayerSpawned = true;

				var e = gs.Spawn("PLAYER");
			}
		}
	}
}
