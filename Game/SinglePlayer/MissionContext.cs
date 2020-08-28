using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using IronStar.ECS;

namespace IronStar.SinglePlayer {
	public class MissionContext {

		public readonly Mission Mission;
		public readonly Game Game;
		public readonly string MapName;
		public readonly Guid UserGuid;
		public readonly ContentManager Content;

		public GameState	GameState	=	null;

		public MissionContext ( Mission mission, string map )
		{
			this.Mission	=	mission;
			this.Game		=	mission.Game;
			this.MapName	=	map;
			this.Content	=	new ContentManager( Game );
			this.UserGuid	=	Guid.NewGuid();
		}
	}
}
