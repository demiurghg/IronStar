﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.BehaviorTree
{
	public abstract class Task : INode
	{
		public abstract TaskStatus Execute(Context context);
	}
}
