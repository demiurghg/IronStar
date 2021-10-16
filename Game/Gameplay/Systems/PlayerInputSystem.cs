#define COMMAND_QUEUE
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
using System.Collections.Concurrent;

namespace IronStar.Gameplay
{
	public class PlayerInputSystem : ISystem
	{
		Aspect			playerAspect	=	new Aspect().Include<Transform,PlayerComponent,UserCommandComponent>();
		UserCommand		userCommand		=	new UserCommand();

		public Aspect GetAspect() { return playerAspect; }

		public UserCommand LastCommand { get { return userCommand; } }


		public void Add( GameState gs, Entity e ) 
		{
			var transform	=	e.GetComponent<Transform>();
			userCommand		=	UserCommand.FromTransform( transform );
			//	#TODO #GAMEPLAY -- should I clear the command queue?
		}
		
		
		public void Remove( GameState gs, Entity e ) 
		{ 
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var playerInput	=	gs.Game.GetService<PlayerInput>();
			var players		=	gs.QueryEntities(playerAspect);
			playerInput.UpdateUserInput( gameTime, ref userCommand );

			foreach ( var player in players )
			{
				var ucc		=	player.GetComponent<UserCommandComponent>();
				var health	=	player.GetComponent<HealthComponent>();
				var alive	=	health==null ? true : health.Health > 0;

				ucc.UpdateFromUserCommand( userCommand.Yaw, userCommand.Pitch, userCommand.Move, userCommand.Strafe, userCommand.Action );
			}
		}
	}
}
