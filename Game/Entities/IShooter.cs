using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.Entities.Players;

namespace IronStar.Entities {

	[Obsolete("Make GetPOV for all entities")]
	public interface IShooter {

		/// <summary>
		/// Gets actual (i.e. physical or logical) shooter point-of-view.
		/// </summary>
		Vector3		GetActualPOV  ();

		/// <summary>
		/// Gets visible shooter point-of-view
		/// </summary>
		Vector3		GetVisiblePOV ();
	}
}
