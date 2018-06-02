using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using IronStar.Core;
using IronStar.Mapping;

namespace IronStar {
	class SinglePlayer : GameComponent {

		readonly GameWorld world;
		readonly ContentManager content;
		readonly Map map;

		class MessageService : IMessageService {
			public void Push( string message )
			{
				Log.Message("MSG: {0}", message);
			}

			public void Push( Guid client, string message )
			{
				Log.Message("MSG: {0} {1}", client, message);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		public SinglePlayer( Game game, string mapName ) : base( game )
		{
			var mapFile	=   Path.Combine("maps", mapName );

			world		=	new GameWorld( game, new MessageService(), true, new Guid() );
			world.InitServerAtoms();

			content		=	new ContentManager( game );

			map			=	content.Load<Map>( mapFile );
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			map.ActivateMap( world, true );
			world.SimulateWorld(1/60.0f);
			world.SimulateWorld(1/60.0f);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				world?.Dispose();
				Game.RenderSystem.RenderWorld.ClearWorld();
			}

			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			world.SimulateWorld( gameTime.ElapsedSec );
		}
	}
}
