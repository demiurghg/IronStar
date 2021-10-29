using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Gameplay.Weaponry
{
	class Ammo
	{
		readonly public string NiceName;
		readonly public int Capacity;

		public Ammo( int capacity, string niceName )
		{
			Capacity	=	capacity;
			NiceName	=	niceName;
		}
	}
}
