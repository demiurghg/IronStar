using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;

namespace IronStar.Entities {

	public class NullEntity : Entity {

		public NullEntity( uint id, short clsid, GameWorld world, NullFactory factory ) : base( id, clsid, world, factory )
		{
			
		}
	}


	public class NullFactory : EntityFactory {

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new NullEntity( id, clsid, world, this );
		}
	}
}
