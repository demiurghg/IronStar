using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;

namespace IronStar.AI.Actions
{
	public class HasTarget : Decorator
	{
		public HasTarget( BTNode node ) : base( node )
		{
		}

		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			var targetEntity = entity.GetBlackboard()?.GetEntry<Entity>("TargetEntity");

			if (targetEntity==null)
			{
				return BTStatus.Failure;
			}
			else
			{
				return Node.Tick( gameTime, entity );
			}
		}
	}
}
