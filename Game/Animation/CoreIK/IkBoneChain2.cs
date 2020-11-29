using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion.Core.Mathematics;

namespace CoreIK {

	public class IkBoneChain2 {

		public struct Solution {
			public Matrix	bone0;
			public Matrix	bone1;
			public Vector3	hitPos;
		}

		IkBone		bone0;
		IkBone		bone1;
		IkSkeleton	skeleton;

		public float MaxDistance () { return bone0.Length + bone1.Length; }
		public float MinDistance () { return Math.Abs( bone0.Length - bone1.Length ); }


		public IkBoneChain2 ( IkSkeleton skel, string boneName0, string boneName1, string boneNameTerm, Vector3 bendDir )
		{
			skeleton	=	skel;

			skeleton.CheckBoneHierarchy( boneName0, boneName1 );
			skeleton.CheckBoneHierarchy( boneName1, boneNameTerm );

			bone0	=	skeleton.ExtractIkBone( boneName0, boneName1,	 bendDir );
			bone1	=	skeleton.ExtractIkBone( boneName1, boneNameTerm, bendDir );
		}



		public void SolveTwoBones ( Vector3 origin, Vector3 target, Vector3 initBendDir, float boneLengthA, float boneLengthB, 
									 out Vector3 bendPos, out Vector3 hitPos, out Vector3 bendDir )
		{
			Vector3 targetDir = target - origin;
			Vector3 normLegDir = targetDir;
			normLegDir.Normalize();

			initBendDir.Normalize();

			float c = targetDir.Length();
			float a = boneLengthA;
			float b = boneLengthB;

            Vector3 t = Vector3.Cross(normLegDir, initBendDir);
            t.Normalize();
            bendDir	= Vector3.Cross(t, normLegDir);
            bendDir.Normalize();

			if (c>(a+b)) {
				bendPos  = origin + a * normLegDir;
				hitPos   = origin + (a+b) * normLegDir;
			} else {
				float p  = 0.5f * (a+b+c);
				float s  = (float)Math.Sqrt( p * (p-a) * (p-b) * (p-c) ); // Heron's formula of triangle area
				float d  = 2*s / c;
				float ap = (float)Math.Sqrt( a*a - d*d );
				bendPos  = origin + ap * normLegDir + bendDir * d;
				hitPos   = target;
			}

			var full = new Color(255,255,255,255);
			var half = new Color(255,255,255,128);
			DebugRender dr = skeleton.DebugRender;
			dr.DrawPoint( origin,	0.1f,		full );
			dr.DrawPoint( target,	0.1f,		half );
			dr.DrawPoint( bendPos,	0.1f,		full );
			dr.DrawPoint( hitPos,	0.1f,		full );

			dr.DrawLine	( origin,	bendPos,	half );
			dr.DrawLine	( bendPos,	hitPos,		half );
			dr.DrawLine	( origin,	target,		half );
		}



		public Solution Solve ( Vector3 origin, Vector3 target, Vector3 initBendDir )
		{
			var		b0	=	bone0;
			var		b1	=	bone1;
			Vector3	bendPos;
			Vector3 bendDir;
			Vector3 hitPos;
			var		solution = new Solution();

			SolveTwoBones( origin, target, initBendDir, bone0.Length, bone1.Length, out bendPos, out hitPos, out bendDir );

			solution.hitPos	=	hitPos;
			solution.bone0	=	bone0.Aim( origin,  bendPos - origin, bendDir );
			solution.bone1	=	bone1.Aim( bendPos, hitPos - bendPos, bendDir );

			skeleton.globalBones[ bone0.Index ] = solution.bone0;
			skeleton.globalBones[ bone1.Index ] = solution.bone1;

			return solution;
		}


		//Vector3	LinearTension ( Vector3 origin, Vector3 target )
		//{
		//    float a		= bone0.Length;
		//    float b		= bone1.Length;
		//    float max	= (a+b) * LimitSoftness;
		//    float min	= Math.Abs(a-b) / LimitSoftness;
		//    Vector3 dir = target - origin;
		//    float len = dir.Length();

		//    if ( len > max ) {
		//        return dir.Resize(len - max);
		//    }

		//    if ( len < min ) {
		//        return -dir.Resize(min - len);
		//    }

		//    return Vector3.Zero;
		//}

	}
}
