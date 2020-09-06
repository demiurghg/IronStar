using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.BehaviorTree
{
	class Executor
	{
		public void Execute( INode root, Context context )
		{
			root.Execute(context);
		}
	}
}
