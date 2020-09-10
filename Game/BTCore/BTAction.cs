using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class BTAction : BTNode
	{
		public override void Attach( BTNode node )
		{
			throw new InvalidOperationException("Can not attach node to Action node");
		}
	}
}
