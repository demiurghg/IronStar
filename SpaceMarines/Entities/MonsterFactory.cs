using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceMarines.Core;

namespace SpaceMarines.Entities {
	public class MonsterFactory : EntityFactory {

		public override Entity Spawn( uint id, GameWorld world )
		{
			return new Monster( id, world, this );
		}
	}
}
