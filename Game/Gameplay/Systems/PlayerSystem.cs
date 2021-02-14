﻿using System;
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
	public class PlayerSystem : ISystem
	{
		Random rand = new Random();

		bool warningFired = false;


		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( GameState gs, Entity e )
		{
			//throw new NotImplementedException();
		}

		public void Remove( GameState gs, Entity e )
		{
			//throw new NotImplementedException();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			UpdatePlayerSpawn( gs );
			UpdatePlayerState( gs, gameTime );
		}


		Aspect startPointAspect = new Aspect().Include<PlayerStartComponent,Transform>();


		void UpdatePlayerSpawn ( GameState gs )
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
			var ps	=	e.GetComponent<PlayerStartComponent>();

			if (!ps.PlayerSpawned)
			{
				ps.PlayerSpawned = true;

				var player = gs.Spawn("PLAYER", t.Position, t.Rotation);
				player.GetComponent<UserCommandComponent>().SetAnglesFromQuaternion( t.Rotation );
			}
		}


		void UpdatePlayerState ( GameState gs, GameTime gameTime )
		{
			var playerInput	=	gs.Game.GetService<PlayerInput>();
			var players		=	gs.QueryEntities<PlayerComponent,UserCommandComponent>();

			foreach ( var player in players )
			{
				var uc		=	player.GetComponent<UserCommandComponent>();
				var health	=	player.GetComponent<HealthComponent>();
				var alive	=	health==null ? true : health.Health > 0;

				playerInput.UpdateUserInput( gameTime, uc, alive );
			}
		}
	}
}
