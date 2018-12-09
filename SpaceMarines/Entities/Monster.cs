using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceMarines.Core;

namespace SpaceMarines.Entities {
	public class Monster : Entity {

		public Monster( uint id, GameWorld world, MonsterFactory factory ) : base( id, world, factory )
		{
		}
	}
}
