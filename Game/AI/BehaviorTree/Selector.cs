using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.AI.BehaviorTree
{
	public sealed class Selector : NodeComposite
	{
		IEnumerator<BTNode> current;

		public override void Initialize()
		{
			current = children.GetEnumerator();
			current.MoveNext(); // point enumerator on the first element
		}


		public override BTStatus Update(GameTime gameTime, Entity entity)
		{
			//	empty selector means that
			//	all nodes are in failed state.
			if (!children.Any()) 
			{
				Log.Warning("Selector: Empty selector -- force failure");
				return BTStatus.Failure;
			}

			while (current.MoveNext())
			{
				var status = current.Current.Tick(gameTime, entity);

				if (status!=BTStatus.Failure) 
				{
					return status;
				}

				if (!current.MoveNext())
				{
					return BTStatus.Failure;
				}
			}

			throw new InvalidOperationException("Selector -- Unexpected loop exit");
		}
	}
}
