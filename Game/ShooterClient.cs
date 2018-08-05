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
using Fusion.Engine.Frames;
using IronStar.Entities.Players;

namespace IronStar {
	public partial class ShooterClient : DisposableBase, IClientInstance {

		Game game;
		GameWorld world;
		UserCommand userCommand;
		GameInput gameInput;
		GameCamera camera;
		ContentManager content;
		readonly Guid userGuid;
		Map map;
		IMessageService msgsvc;

		HudFrame hud;

		public Guid UserGuid { get { return userGuid; } }


		public UserCommand UserCommand {
			get { return userCommand; }
		}



		public ShooterClient ( GameClient client, IMessageService msgsvc, Guid userGuid )
		{
			this.msgsvc		=	msgsvc;
			this.userGuid	=	userGuid;
			game			=	client.Game;
			gameInput		=	new GameInput( client.Game );
			userCommand		=	new UserCommand();
			content			=	new ContentManager( client.Game );


			game.Config.ApplySettings( this );

			gameInput.EnableControl = true;
		}



		public void Initialize( string serverInfo )
		{
			map		=   content.Load<Map>( @"maps\" + serverInfo );
			world	=	new GameWorld( game, map, content, msgsvc, true, userGuid );

			world.EntitySpawned += World_EntitySpawned;

			camera	=	new GameCamera( world, this );
			hud		=	new HudFrame( game.GetService<FrameProcessor>().RootFrame );
		}

		

		protected override void Dispose(bool disposing)
		{
			if ( disposing ) {

				game.Config.RetrieveSettings( this );

				gameInput?.Dispose();
				world?.Dispose();
				hud?.Close();
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
			hud.Player	=	world.GetPlayerEntity( this.UserGuid ) as Player;

			gameInput.Update( gameTime, world, ref userCommand );

			camera.Update( gameTime, 1 );
			
			world.PresentWorld( gameTime, 1, camera, userCommand );

			return UserCommand.GetBytes( userCommand );
		}



		public IContentPrecacher CreatePrecacher( string serverInfo )
		{
			return new GameWorld.Precacher( content, serverInfo );
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
