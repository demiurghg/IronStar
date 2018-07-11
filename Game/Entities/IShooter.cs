using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.Entities {

	public enum WeaponState {
		Idle,
		Cooldown,
		Empty,
		Drop,
		Raise,
	}

	public enum WeaponCommand {	
		None,
		Attack,
		Zoom,
	}

	public enum AttackResult {
		Success,
		FailCooldown,
		FailNoAmmo,
	}


	public interface IShooter {

		/// <summary>
		/// Gets weapon point-of-view
		/// </summary>
		/// <returns></returns>
		Vector3 GetWeaponPOV ( bool useViewOffset );

		/// <summary>
		/// Gets and sets weapon state timer (milliseconds)
		/// </summary>
		int WeaponTime { get; set; }

		/// <summary>
		/// Gets and sets weapon state
		/// </summary>
		WeaponState WeaponState { get; set; }

		/// <summary>
		/// Indicated whether shooter is going to attack
		/// </summary>
		WeaponCommand WeaponCommand { get; }

		/// <summary>
		/// Attempts to consume ammo from entity
		/// </summary>
		/// <param name="ammo"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		bool TryConsumeAmmo ( string ammo, short count );

		#if false
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ammo"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		bool TryAddItem ( string item, short count );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="health"></param>
		/// <returns></returns>
		bool TryAddHealth ( short health );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="armor"></param>
		/// <returns></returns>
		bool TryAddAmmo ( short armor );
		#endif
	}
}
