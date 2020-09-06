﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.AI.BehaviorTree
{
	public class Selector : INode
	{
		public readonly NodeCollection Nodes = new NodeCollection();

		public TaskStatus Execute(Context context)
		{
			foreach ( var node in Nodes )
			{
				var status = node.Execute(context);

				if (status==TaskStatus.InProgress || status==TaskStatus.Success)
				{
					return status;
				}
			}

			return TaskStatus.Failure;
		}
	}
}
