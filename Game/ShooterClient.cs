﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using IronStar.Core;
using IronStar.Client;
using IronStar.Views;

namespace IronStar {
	public class ShooterClient : IClientInstance {

		Game game;
		GameWorld world;
		UserCommand userCommand;
		GameInput gameInput;
		Hud hud;
		GameCamera camera;
		readonly Guid userGuid;
		Map map;

		public Guid UserGuid { get { return userGuid; } }


		public UserCommand UserCommand {
			get { return userCommand; }
		}



		public ShooterClient ( GameClient client, IMessageService msgsvc, Guid userGuid )
		{
			this.userGuid	=	userGuid;
			game			=	client.Game;
			world			=	new GameWorld( client.Game, msgsvc, true, userGuid );
			gameInput		=	new GameInput( client.Game );
			userCommand		=	new UserCommand();
			camera			=	new GameCamera( world, this );
			hud				=	new Hud( world );

			#warning (game.UserInterface.Instance as ShooterInterface).ShowMenu = false;
		}



		public void Initialize( string serverInfo )
		{
			hud.Initialize();
			map		=   world.Content.Load<Map>( @"maps\" + serverInfo );
			world.InitServerAtoms();
			map.ActivateMap( world, false );

			world.EntitySpawned +=World_EntitySpawned;
		}

		
		
		private void World_EntitySpawned( object sender, EntityEventArgs e )
		{
			if (e.Entity.UserGuid==userGuid) {
				userCommand.SetAnglesFromQuaternion( e.Entity.Rotation );
			}
		}



		public byte[] Update( GameTime gameTime, uint sentCommandID )
		{
			gameInput.Update( gameTime, ref userCommand );

			camera.Update( gameTime.ElapsedSec, 1 );
			
			hud.Update( gameTime.ElapsedSec, 1, world );

			world.PresentWorld( gameTime.ElapsedSec, 1, camera, userCommand );

			return UserCommand.GetBytes( userCommand );
		}



		public IContentPrecacher CreatePrecacher( string serverInfo )
		{
			return new GameWorld.Precacher( world.Content, serverInfo );
		}



		public void FeedAtoms( AtomCollection atoms )
		{
			world.Atoms = atoms;
		}



		public void FeedNotification( string message )
		{
			Log.Message("NOTIFICATION : {0}", message );
		}



		public void FeedSnapshot( GameTime serverTime, byte[] snapshot, uint ackCommandID )
		{
			using ( var ms = new MemoryStream( snapshot ) ) {
				world.ReadFromSnapshot( ms, 1 );
			}
		}



		public string UserInfo()
		{
			return "Bob" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
		}



		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {
					#warning (game.UserInterface.Instance as ShooterInterface).ShowMenu = true;
					gameInput?.Dispose();
					world?.Dispose();
					hud?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ClientWorld() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
