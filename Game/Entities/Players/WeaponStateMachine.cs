using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Entities.Players {


	public enum WeaponState {
		Idle,
		Firing,
		Cooldown,
		Dropping,
		Raising,
	}


	public class WeaponStateMachine {

		int weaponTimer;

		public void Attack ( int cooldown )
		{
		}


		public void Switch ()
		{
		}


		public void Reload ()
		{
		}
	}
}
