using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class NodeComposite : BTNode
	{
		protected NodeCollection children = new NodeCollection();
		
		public override void Attach( BTNode node )
		{
			children.Add( node );
		}
	}
}
