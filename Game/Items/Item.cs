using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using Fusion.Core;

namespace IronStar.Items {

	/// <summary>
	/// ITEM
	/// 
	/// Item is stateless. State is kept by IShooter implementation.
	/// 
	/// </summary>
	public abstract class Item {

		/// <summary>
		/// Called when player attempts to picks the item up.
		/// This method return false, if item decided not to be added
		/// and true otherwice.
		/// </summary>
		/// <param name="player">Player that picked item up</param>
		public abstract bool Pickup ( Entity player );

		/// <summary>
		/// Called when player or monster drops the item.
		/// On drop, creates new entity.
		/// </summary>
		public abstract Entity Drop ();

		/// <summary>
		/// Updates internal item state
		/// </summary>
		public abstract void Update ( GameTime gameTime );
	}
}