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
			var e	=	gs.QueryEntities<PlayerStart,Transform>().FirstOrDefault();

			if (e==null)
			{
				return;
			}

			var t	=	e.GetComponent<Transform>();
			var ps	=	e.GetComponent<PlayerStart>();

			if (!ps.PlayerSpawned)
			{
				ps.PlayerSpawned = true;
				gs.Spawn("PLAYER", t.Position, t.Rotation);
			}
		}
	}
}
