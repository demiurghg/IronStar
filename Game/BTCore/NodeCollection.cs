﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.BTCore
{
	public class NodeCollection : List<BTNode>
	{
		public NodeCollection()
		{
		}

		public NodeCollection( IEnumerable<BTNode> other ) : base(other)
		{
		}
	}
}