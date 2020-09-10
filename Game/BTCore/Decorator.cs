using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class Decorator : BTNode
	{
		protected readonly BTNode Node;

		public Decorator( BTNode node )
		{
			if (node==null) throw new ArgumentNullException(nameof(node));
			Node	=	node;
		}


		public override void Attach( BTNode node )
		{
			Node.Attach( node );
		}
	}
}
