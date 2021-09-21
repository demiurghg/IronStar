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
	}
}
