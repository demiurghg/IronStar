using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.ECS
{
	public interface ISystem
	{
		/// <summary>
		/// Gets system's aspect
		/// </summary>
		Aspect GetAspect ();

		/// <summary>
		/// Called each frame
		/// </summary>
		/// <param name="gs"></param>
		/// <param name="gameTime"></param>
		void Update ( GameState gs, GameTime gameTime );
	}
}
