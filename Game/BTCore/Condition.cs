using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class Condition : BTAction
	{
		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			return Check(entity) ? BTStatus.Success : BTStatus.Failure;
		}

		public abstract bool Check(Entity entity);
	}
}
