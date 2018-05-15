using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Engine.Common {

	public interface IUpdatable {

		/// <summary>
		/// Called when the game component should be updated.
		/// </summary>
		/// <param name="gameTime"></param>
		void Update(GameTime gameTime);

		/// <summary>
		/// Raised when the Enabled property changes.
		/// </summary>
		event EventHandler<EventArgs> EnabledChanged;

		/// <summary>
		/// Raised when the UpdateOrder property changes.
		/// </summary>
		event EventHandler<EventArgs> UpdateOrderChanged;

		/// <summary>
		/// Indicates whether the game component's 
		/// Update method should be called in Game.Update.
		/// </summary>
		bool Enabled { get; }

		/// <summary>
		/// Indicates when the game component should be updated relative to other game components. 
		/// Lower values are updated first.
		/// </summary>
		int UpdateOrder { get; }
	}
}
