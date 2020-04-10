﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Mathematics {

	/// <summary>
	/// Represents two-dimensional KdTree.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class KdTree3<T> {

		class Node {
			public	Vector3 Point;
			public	T		Value;
			public	Node[]	KdBranch = new Node[2]{ null, null };
		}


		Node treeRoot	=	null;

		/// <summary>
		/// Gets maximum depth
		/// </summary>
		public int MaxDepth { get; protected set; }


		/// <summary>
		/// 
		/// </summary>
		public delegate bool SearchCallback ( T current, Vector3 point, float distance );
		

		/// <summary>
		/// Constructor
		/// </summary>
		public KdTree3 ()
		{
			
		}


		/// <summary>
		/// Adds node to the tree
		/// </summary>
		/// <param name="point"></param>
		/// <param name="value"></param>
		public void Add( Vector3 point, T value )
		{
			AddNodeToKdTree( ref treeRoot, new Node() { Point = point, Value = value }, 0 );
		}



		/// <summary>
		/// Gets nearest object to target point.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="result"></param>
		/// <param name="distance"></param>
		public T Nearest ( Vector3 target )
		{
			var result	 = default(T);
			var distance = 0f;
			Nearest( treeRoot, target, ref result, ref distance, 0 );
			return result;
		}



		/// <summary>
		/// Gets nearest withing given radius object to target point.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="result"></param>
		public IEnumerable<T> NearestRadius ( Vector3 target, float radius, Func<T,bool> predicate )
		{
			List<T> result = new List<T>();

			NearestRadius( target, radius, 
				(t,p,d) => {
					if (predicate(t)) {
						result.Add(t);
					}
					return false;
				}
			);

			return result;
		}


		/// <summary>
		/// Gets nearest withing given radius object to target point that meets call back
		/// </summary>
		/// <param name="target"></param>
		/// <param name="result"></param>
		public void NearestRadius ( Vector3 target, float radius, SearchCallback callback )
		{
			NearestRadius ( treeRoot, target, radius, callback, 0 );
		}



		/// <summary>
		/// Get nearest value
		/// </summary>
		/// <param name="point"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		void Nearest ( Node root, Vector3 target, ref T result, ref float distance, int depth = 0 )
		{
			if (root==null) {
				return;
			}

			#if false
			if (root==ignoreWayPoint) {
				KdNearest( root.KdBranch[ 0 ], target, ref result, ref distance, depth+1 /*, ignoreWayPoint*/ );
				KdNearest( root.KdBranch[ 1 ], target, ref result, ref distance, depth+1 /*, ignoreWayPoint*/ );
				return;
			}
			#endif

			float dist		=	(root.Point - target).Length();
			float delta		=	KdTreeDelta ( root.Point, target, depth );
			int   branch	=	KdTreeBranch( root.Point, target, depth );

			if ( result == null || dist < distance ) {
				result = root.Value;
				distance = dist;
			}

			/*if ( dist<0.0001f ) {
			} */

			if ( root.Point==target ) {
				return;
			}

			Nearest( root.KdBranch[ branch ], target, ref result, ref distance, depth+1 /*, ignoreWayPoint*/ );

			if ( Math.Abs(delta) >= distance ) return;

			Nearest( root.KdBranch[ 1-branch ], target, ref result, ref distance, depth+1 /*, ignoreWayPoint*/ );
		}




		/// <summary>
		/// Search for all nodes within given point
		/// </summary>
		/// <param name="root"></param>
		/// <param name="target"></param>
		/// <param name="waypoints"></param>
		/// <param name="distances"></param>
		/// <param name="radius"></param>
		/// <param name="depth"></param>
		void NearestRadius ( Node root, Vector3 target, float radius, SearchCallback callback, int depth=0 )
		{
			if (root==null) return;

			float dist		=	(root.Point - target).Length();
			float delta		=	KdTreeDelta( root.Point, target, depth );
			int   branch	=	KdTreeBranch( root.Point, target, depth );

			if ( dist <= radius ) {
				if ( callback( root.Value, root.Point, dist ) ) {
					return;
				}
			}

			NearestRadius( root.KdBranch[ branch ], target, radius, callback, depth+1 );

			if ( Math.Abs(delta) >= radius ) return;

			NearestRadius( root.KdBranch[ 1-branch ], target, radius, callback, depth+1 );
		}



		/// <summary>
		/// Returns distance vetween pivot and point for given dimension
		/// </summary>
		/// <param name="pivot"></param>
		/// <param name="point"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		float KdTreeDelta ( Vector3 pivot, Vector3 point, int depth )
		{
			#if false
			float pivotX = ((depth%2)==0) ? pivot.X : pivot.Y;
			float pointX = ((depth%2)==0) ? point.X : point.Y;
			return pivotX - pointX;
			#else 

			depth = depth % 3;

			switch (depth) {
				case 0 : return pivot.X - point.X;
				case 1 : return pivot.Y - point.Y;
				case 2 : return pivot.Z - point.Z;
			}

			throw new ArgumentException("depth");
			#endif
		}



		/// <summary>
		/// Returns branch ID fot given pivot and point
		/// </summary>
		/// <param name="pivot"></param>
		/// <param name="point"></param>
		/// <param name="depth"></param>
		/// <returns></returns>
		int KdTreeBranch ( Vector3 pivot, Vector3 point, int depth )
		{
			float delta = KdTreeDelta( pivot, point, depth );
			return (delta > 0) ? 1 : 0;
		}



		/// <summary>
		/// Adds way point to Kd-Tree
		/// </summary>
		/// <param name="root"></param>
		/// <param name="wp"></param>
		/// <param name="depth"></param>
		void AddNodeToKdTree ( ref Node root, Node node, int depth )
		{
			node.KdBranch[0] = null;
			node.KdBranch[1] = null;

			if ( root==null ) {
				root = node;
				MaxDepth = Math.Max( MaxDepth, depth );
				return;
			}

			int branch = KdTreeBranch( root.Point, node.Point, depth );

			if ( root.KdBranch[ branch ]==null ) {
				root.KdBranch[ branch ] = node;
				MaxDepth = Math.Max( MaxDepth, depth );
				return;
			}

			AddNodeToKdTree( ref root.KdBranch[ branch ], node, depth + 1 );
		}


	}
}
