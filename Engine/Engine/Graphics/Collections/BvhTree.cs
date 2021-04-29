using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Collections
{
	public class BvhTree<TPrimitive>
	{
		sealed class SortedPrimitive
		{
			public SortedPrimitive( int primitiveIndex, BoundingBox bbox, Vector3 centroid )
			{
				PrimitiveIndex	=	primitiveIndex;
				BoundingBox		=	bbox;	
				Centroid		=	centroid;
				ZOrder			=	ComputeZOrder( centroid );
			}

			public readonly int PrimitiveIndex;
			public readonly BoundingBox BoundingBox;
			public readonly Vector3 Centroid;
			public readonly ulong ZOrder;
		}


		sealed class Node
		{
			public readonly bool IsLeaf;
			public readonly int PrimitiveIndex;
			public readonly Node Left;
			public readonly Node Right;
			public BoundingBox BoundingBox;

			Node() {}

			public Node ( int primitiveIndex, BoundingBox bbox )
			{
				IsLeaf			=	true;
				PrimitiveIndex	=	primitiveIndex;
				BoundingBox		=	bbox;
			}

			public Node ( Node left, Node right )
			{
				IsLeaf			=	false;
				PrimitiveIndex	=	-1;
				Left			=	left;
				Right			=	right;
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	BVH construction
		-----------------------------------------------------------------------------------------*/

		readonly Node root;
		readonly TPrimitive[] primitives;

		public TPrimitive[] Primitives { get { return primitives; } }


		/// <summary>
		/// Create BVH-tree from collection of primitives.
		/// </summary>
		public BvhTree( IEnumerable<TPrimitive> primitiveCollection, Func<TPrimitive,BoundingBox> bboxSelector, Func<TPrimitive,Vector3> centroidSelector )
		{
			if (!primitiveCollection.Any())
			{
				root = null;
				primitives = null;
				return;
			}

			primitives	=	primitiveCollection.ToArray();

			var sortedPrimitives	=	Enumerable.Range(0, primitives.Length)
					.Select( index => new SortedPrimitive( 
						index, 
						bboxSelector( primitives[ index ] ), 
						centroidSelector( primitives[ index ] ) ) ) 
					.OrderBy( p1 => p1.ZOrder )
					.ToArray();

			root = GenerateHierarchyRecursive( sortedPrimitives, 0, sortedPrimitives.Length-1 );

			ComputeBoundingBoxRecursive( root );
		}

										

		/// <summary>
		/// https://devblogs.nvidia.com/thinking-parallel-part-iii-tree-construction-gpu/
		/// </summary>
		Node GenerateHierarchyRecursive( SortedPrimitive[] sortedPrimitives, int first, int last )
		{
			// Single object => create a leaf node.
			if (first==last)
			{
				int primitiveIndex	=	sortedPrimitives[first].PrimitiveIndex;
				return new Node(primitiveIndex, sortedPrimitives[first].BoundingBox );
			}

			// Determine where to split the range.
			int split = FindSplit( sortedPrimitives, first, last );

			// Process the resulting sub-ranges recursively.
			Node left	=	GenerateHierarchyRecursive( sortedPrimitives, first, split );
			Node right	=	GenerateHierarchyRecursive( sortedPrimitives, split+1, last );

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

		/*-----------------------------------------------------------------------------------------
		 *	Compact BVH construction
		-----------------------------------------------------------------------------------------*/

		public delegate TFlatData FlattenDataSelector<TFlatData>( bool isLeaf, uint index, BoundingBox bbox ) where TFlatData: struct;

		public TFlatData[] FlattenTree<TFlatData>( FlattenDataSelector<TFlatData> selector ) where TFlatData: struct
		{
			var flatTree = new TFlatData[ Count() ];

			int offset = 0;
			FlattenBVHTreeRecursive( flatTree, selector, root, ref offset );

			return flatTree;
		}



		int FlattenBVHTreeRecursive<TFlatData>(TFlatData[] flatTree, FlattenDataSelector<TFlatData> selector, Node node, ref int offset) where TFlatData: struct
		{
			int currentOffset = offset++;
			
			if (node.IsLeaf) 
			{
				flatTree[ currentOffset ] =	selector( true, (uint)node.PrimitiveIndex, node.BoundingBox );
			} 
			else 
			{
									FlattenBVHTreeRecursive( flatTree, selector, node.Left,  ref offset);
				int rightIndex	=	FlattenBVHTreeRecursive( flatTree, selector, node.Right, ref offset);

				flatTree[ currentOffset ] = selector( false, (uint)rightIndex, node.BoundingBox );
			}

			return currentOffset;
		}

		/*-----------------------------------------------------------------------------------------
		 *	BVH traversal
		-----------------------------------------------------------------------------------------*/

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
						var primitive = primitives[ current.Node.PrimitiveIndex ];
						result.Add( primitive );
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

				if (current.PrimitiveIndex>=0)
				{
					var primitive = primitives[ current.PrimitiveIndex ];
					action( primitive, current.BoundingBox );
				}

				stack.Push( current.Right );
				stack.Push( current.Left );
			}
		}


		public int Count()
		{
			int count = 0;
			var stack = new Stack<Node>();

			stack.Push( root );

			while ( stack.Any() ) 
			{
				var current = stack.Pop();

				if (current==null) continue;

				count++;

				stack.Push( current.Right );
				stack.Push( current.Left );
			}

			return count;
		}

	}
}

