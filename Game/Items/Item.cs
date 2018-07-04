using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using IronStar.Entities.Players;

namespace IronStar.Items {

	#region
	/// *** QUESTION: IS ITEM STATELESS OR STATEFUL??? ***	
	/// *** ANSWER:   STATEFUL
	/// 
	/// *** QUESTION: WHERE IS AMMO??? ***
	/// ***	ANSWER:   ITEM KEEPS AMMO QUANTITY, BECAUSE IT IS STATEFUL
	/// 
	/// IItem
	///		Ammo
	///		Weapon
	///		Powerup
	///		Key
	///		
	/// IItemAnimator OR IItemFPV
	/// IItemHud
	///		
	/// IItemFactory
	/// 	AmmoFactory
	/// 	WeaponFactory
	/// 	PowerupFactory
	/// 	KeyFactory
	/// 
	/// GameWorld.RegisterItem ( new Railgun("appearance_description") ); -- NO!!!
	/// 
	/// All items are:
	///		* Pickable
	///		* Droppable (unless otherwise specified)
	///		* Has world apperarance (e.g. dropped weapon)
	///		* Has first-person appearance and animation (e.g. hands and weapon)
	///		* Has third-person appearance and animation (e.g. enemies)
	///		* Rigid or floating body 
	///		* Collectable and limited in inventory
	///		
	/// Item could be:
	///		* Weapon, that shoots and consume ammo
	///			* Projectile class
	///			* Muzzle, hit and trail FXes
	///			* Animation stages
	///			* Damage, damage type warmup and cooldown, reloading and overheating period,
	///			* Ammo
	///		* Ammo, that is limited and consumed by weapon
	///		* Devices that:
	///			* Spawn objects
	///			* Increase health, armor, speed, etc
	///			* Unlock the doors
	///			* Consumes something on use
	///			* Looks like weapon!
	///		* Health packs
	///		* Armor packs
	///		* Powerups that change player abilities:
	///			* Increase speed
	///			* Increase fire rate
	///			* Increase damage
	///			* Increase ammo
	///			* Increase max health, max armor, max inventory
	///			* React of external damage, attacking, walking or using of other items
	///		* Powerups could be:
	///			* Passive			- when owned change player properties forever 
	///			* Reactive			- react on particular event and change event effect, comsumes something
	///			* Active			- change player properties when enabled, consumes something
	///			* Active-Temporal	- change player properties for limited time
	///			* Active-Reactive	- when activated react on particular event and change effect, consumes something, could be deactivated
	///		* Keys that unlock the doors
	///		UIState count, percentage, image
	///		
	/// 	-----------------------------------------------------------
	/// 	
	///		** EXAMPLES **
	/// 	
	///		Instant health could be picked up if player does not have enough health.
	///		Immidiatly depleted
	/// 
	///		
	#endregion

	public abstract class Item {

		public Item ()
		{
		}

		/// <summary>
		/// The internal name of the item
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The nice name of the item
		/// </summary>
		public readonly string NiceName;

	}
}
