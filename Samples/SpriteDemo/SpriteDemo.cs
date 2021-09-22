using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;

namespace SpriteDemo
{
	class SpriteDemo : Game
	{
		public SpriteDemo( string gameId, string gameTitle ) : base( gameId, gameTitle )
		{
			this.AddServiceAndComponent( 100, new RenderSystem(this, true) );
		}


		protected override void Initialize()
		{
			base.Initialize();
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				//	dispose disposable stuff here
			}

			base.Dispose( disposing );
		}


		protected override void Update( GameTime gameTime )
		{
			if (Keyboard.IsKeyDown(Fusion.Core.Input.Keys.Escape))
			{
				Exit();
			}
		}
	}
}
