using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Core;
using Fusion.Core.Shell;

namespace IronStar {
	class ShooterServer : IServerInstance {

		readonly Game game;
		public GameWorld World { get { return world; } }

		IMessageService msgsvc;
		GameWorld world;
		ContentManager content;
		readonly string mapName;
		Map map;

		Invoker invoker;

		
		public ShooterServer ( GameServer server, IMessageService msgsvc, string mapName )
		{
			this.game		=	server.Game;
			this.msgsvc		=	msgsvc;
			this.mapName	=	mapName;
			content			=	new ContentManager(server.Game);
			invoker			=	new Invoker(server.Game);
			#warning invoker.AddCommands(this);
		}


		void IServerInstance.Initialize()
		{
			map		=   content.Load<Map>( @"maps\" + mapName );
			world	=	new GameWorld( game, mapName, map, content, msgsvc, new Guid(), false );
		}


		public AtomCollection Atoms {
			get {
				return world.Atoms;
			}
		}


		public string ServerInfo()
		{
			return mapName;
		}


		public void Update( GameTime gameTime )
		{
			var dt = gameTime.ElapsedSec;// 1 / world.Game.GameServer.TargetFrameRate;

			world.SimulateWorld( gameTime );
		}


		public byte[] MakeSnapshot ( Guid clientGuid )
		{
			//	write world to stream :
			using ( var ms = new MemoryStream() ) {
				world.WriteToSnapshot( clientGuid, ms );
				return ms.GetBuffer();
			}
		}


		public void FeedCommand( Guid clientGuid, byte[] userCommand, uint commandID, float lag )
		{
			if ( !userCommand.Any() ) {
				return;
			}

			var command = UserCommand.FromBytes( userCommand );

			world.FeedPlayerCommand( clientGuid, command );
		}



		public void FeedNotification( Guid clientGuid, string message )
		{
			if (message.StartsWith("*chat ")) {
				msgsvc.Push(message);
			}
			if (message.StartsWith("*cmd ")) {
				var rcmd = message.Replace("*cmd ","");
				Log.Message("Remote command: {0} {1}", clientGuid, rcmd);

				var args = rcmd.SplitCommandLine().ToArray();
				var arg0 = args[0];
				var arg1 = (args.Length>1) ? args[1] : "";

				if (arg0=="give") {
					GiveItem(clientGuid, arg1);	
				}
			}
		}



		void GiveItem ( Guid clientGuid, string item )
		{
			try {

				/*var player		=	World.GetPlayerEntity( clientGuid );

				var itemFactory	=	World.Content.Load<Item>(@"items\" + item);
				var newItem		=	itemFactory.Spawn(World);

				if (newItem.Pickup( player )) {
					Log.Message("New item {0}", item);
				} else {
					Log.Warning("Can not put item {0} to inventory", item);
				} */

			} catch ( Exception e ) {
				Log.Error( e.Message );
			}
		}


		public void ClientConnected( Guid clientGuid, string userInfo )
		{
			Log.Message("Client Connected: {0} {1}", clientGuid, userInfo );
			world.PlayerConnected( clientGuid, userInfo );
		}


		public void ClientActivated( Guid clientGuid )
		{
			Log.Message("Client Activated: {0}", clientGuid );
			world.PlayerEntered( clientGuid );
		}


		public void ClientDeactivated( Guid clientGuid )
		{
			Log.Message("Client Deactivated: {0}", clientGuid );
			world.PlayerLeft( clientGuid );
		}


		public void ClientDisconnected( Guid clientGuid )
		{
			Log.Message("Client Disconnected: {0}", clientGuid );
			world.PlayerDisconnected( clientGuid );
		}


		public bool ApproveClient( Guid clientGuid, string userInfo, out string reason )
		{
			reason = "";
			return true;
			throw new NotImplementedException();
		}


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {
					world?.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
		}
		#endregion
	}
}
