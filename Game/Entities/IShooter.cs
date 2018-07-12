using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.Entities.Players;

namespace IronStar.Entities {

	public interface IShooter {

		/// <summary>
		/// Gets actual (i.e. physical or logical) shooter point-of-view.
		/// </summary>
		Vector3		GetActualPOV  ();

		/// <summary>
		/// Gets visible shooter point-of-view
		/// </summary>
		Vector3		GetVisiblePOV ();

		/// <summary>
		/// Gets inventory.
		/// Return value could be NULL, this means that IShooter does not have inventory.
		/// I.e. Monster or FuncShooter
		/// </summary>
		/// <returns></returns>
		Inventory	GetInventory ();
	}
}
