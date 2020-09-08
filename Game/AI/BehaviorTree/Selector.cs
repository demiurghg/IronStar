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
			}

			return BTStatus.Failure;
		}
	}
}
