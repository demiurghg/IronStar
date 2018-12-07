using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using SpaceMarines.Mapping;
using SpaceMarines.SFX;

namespace SpaceMarines {
	class SinglePlayer : GameComponent {

		public SinglePlayer( Game game ) : base( game )
		{
		}


		public override void Initialize()
		{
			base.Initialize();
		}


		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
		}


		public override void Update( GameTime gameTime )
		{
			base.Update( gameTime );
		}


		public void StartMap ( string mapName )
		{
			var map = Game.Content.Load<Map>( @"maps\" + mapName );
			map.DrawStatic( Game.GetService<ViewWorld>() );
		}
	}
}
