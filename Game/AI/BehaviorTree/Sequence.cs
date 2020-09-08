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
	public sealed class Sequence : NodeComposite
	{
		IEnumerator<BTNode> current = null;

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
				Log.Warning("Sequence: Empty selector -- force success");
				return BTStatus.Success;
			}

			while (current.MoveNext())
			{
				var status = current.Current.Tick(gameTime, entity);

				if (status!=BTStatus.Success) 
				{
					return status;
				}
			}

			return BTStatus.Success;
		}
	}
}
