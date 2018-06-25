﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Entities;

namespace IronStar {

	public class PlayerState {

		static Random rand = new Random();

		/// <summary>
		/// User's GUID
		/// </summary>
		public Guid Guid { get; private set; }

		/// <summary>
		/// User's info
		/// </summary>
		public string UserInfo { get; private set; }

		/// <summary>
		///	Player score.
		/// </summary>
		public int Score;

		/// <summary>
		/// 
		/// </summary>
		public bool Ready;


		float respawnTime = 0;


		public Entity PlayerEntity { get; private set; }


		/// <summary>
		/// 
		/// </summary>
		public UserCommand UserCmd = new UserCommand();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="userInfo"></param>
		public PlayerState ( Guid guid, string userInfo )
		{
			Guid		=	guid;
			UserInfo	=	userInfo;
			Score		=	0;
		}



		/// <summary>
		/// Called when player's entity was killed
		/// </summary>
		public void Killed ( Entity entity )
		{
			respawnTime	=	0;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="cmdData"></param>
		public void FeedCommand ( GameWorld world, byte[] cmdData )
		{
			var player		=	world.GetPlayerEntity(Guid);
			var oldCmd		=	UserCmd;
			UserCmd			=	UserCommand.FromBytes( cmdData );

			player?.Controller?.Move( UserCmd.MoveForward, UserCmd.MoveRight, UserCmd.MoveUp );
			UserCommand.FireUserCommandAction( 
				oldCmd, 
				UserCmd, 
				userAction1 => BeginAction(player, userAction1), 
				userAction2 => EndAction(player, userAction2) 
			);
		}



		/// <summary>
		/// Handle user button events (actions)
		/// </summary>
		/// <param name="world"></param>
		/// <param name="ctrlFlag"></param>
		void BeginAction ( Entity player, UserAction userAction )
		{
			if (player!=null) {
				var controller	= player.Controller;
				controller?.Action( userAction ); 
			} else {
				if (userAction==UserAction.Attack) {
					ForceRespawn();
				}
			}
		}


		/// <summary>
		/// Handle user button events (actions)
		/// </summary>
		/// <param name="world"></param>
		/// <param name="ctrlFlag"></param>
		void EndAction ( Entity player, UserAction userAction )
		{
			player?.Controller?.CancelAction( userAction ); 
		}



		/// <summary>
		/// 
		/// </summary>
		void ForceRespawn ()
		{
			if (respawnTime>1) {
				respawnTime = 9999;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		/// <param name="dt"></param>
		public void Update ( GameWorld world, float dt )
		{
			if (!Ready) {
				return;
			}

			if (respawnTime<20) {
				respawnTime += dt;
			}

			var player = world.GetPlayerEntity( Guid );

			if (player!=null) {
				player.Rotation		=	Quaternion.RotationYawPitchRoll( UserCmd.Yaw, UserCmd.Pitch, UserCmd.Roll );
			}

			if (player==null) {
				if ( respawnTime>3 ) {
					player	=	Respawn(world);
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="world"></param>
		public Entity Respawn (GameWorld world)
		{
			var sp = world.GetEntities()
				.Where( e1 => e1.Controller is StartPoint && (e1.Controller as StartPoint).StartPointType==StartPointType.SinglePlayer)
				.OrderBy( e => rand.Next() )
				.FirstOrDefault();					

			Entity ent;

			if (sp==null) {
				throw new GameException("No start point");
			}

			ent = world.Spawn( "player", 0, sp.Position, sp.Rotation );
			world.SpawnFX("TeleportOut", ent.ID, sp.Position );
			ent.UserGuid = Guid;

			PlayerEntity = ent;

			return ent;
		}
	}
}
