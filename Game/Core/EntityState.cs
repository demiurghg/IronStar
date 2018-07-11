using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {

	[Flags]
	public enum EntityState : int {

		Crouching	=	0x0001,
		Zooming		=	0x0002,
		HasTraction	=	0x0004,
	}
}
