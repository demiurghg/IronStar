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
	/// <summary>
	/// In MASTER mode PlayerInputSystem scans input devices and form UserCommand. UserCommand is sent to shared UserCommandQueue.
	/// Once player spawns, UserCommand is updated based on data from player transform to align view abgles.
	/// 
	/// In SLAVE mode PlayerInputSystem read shared	UserCommandQueue and applies command to all players.
	/// #TODO UserCommand is applied to all players, its OK for single player, but for multiplayer command must be
	/// applied only to current player.
	/// </summary>
	public class PlayerInputSystem : ISystem
	{
		Aspect			playerAspect	=	new Aspect().Include<Transform,PlayerComponent,UserCommandComponent>();
		UserCommand		userCommand		=	new UserCommand();

		public Aspect GetAspect() { return playerAspect; }

		public UserCommand LastCommand 
		{ 
			get 
			{ 
				if (!master) throw new InvalidOperationException("LastCommand is available only for MASTER player input system");
				return userCommand; 
			} 
		}

		readonly bool master;
		readonly UserCommandQueue queue;


		public PlayerInputSystem(UserCommandQueue queue, bool master)
		{
			this.master	=	master;
			this.queue	=	queue;
		}


		public void Add( IGameState gs, Entity e ) 
		{
			if (master)
			{
				var transform	=	e.GetComponent<Transform>();
				userCommand		=	UserCommand.FromTransform( transform );
			}
			//	#TODO #GAMEPLAY -- should I clear the command queue?
		}
		
		
		public void Remove( IGameState gs, Entity e ) 
		{ 
		}

		UserCommand mergedCommand = new UserCommand();

		public void Update( IGameState gs, GameTime gameTime )
		{
			if (master)
			{
				var playerInput	=	gs.Game.GetService<PlayerInput>();
				playerInput.UpdateUserInput( gameTime, ref userCommand );

				queue.Enqueue( userCommand );
			}
			else
			{
				var recvCommand	=	new UserCommand();
				var players		=	gs.QueryEntities(playerAspect);

				while (queue.TryDequeue(out recvCommand))
				{
					mergedCommand = recvCommand;

					foreach ( var player in players )
					{
						var ucc		=	player.GetComponent<UserCommandComponent>();
						var health	=	player.GetComponent<HealthComponent>();
						var alive	=	health==null ? true : health.Health > 0;

						ucc.UpdateFromUserCommand( mergedCommand.Yaw + mergedCommand.DeltaYaw, mergedCommand.Pitch + mergedCommand.DeltaPitch, mergedCommand.Move, mergedCommand.Strafe, mergedCommand.Action );
					}
				}
				
			}
		}
	}
}
