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
		/// Called once game started.
		/// Could be used to handle static objects.
		/// </summary>
		/// <param name="gs"></param>
		void Intialize( GameState gs );

		/// <summary>
		/// Called each frame
		/// </summary>
		/// <param name="gs"></param>
		/// <param name="gameTime"></param>
		void Update ( GameState gs, GameTime gameTime );

		/// <summary>
		/// Called when game stops.
		/// Could be used to clean-up managed resources
		/// To clean-up unmanaged resources use IDisposable interface
		/// </summary>
		/// <param name="gs"></param>
		void Shutdown( GameState gs );
	}
}
