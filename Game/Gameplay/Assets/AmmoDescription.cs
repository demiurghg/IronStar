using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;

namespace IronStar.Gameplay.Assets
{
	class AmmoDescription : JsonContent
	{
		public string	Name			=	null;
		public string	NiceName		=	null;

		public int		MaxCapacity		=	200;
	}
}
