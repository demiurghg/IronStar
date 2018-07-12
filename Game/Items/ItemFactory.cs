using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using Fusion.Core;

namespace IronStar.Items {

	public abstract class ItemFactory : JsonObject {
		public abstract Item Spawn ( short clsid, GameWorld world );
	}
}