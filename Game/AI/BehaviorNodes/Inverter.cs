using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.AI.BehaviorTree;
using IronStar.ECS;

namespace IronStar.AI.BehaviorNodes
{
	public sealed class Inverter : Decorator
	{
		public Inverter( BTNode node ) : base(node)
		{
		}

		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			if (Node==null) return BTStatus.Failure;

			var status = Node.Tick(gameTime, entity);

			if (status==BTStatus.Success) return BTStatus.Failure;
			if (status==BTStatus.Failure) return BTStatus.Success;

			return status;
		}
	}
}
