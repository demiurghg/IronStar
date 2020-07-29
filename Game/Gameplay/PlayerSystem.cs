using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using IronStar.ECS;
using Fusion;

namespace IronStar.Gameplay
{
	public class PlayerSystem : ISystem
	{
		Random rand = new Random();


		public Aspect GetAspect()
		{
			return Aspect.Empty();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			UpdatePlayerSpawn( gs );
			UpdatePlayerInput( gs, gameTime );
		}


		public void UpdatePlayerSpawn ( GameState gs )
		{
			var e	=	gs.QueryEntities<PlayerStart,Transform>().RandomOrDefault(rand);

			if (e==null)
			{
				Log.Warning("UpdatePlayerSpawn: missing entity with PlayerStart and Transform");
				return;
			}

			var t	=	e.GetComponent<Transform>();
			var ps	=	e.GetComponent<PlayerStart>();

			if (!ps.PlayerSpawned)
			{
				ps.PlayerSpawned = true;

				var player = gs.Spawn("PLAYER", t.Position, t.Rotation);
				player.GetComponent<UserCommand2>().SetAnglesFromQuaternion( t.Rotation );
			}
		}


		public void UpdatePlayerInput ( GameState gs, GameTime gameTime )
		{
			var playerInput	=	gs.Game.GetService<PlayerInput>();
			var players		=	gs.QueryEntities<PlayerController,UserCommand2>();

			foreach ( var player in players )
			{
				var uc	=	player.GetComponent<UserCommand2>();
				playerInput.UpdateUserInput( gameTime, uc );
			}
		}
	}
}
