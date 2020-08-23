using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.Animation
{
	[Flags]
	public enum SequenceMode 
	{
		None			=	0x0000,
		Immediate		=	0x0001,
		Looped			=	0x0002,
		Hold			=	0x0004,
		DontPlayTwice	=	0x0008,
	}
}
