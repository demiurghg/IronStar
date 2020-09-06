using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.BehaviorTree
{
	public class Root : INode
	{
		public Root Node { get; set; }

		public TaskStatus Execute(Context context)
		{
			var node = Node;

			if (node==null) 
			{
				return TaskStatus.Failure;
			}
			else
			{
				return node.Execute(context);
			}
		}
	}
}
