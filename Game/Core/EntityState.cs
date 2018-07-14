using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Core {

	[Flags]
	public enum EntityState : int {

		Crouching	=	1 <<  0,
		Zooming		=	1 <<  1,
		HasTraction	=	1 <<  2,


	}
}
