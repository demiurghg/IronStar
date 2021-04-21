using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion;
using IronStar.Mapping;

namespace IronStar.Editor.Manipulators 
{
	public class HandleIntersection 
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="pickDistance"></param>
		/// <param name="distance"></param>
		/// <param name="hitPoint"></param>
		public HandleIntersection ( bool hit, float pickDistance, float distance, Vector3 hitPoint ) 
		{
			Hit = hit; 
			PickDistance = pickDistance; 
			Distance	= distance;
			HitPoint	= hitPoint;
		}

		public readonly bool Hit;
		public readonly Vector3 HitPoint;
		public readonly float PickDistance;
		public readonly float Distance;
	

		/// <summary>
		/// Gets index of closest intersection.
		/// </summary>
		/// <param name="intersectionResults"></param>
		/// <returns></returns>
		public static int PollIntersections ( params HandleIntersection[] intersectionResults )
		{
			int index = -1;
			HandleIntersection result = null;
						

			for ( int i=0; i<intersectionResults.Length; i++ ) {
				var intersection = intersectionResults[i];

				if (intersection.Hit) {

					if (result==null) {
						index  = i;
						result = intersection;
					} else {
						if (intersection.PickDistance < result.PickDistance) {
							result = intersection;
							index = i;
						}
					}
				}
			}

			return index;
		}
	}
}
