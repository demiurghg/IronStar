using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
using IronStar.Core;
using Fusion.Engine.Storage;
using System.IO;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.PositionUpdating;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;

namespace IronStar.Items {

	interface IGameItem {
		
		void Pickup ( Entity ent );
		void Drop ( Entity ent );
		void Shoot ( Entity ent );
		bool IsBusy { get; }
	}

	/// <summary>
	/// 
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
	/// 
	/// </summary>
	public class ItemFactory {

		[Category("Appearance")]
		public string NiceName { get; set; } = "<NiceName>";

		[Category("Appearance")]
		public string Icon { get; set; } = "";

		[Category("Appearance")]
		public string WorldModel { get; set; } = "";

		[Category("Appearance")]
		public string ViewModel { get; set; } = "";

		[Category("Appearance")]
		public string IdleFX { get; set; } = "";

		[Category("Appearance")]
		public string PickupFX { get; set; } = "";

		[Category("Appearance")]
		public string DropFX { get; set; } = "";



		[Category("Physics")]
		public float Width { get; set; } = 1;

		[Category("Physics")]
		public float Height { get; set; } = 1;

		[Category("Physics")]
		public float Depth { get; set; } = 1;

		[Category("Physics")]
		public float Mass { get; set; } = 1;
		


		[Category("Inventory")]
		[Description("Default maximum number of given item in player's inventory")]
		public int MaxInventoryCount { get; set; } = 1;

		[Category("Inventory")]
		[Description("Number of pickable items")]
		public int PickupCount { get; set; } = 1;
		


		public void Draw( DebugRender dr, Matrix transform, Color color )
		{
			var w = Width/2;
			var h = Height/2;
			var d = Depth/2;

			dr.DrawBox( new BoundingBox( new Vector3(-w, -h, -d), new Vector3(w, h, d) ), transform, color );
			dr.DrawPoint( transform.TranslationVector, (w+h+d)/3/2, color );
		}
	}



	[ContentLoader( typeof( ItemFactory ) )]
	public sealed class ItemFactoryLoader : ContentLoader {

		static Type[] extraTypes;

		public override object Load( ContentManager content, Stream stream, Type requestedType, string assetPath, IStorage storage )
		{
			if (extraTypes==null) {
				extraTypes = Misc.GetAllSubclassesOf( typeof(ItemFactory) );
			}

			return Misc.LoadObjectFromXml( typeof(ItemFactory), stream, extraTypes );
		}
	}
}
