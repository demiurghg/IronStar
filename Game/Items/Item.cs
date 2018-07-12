﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using Fusion.Core;

namespace IronStar.Items {

	/// <summary>
	/// 
	/// </summary>
	public class Item : JsonObject {

		/// <summary>
		/// Attempts to use item as weapon.
		/// Returns FALSE if item could not be used as weapon. TRUE otherwise.
		/// </summary>
		public virtual bool Attack ( IShooter shooter, Entity attacker ) { return false; }

		/// <summary>
		/// Called when holding entity attempts to activate item.
		/// Returns FALSE if item could not be activated. TRUE otherwise.
		/// </summary>
		public virtual bool Activate ( Entity target ) { return false; }

		/// <summary>
		/// Attempts to apply current item on another one.
		/// Return TRUE if succeded, FALSE otherwice, i.e. not applicable item (medkit on weapon)
		/// </summary>
		public virtual bool Apply ( Item target ) { return false; }

		/// <summary>
		/// Indicates, that given item could not be used any more and must be removed.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsDepleted () { return false; }

		/// <summary>
		/// Called when player attempts to picks the item up.
		/// This method return false, if item decided not to be added
		/// and true otherwice.
		/// </summary>
		public virtual bool Pickup ( Entity player ) { return false; }

		/// <summary>
		/// Called when player or monster drops the item.
		/// On drop, creates new entity.
		/// </summary>
		public virtual Entity Drop () { return null; }

		/// <summary>
		/// Updates internal item state
		/// </summary>
		public virtual void Update ( GameTime gameTime, Entity entity ) {}
	}
}