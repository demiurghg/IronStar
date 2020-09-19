using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.BTCore;
using IronStar.ECS;

namespace IronStar.BTCore.Decorators
{
	public sealed class Success : Decorator
	{
		public Success( BTNode node ) : base(node)
		{
		}

		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			var status = Node.Tick(gameTime, entity, cancel);

			if (status==BTStatus.InProgress) 
			{
				return BTStatus.InProgress;
			} 
			else
			{
				return BTStatus.Success;
			}

			return status;
		}
	}
}
