using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceMarines.Core {
	public abstract class EntityFactory {

		public abstract Entity Spawn ( uint id, GameWorld world );

	}
}
