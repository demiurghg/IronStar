using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using SpaceMarines.Mapping;
using SpaceMarines.SFX;

namespace SpaceMarines.Core {

	class GameWorld : GameComponent {
		
		ViewWorld viewWorld;
		
		public GameWorld( Game game, Map map ) : base( game )
		{
			var view = game.GetService<ViewWorld>();
			map.DrawStatic( view );
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
	}
}
