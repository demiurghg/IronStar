﻿#define COMMAND_QUEUE
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
using IronStar.Environment;
using IronStar.Gameplay.Weaponry;

namespace IronStar.Gameplay
{
	/// <summary>
	/// In MASTER mode PlayerInputSystem scans input devices and form UserCommand. UserCommand is sent to shared UserCommandQueue.
	/// Once player spawns, UserCommand is updated based on data from player transform to align view angles.
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


		private bool IsPlayerEngagedWithInGameGUI(IGameState gs)
		{
			var guiSystem   =	gs.GetService<GUISystem>();

			if (guiSystem!=null)
			{
				return guiSystem.Engaged;
			}
			else
			{
				return false;
			}
		}


		public void Update( IGameState gs, GameTime gameTime )
		{
			if (master)
			{
				var playerInput	=	gs.Game.GetService<PlayerInput>();
				var engaged		=	IsPlayerEngagedWithInGameGUI(gs);

				playerInput.UpdateUserInput( gameTime, ref userCommand, engaged );

				queue.Enqueue( userCommand );
			}
			else
			{
				var players	=	gs.QueryEntities(playerAspect);
				var uc		=	new UserCommand();
				var	action	=	UserAction.None;

				var	yaw		=	0f;
				var	pitch	=	0f;
				var	dYaw	=	0f;
				var	dPitch	=	0f;
				var	move	=	0f;
				var	strafe	=	0f;
				var count	=	0f;

				while (queue.TryDequeue(out uc))
				{
					action	=	uc.Action | action;
					dYaw	+=	uc.DeltaYaw;
					dPitch	+=	uc.DeltaPitch;
					yaw		+=	uc.Yaw;
					pitch	+=	uc.Pitch;
					move	+=	uc.Move;
					strafe	+=	uc.Strafe;
					count++;
				}

				if (count>0)
				{
					move	/=	count;
					strafe	/=	count;
					dYaw	/=	count;
					dPitch	/=	count;
					yaw		/=	count;
					pitch	/=	count;

					foreach ( var player in players )
					{
						var ucc			=	player.GetComponent<UserCommandComponent>();
						var health		=	player.GetComponent<HealthComponent>();
						var alive		=	health==null ? true : health.Health > 0;

						ucc.UpdateFromUserCommand( yaw + dYaw, pitch + dPitch, move, strafe, action );
					}
				}
			}
		}
	}
}
