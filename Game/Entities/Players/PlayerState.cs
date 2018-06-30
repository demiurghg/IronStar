using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Entities.Players {

	[Flags]
	public enum PlayerState : byte {

		Crouching	=	0x0001,
		Zooming		=	0x0002,

	}
}
