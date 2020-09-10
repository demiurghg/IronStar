using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public sealed class Sequence : NodeComposite
	{
		IEnumerator<BTNode> current = null;

		public override bool Initialize(Entity entity)
		{
			current = children.GetEnumerator();
			current.MoveNext(); // point enumerator on the first element
			return true;
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

			while (true)
			{
				var status = current.Current.Tick(gameTime, entity);

				if (status!=BTStatus.Success) 
				{
					return status;
				}

				if (!current.MoveNext())
				{
					return BTStatus.Success;
				}
			}

			throw new InvalidOperationException("Sequence -- Unexpected loop exit");
		}
	}
}
