using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Collection;

namespace IronStar.Gameplay
{
	public sealed class AmmoDesc 
	{
		static readonly EnumArray<AmmoType,AmmoDesc> descs = new EnumArray<AmmoType, AmmoDesc>();

		public readonly string	NiceName;
		public readonly int		Capacity;
		public readonly string	Icon;

		public AmmoDesc( string name, int capacity, string icon )
		{
			NiceName	=	name;
			Capacity	=	capacity;
			Icon		=	icon;
		}


		static AmmoDesc()
		{
			descs[ AmmoType.Bullets	] = new AmmoDesc( "Bullets"	, 200, "" );
			descs[ AmmoType.Shells	] = new AmmoDesc( "Shells"	,  50, "" );
			descs[ AmmoType.Cells	] = new AmmoDesc( "Cells"	, 200, "" );
			descs[ AmmoType.Rockets	] = new AmmoDesc( "Rockets"	,  50, "" );
			descs[ AmmoType.Slugs	] = new AmmoDesc( "Slugs"	,  50, "" );
		}


		public static AmmoDesc GetAmmo( AmmoType ammoType )
		{
			return descs[ ammoType ];
		}
	}
}
