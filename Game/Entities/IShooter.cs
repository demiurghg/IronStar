using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.Entities {
	public interface IShooter {
		
		/// <summary>
		/// Sets weapon cooldown
		/// </summary>
		/// <param name="cooldownPeriod"></param>
		bool TrySetCooldown ( float cooldown );

		/// <summary>
		/// Tries to consume ammo.
		/// </summary>
		/// <param name="ammoClassname"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		bool TryConsumeAmmo ( string ammoClassname, short count );

		/// <summary>
		/// Gets weapon point-of-view
		/// </summary>
		/// <returns></returns>
		Vector3 GetWeaponPOV ( bool useViewOffset );

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
