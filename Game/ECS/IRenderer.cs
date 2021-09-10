using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.ECS
{
	public interface IRenderer
	{
		void Render( GameState gs, GameTime gameTime );
	}
}
