using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Collection {
	public class Octree<TValue> {

		class Node {

			public readonly Vector3 Point;
			public readonly TValue Value;
			public readonly Node[] Nodes;


			public Node ( Vector3 point, TValue value ) 
			{
				Point	=	point;
				Value	=	value;
				Nodes	=	new Node[8];
			}
		}


		Node root = null;


		/// <summary>
		/// Creates instance of octree.
		/// </summary>
		public Octree()
		{
		}


		/// <summary>
		/// Inserts value at given point
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public TValue Insert ( Vector3 point, TValue value, Comparison<TValue> comparison, Action<TValue> insert = null )
		{
			return InsertRecursiveInternal( ref root, point, value, comparison, insert );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="node"></param>
		/// <param name="point"></param>
		/// <param name="value"></param>
		/// <param name="comparison"></param>
		/// <returns></returns>
		TValue InsertRecursiveInternal ( ref Node node, Vector3 point, TValue value, Comparison<TValue> comparison, Action<TValue> insert )
		{
			if (node==null) {
				node = new Node( point, value );
				insert?.Invoke(value);
				return value;
			}

			if ( node.Point == point && comparison(node.Value, value) == 0 ) {
				return node.Value;
			}

			int branch = ClassifyPointPair( node.Point, point );

			return InsertRecursiveInternal( ref node.Nodes[branch], point, value, comparison, insert );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="v0"></param>
		/// <param name="v1"></param>
		/// <returns></returns>
		int ClassifyPointPair ( Vector3 v0, Vector3 v1 )
		{
			int res = 0;
			if (v0.X < v1.X) res |= 0x1;
			if (v0.Y < v1.Y) res |= 0x2;
			if (v0.Z < v1.Z) res |= 0x4;
			return res;
		}

	}
}
