using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Gameplay.Weaponry
{
	class AmmoDescriptor
	{
		readonly string NiceName;
		readonly int Capacity;

		public AmmoDescriptor( int capacity, string niceName )
		{
			Capacity	=	capacity;
			NiceName	=	niceName;
		}
	}
}
