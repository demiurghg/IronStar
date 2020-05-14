using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Bvh
{
	public static class BvhTree
	{
		public static BvhNode<T> Construct<T> ( IEnumerable<T> primitives, Func<T,BoundingBox> bboxSelector, Func<T,Vector3> centroidSelector )
		{
			var sortedData	=	primitives
					.Select( p0 => new { 
						Primitive = p0, 
						BoundingBox = bboxSelector(p0), 
						Centroid = centroidSelector(p0),
						ZOrder = ComputeZOrder( centroidSelector(p0) ) })
					.OrderBy( p1 => p1.ZOrder )
					.ToArray();

			
			throw new NotImplementedException();
		}


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


		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="root">Root of BVH tree</param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public static IEnumerable<T> Traverse<T>( BvhNode<T> root, Func<T,BoundingBox,ContainmentType> selector )
		{
			throw new NotImplementedException();
		}
	}
}
