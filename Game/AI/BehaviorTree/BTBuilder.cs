using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.AI.BehaviorNodes;
using IronStar.ECS;

namespace IronStar.AI.BehaviorTree
{
	public sealed class BTBuilder
	{
		private BTNode currentNode = null;
		private readonly Stack<BTNode> parentStack = new Stack<BTNode>();


		private BTBuilder Do( BTNode node )
		{
			if (parentStack.Count <= 0) throw new InvalidOperationException("Can not create an unnested ActionNode, it must be leaf node.");

			parentStack.Peek().Attach(node);
			return this;
		}


		public BTBuilder Action( BTAction actionNode )
		{
			return Do( actionNode );
		}


		public BTBuilder Decorator( Decorator decoratorNode )
		{
			return Do( decoratorNode );
		}


		public BTBuilder Repeat( int count, BTNode node )
		{
			return Do( new Repeat( node, count ) );
		}


		public BTBuilder Condition( Condition condition )
		{
			if (parentStack.Count <= 0) throw new InvalidOperationException("Can not create an unnested ActionNode, it must be leaf node.");

			parentStack.Peek().Attach(condition);
			return this;
		} 


		public BTBuilder Sequence()
		{
			var sequenceNode = new Sequence();

			if (parentStack.Count > 0)
			{
				parentStack.Peek().Attach(sequenceNode);
			}

			parentStack.Push(sequenceNode);
			return this;
		}


		public BTBuilder Selector()
		{
			var selectorNode = new Selector();

			if (parentStack.Count > 0)
			{
				parentStack.Peek().Attach(selectorNode);
			}

			parentStack.Push(selectorNode);
			return this;
		}


		public BTBuilder Splice(BTNode subTree)
		{
			if (subTree == null) throw new ArgumentNullException("subTree");

			if (parentStack.Count <= 0)
			{
				throw new ApplicationException("Can not splice unnested sub-tree, there must be a parent-tree.");
			}

			parentStack.Peek().Attach(subTree);
			return this;
		}


		public BTBuilder End()
		{
			currentNode = parentStack.Pop();
			return this;
		}


		public static implicit operator BTNode(BTBuilder builder)
		{
			if (builder.currentNode == null)
			{
				throw new InvalidOperationException("Can not create behavior tree with no nodes");
			}

			return builder.currentNode;
		}
	}
}
