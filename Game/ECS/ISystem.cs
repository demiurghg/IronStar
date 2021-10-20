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
		/// Called when system detects new entity with given aspect.
		/// </summary>
		/// <param name="e"></param>
		void Add( IGameState gs, Entity e );

		/// <summary>
		/// Called when system entity with given aspect is removed.
		/// </summary>
		/// <param name="e"></param>
		void Remove( IGameState gs, Entity e );

		/// <summary>
		/// Called each frame
		/// </summary>
		/// <param name="gs"></param>
		/// <param name="gameTime"></param>
		void Update ( IGameState gs, GameTime gameTime );
	}
}
