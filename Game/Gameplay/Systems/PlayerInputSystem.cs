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
	public class PlayerInputSystem : ISystem, IDrawSystem
	{
		Aspect							playerAspect	=	new Aspect().Include<Transform,PlayerComponent,UserCommandComponent>();
		UserCommand						userCommand		=	new UserCommand();
		ConcurrentQueue<UserCommand>	commandQueue	=	new ConcurrentQueue<UserCommand>();

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

			foreach ( var player in players )
			{
				var ucc		=	player.GetComponent<UserCommandComponent>();
				var health	=	player.GetComponent<HealthComponent>();
				var alive	=	health==null ? true : health.Health > 0;

				UserAction action = UserAction.None;
				UserCommand uc;
				float yaw = 0, pitch = 0;
				float dYaw = 0, dPitch = 0;
				float move = 0, strafe = 0;
				bool command = false;
				int count = 0;

				#if COMMAND_QUEUE
					while (commandQueue.TryDequeue( out uc ))
					{
						action	=	uc.Action | action;
						dYaw	+=	uc.DeltaYaw;
						dPitch	+=	uc.DeltaPitch;
						yaw		=	uc.Yaw;
						pitch	=	uc.Pitch;
						move	+=	uc.Move;
						strafe	+=	uc.Strafe;
						command	=	true;
						count++;
					}

					if (count>0)
					{
						move	/=	count;
						strafe	/=	count;
						dYaw	/=	count;
						dPitch	/=	count;
					}

					if (command)
					{
						ucc.UpdateFromUserCommand( yaw + dYaw, pitch + dPitch, move, strafe,  action );
					}
				#else
					playerInput.UpdateUserInput( gameTime, ref userCommand );
					ucc.UpdateFromUserCommand( userCommand.Yaw, userCommand.Pitch, userCommand.Action );
				#endif
			}
		}

		
		public void Draw( GameState gs, GameTime gameTime )
		{
			#if COMMAND_QUEUE
				var playerInput	=	gs.Game.GetService<PlayerInput>();
				playerInput.UpdateUserInput( gameTime, ref userCommand );
				commandQueue.Enqueue( userCommand );
			#endif
		}
	}
}
