using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.Core;
using IronStar.Entities;
using Fusion.Core;

namespace IronStar.Core {

	public abstract class ItemFactory : JsonObject {

		public string NiceName { get; set; } = "#UNNAMED_ITEM";

		public abstract Item Spawn ( uint id, short clsid, GameWorld world );
	}
}