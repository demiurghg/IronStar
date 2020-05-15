using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Bvh
{
	public class BvhTree<TPrimitive>
	{
		sealed class SortedPrimitive
		{
			public SortedPrimitive( TPrimitive primitive, BoundingBox bbox, Vector3 centroid )
			{
				Primitive	=	primitive;
				BoundingBox	=	bbox;	
				Centroid	=	centroid;
				ZOrder		=	ComputeZOrder( centroid );
			}

			public readonly TPrimitive Primitive;
			public readonly BoundingBox BoundingBox;
			public readonly Vector3 Centroid;
			public readonly ulong ZOrder;
		}


		sealed class Node
		{
			public readonly bool IsLeaf;
			public Node Left;
			public Node Right;
			public TPrimitive Primitive;
			public BoundingBox BoundingBox;

			Node() {}

			public Node ( TPrimitive primitive, BoundingBox bbox )
			{
				IsLeaf		=	true;
				Primitive	=	primitive;
				BoundingBox	=	bbox;
			}

			public Node ( Node left, Node right )
			{
				IsLeaf		=	false;
				Primitive	=	default(TPrimitive);
				Left		=	left;
				Right		=	right;
			}
		}


		readonly Node root;


		/// <summary>
		/// Create BVH-tree from collection of primitives.
		/// </summary>
		public BvhTree( IEnumerable<TPrimitive> primitives, Func<TPrimitive,BoundingBox> bboxSelector, Func<TPrimitive,Vector3> centroidSelector )
		{
			if (!primitives.Any())
			{
				root = null;
				return;
			}

			var sortedPrimitives	=	primitives
					.Select( p0 => new SortedPrimitive( p0, bboxSelector(p0), centroidSelector(p0) ) ) 
					.OrderBy( p1 => p1.ZOrder )
					.ToArray();

			root = GenerateHierarchyRecursive( sortedPrimitives, 0, sortedPrimitives.Length-1 );

			ComputeBoundingBoxRecursive( root );
		}

										

		/// <summary>
		/// https://devblogs.nvidia.com/thinking-parallel-part-iii-tree-construction-gpu/
		/// </summary>
		Node GenerateHierarchyRecursive( SortedPrimitive[] primitives, int first, int last )
		{
			// Single object => create a leaf node.
			if (first==last)
			{
				return new Node( primitives[first].Primitive, primitives[first].BoundingBox );
			}

			// Determine where to split the range.
			int split = FindSplit( primitives, first, last );

			// Process the resulting sub-ranges recursively.
			Node left	=	GenerateHierarchyRecursive( primitives, first, split );
			Node right	=	GenerateHierarchyRecursive( primitives, split+1, last );

			return new Node( left, right );
		}



		/// <summary>
		/// https://devblogs.nvidia.com/thinking-parallel-part-iii-tree-construction-gpu/
		/// </summary>
		int FindSplit( SortedPrimitive[] primitives, int first, int last )
		{
			ulong firstCode	=	primitives[first].ZOrder;
			ulong lastCode	=	primitives[last ].ZOrder;

			// Identical Morton codes => split the range in the middle.
			if (firstCode==lastCode)
			{
				return (first+last)/2;
			}

			// Calculate the number of highest bits that are the same
			// for all objects, using the count-leading-zeros intrinsic.
			int commonPrefix = BitUtils.CountLeadingZeros(firstCode ^ lastCode);

			// Use binary search to find where the next bit differs.
			// Specifically, we are looking for the highest object that
			// shares more than commonPrefix bits with the first one.
			int split = first; // initial guess
			int step = last - first;

			do
			{
				step = (step + 1) >> 1; // exponential decrease
				int newSplit = split + step; // proposed new position

				if (newSplit < last)
				{
					ulong splitCode = primitives[newSplit].ZOrder;
					int splitPrefix = BitUtils.CountLeadingZeros(firstCode ^ splitCode);

					if (splitPrefix > commonPrefix)
					{
						split = newSplit; // accept proposal
					}
				}
			}
			while (step > 1);

			return split;
		}


		/// <summary>
		/// Computes bounding box for given node
		/// </summary>
		/// <param name="node"></param>
		void ComputeBoundingBoxRecursive( Node node )
		{
			if (node.IsLeaf) return;

			ComputeBoundingBoxRecursive( node.Left );
			ComputeBoundingBoxRecursive( node.Right );

			node.BoundingBox = BoundingBox.Merge( node.Left.BoundingBox, node.Right.BoundingBox );
		}


		/// <summary>
		/// Compute Z-order for given point
		/// </summary>
		static ulong ComputeZOrder ( Vector3 point )
		{
			const int scale = 128;
			const int bias  = 8192;
			Int3 intPoint = new Int3( 
				(int)(point.X * scale + bias),
				(int)(point.Y * scale + bias),
				(int)(point.Z * scale + bias) );

			return MortonCode.Code3Uint64( intPoint );
		}


		class StackEntry
		{
			public StackEntry( Node node, bool trivialAccept )
			{
				Node = node;
				TrivialAccept  = trivialAccept;
			}
			public Node Node;
			public bool TrivialAccept;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="root">Root of BVH tree</param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public IEnumerable<TPrimitive> Traverse( Func<BoundingBox,ContainmentType> selector )
		{
			if (root==null)	return new List<TPrimitive>();

			Stack<StackEntry> stack = new Stack<StackEntry>();
			List<TPrimitive> result = new List<TPrimitive>();

			stack.Push( new StackEntry(root,false) );

			while ( stack.Any() ) 
			{
				var current = stack.Pop();

				if (current.Node==null) continue;

				var containment		= current.TrivialAccept ? ContainmentType.Contains : selector( current.Node.BoundingBox );
				var trivialAccept	= containment == ContainmentType.Contains;

				if (containment!=ContainmentType.Disjoint)
				{
					if (current.Node.IsLeaf)
					{
						result.Add( current.Node.Primitive );
					}
					else
					{
						stack.Push( new StackEntry( current.Node.Right, trivialAccept ) );
						stack.Push( new StackEntry( current.Node.Left,  trivialAccept ) );
					}
				}
			}

			return result;
		}


		public void Traverse( Action<TPrimitive,BoundingBox> action )
		{
			var stack = new Stack<Node>();

			stack.Push( root );

			while ( stack.Any() ) 
			{
				var current = stack.Pop();

				if (current==null) continue;

				action( current.Primitive, current.BoundingBox );

				stack.Push( current.Right );
				stack.Push( current.Left );
			}
		}

	}
}
