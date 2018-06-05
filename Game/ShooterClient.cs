using System;
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
	public partial class ShooterClient : DisposableBase, IClientInstance {

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

			game.Config.ApplySettings( this );
			game.Config.ApplySettings( gameInput );
			game.Config.ApplySettings( camera );

			gameInput.EnableControl = true;

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

		

		protected override void Dispose(bool disposing)
		{
			if ( disposing ) {

				game.Config.RetrieveSettings( this );
				game.Config.RetrieveSettings( camera );
				game.Config.RetrieveSettings( gameInput );

				#warning (game.UserInterface.Instance as ShooterInterface).ShowMenu = true;
				gameInput?.Dispose();
				world?.Dispose();
				hud?.Dispose();
			}

			base.Dispose(disposing);
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


	}
}
