using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using IronStar.ECS;
using Fusion;
using IronStar.Gameplay.Components;
using IronStar.ECSPhysics;

namespace IronStar.Gameplay
{
	public class PlayerSpawnSystem : ISystem
	{
		Random rand = new Random();

		bool warningFired = false;

		Aspect	playerAspect = new Aspect().Include<PlayerComponent,UserCommandComponent>();
		Aspect startPointAspect = new Aspect().Include<PlayerStartComponent,Transform>();

		public Aspect GetAspect() { return Aspect.Empty; }
		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		public void Update( IGameState gs, GameTime gameTime )
		{
			var e	=	gs.QueryEntities(startPointAspect).RandomOrDefault(rand);

			if (e==null)
			{
				if (!warningFired)
				{
					Log.Warning("UpdatePlayerSpawn: missing entity with PlayerStart and Transform");
					warningFired = true;
				}
				return;
			}

			var t	=	e.GetComponent<Transform>();
			var r	=	t.Rotation;
			var ps	=	e.GetComponent<PlayerStartComponent>();

			if (!ps.PlayerSpawned)
			{
				ps.PlayerSpawned = true;

				var player = gs.Spawn("PLAYER", t.Position, t.Rotation);
			}
		}
	}
}
