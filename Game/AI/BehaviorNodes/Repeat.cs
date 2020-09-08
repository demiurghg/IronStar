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
	public sealed class Repeat : Decorator
	{
		readonly int repeatCount; 
		int counter = 0;

		public Repeat( BTNode node, int count ) : base(node)
		{
			if (count<0) throw new ArgumentOutOfRangeException(nameof(count), "count must be non-negative");
			repeatCount	=	count;
		}


		public override void Initialize()
		{
			counter = 0;
		}


		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			while (true)
			{
				var status = Node.Tick(gameTime, entity);

				if (status==BTStatus.InProgress) return BTStatus.InProgress;
				if (status==BTStatus.Failure) return BTStatus.Failure;

				counter++;

				if (counter>=repeatCount) return BTStatus.Success;
			}
		}
	}
}
