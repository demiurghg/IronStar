using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.ECS
{
	public interface IGameState
	{
		/// <summary>
		/// Spawns new entity
		/// </summary>
		/// <returns>New entity</returns>
		Entity Spawn();

		/// <summary>
		/// Updates game state
		/// </summary>
		/// <param name="gameTime"></param>
		void Update ( GameTime gameTime );
	}
}
