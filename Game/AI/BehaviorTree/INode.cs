using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.BehaviorTree
{
	public interface INode
	{
		TaskStatus Execute(Context context);
	}
}
