using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Mapping;

namespace IronStar {
	class ShooterCampaign : GameComponent {

		private readonly string mapname;
		
		private readonly ContentManager content;
		private readonly RenderSystem rs;
		private GameWorld world;
		private Map map;


		public ShooterCampaign( Game game, string mapname ) : base( game )
		{
			this.mapname	=	mapname;
			this.content	=	new ContentManager( game );
		}


		public override void Initialize()
		{
			base.Initialize();

			map		=	content.Load<Map>( Path.Combine( "maps", mapname ) );
			world	=	new GameWorld( Game, map, content, new LocalMessageService(), true, Guid.NewGuid() );
		}


		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );

			if ( disposing ) {

				world?.Dispose();
				rs.RenderWorld.ClearWorld();

			}
		}


		public override void Update( GameTime gameTime )
		{
			base.Update( gameTime );
		}
	}
}
