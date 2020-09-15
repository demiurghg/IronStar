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
	public sealed class Selector : NodeComposite
	{
		IEnumerator<BTNode> current;

		public Selector( params BTNode[] childNodes ) : base(childNodes) {}


		public override bool Initialize(Entity entity)
		{
			current = children.GetEnumerator();
			current.MoveNext(); // point enumerator on the first element
			return true;
		}


		public override BTStatus Update(GameTime gameTime, Entity entity, bool cancel)
		{
			//	empty selector means that
			//	all nodes are in failed state.
			if (!children.Any()) 
			{
				Log.Warning("Selector: Empty selector -- force failure");
				return BTStatus.Failure;
			}

			while (true)
			{
				var status = current.Current.Tick(gameTime, entity, cancel);

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
