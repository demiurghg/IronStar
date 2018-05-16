﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Server;
using Lidgren.Network;
using Fusion.Core;

namespace Fusion.Engine.Client {

	class ClientContext : IDisposable, IMessageService {

		public readonly Game Game;
		public readonly GameClient GameClient;
		public readonly IClientInstance Instance;
		public readonly NetClient NetClient;
		public readonly Guid Guid;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public ClientContext ( Game game )
		{
			Guid		=	Guid.NewGuid();
			Game		=	game;
			GameClient	=	game.GameClient;
			//Instance	=	game.CreateClient( game, this, Guid );



			var netConfig	=	new NetPeerConfiguration(Game.GameID);

			netConfig.AutoFlushSendQueue	=	true;
			netConfig.EnableMessageType( NetIncomingMessageType.ConnectionApproval );
			netConfig.EnableMessageType( NetIncomingMessageType.ConnectionLatencyUpdated );
			netConfig.EnableMessageType( NetIncomingMessageType.DiscoveryRequest );
			netConfig.UnreliableSizeBehaviour = NetUnreliableSizeBehaviour.NormalFragmentation;

			if (Debugger.IsAttached) {
				netConfig.ConnectionTimeout		=	float.MaxValue;	
				Log.Message("CL: Debugger is attached: ConnectionTimeout = {0} sec", netConfig.ConnectionTimeout);
			}

			NetClient	=	new NetClient( netConfig );
			NetClient.Start();
		}









		private bool disposedValue = false; // To detect redundant calls


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {
					NetClient.Shutdown("Shutdown");
					Instance?.Dispose();
				}

				disposedValue = true;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="client"></param>
		/// <param name="message"></param>
		public void Push( Guid client, string message )
		{
			Push(message);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void Push( string message )
		{
			var msg		=	NetClient.CreateMessage( message.Length + 1 );
				
			msg.Write( (byte)NetCommand.Notification );
			msg.Write( message );

			NetClient.SendMessage( msg, NetDeliveryMethod.ReliableSequenced );
		}
	}
}
