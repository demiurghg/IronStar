using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion.Core.Mathematics;


namespace CoreIK {
	class IkBoneChain3 {

		IkBone		bone0;
		IkBone		bone1;
		IkBone		bone2;
		IkSkeleton	skeleton;

		public struct Solution {
			public Matrix	bone0;
			public Matrix	bone1;
			public Matrix	bone2;
		}

		public IkBoneChain3 ( IkSkeleton skel, string boneName0, string boneName1, string boneName2, string boneNameTerm, Vector3 bendDir )
		{
			skeleton	=	skel;

			skeleton.CheckBoneHierarchy( boneName0, boneName1 );
			skeleton.CheckBoneHierarchy( boneName1, boneName2 );
			skeleton.CheckBoneHierarchy( boneName2, boneNameTerm );

			bone0	=	skeleton.ExtractIkBone( boneName0, boneName1,	 bendDir );
			bone1	=	skeleton.ExtractIkBone( boneName1, boneName2,	 bendDir );
			bone2	=	skeleton.ExtractIkBone( boneName2, boneNameTerm, bendDir );
		}



		public void SolveThreeBones ( Vector3 origin, Vector3 target, Vector3 initBendDir, float a, float b, float c, 
			out Vector3 bend1, out Vector3 bend2, out Vector3 bendDir ) 
		{
			Vector3	dir	=	target - origin;
			Vector3	dn	=	dir.Normalized();
			float	d	=	dir.Length();
			Vector3	t	=	Vector3.Cross( dir, initBendDir );
			bendDir		=	Vector3.Cross( t, dir );
			bendDir.Normalize();


			//	See: http://en.wikipedia.org/wiki/Cyclic_quadrilateral#Parameshvara.27s_formula
			float p		=	0.5f *  ( a + b + c + d );	
			float S2	=	(p-a) * (p-b) * (p-c) * (p-d);	
			float S		=	(float)Math.Sqrt( S2 );
			float R		=	(float)Math.Sqrt( (a*c+b*d) * (a*d+b*c) * (a*b+c*d ) ) / S / 4;

			//	See: http://en.wikipedia.org/wiki/Cyclic_quadrilateral#Angle_formulas
			float cosA	=	( a*a + d*d - b*b - c*c ) / 2 / (a*d+b*c);
			float sinA	=	2 * S / (a*d+b*c);

			float cosD	=	( c*c + d*d - b*b - a*a ) / 2 / (d*c+a*b);
			float sinD	=	2 * S / (d*c+a*b);

			bend1		=	origin	+ dn * cosA * a + bendDir * sinA * a;
			bend2		=	target	- dn * cosD * c + bendDir * sinD * c;
		}


		public Solution Solve ( Vector3 origin, Vector3 target, Vector3 initBendDir )
		{
			var		a	=	bone0.Length;
			var		b	=	bone1.Length;
			var		c	=	bone2.Length;
			Vector3	bend1;
			Vector3	bend2;
			Vector3 bendDir;
			var		solution = new Solution();

			Vector3	dir	=	target - origin;
			dir		= dir.ClampLength( ( a + b + c ) * 0.98f );
			target	= origin + dir;

			SolveThreeBones( origin, target, initBendDir, a, b, c, out bend1, out bend2, out bendDir );

			solution.bone0	=	bone0.Aim( origin, bend1  - origin, bendDir );
			solution.bone1	=	bone1.Aim( bend1,  bend2  - bend1,  bendDir );
			solution.bone2	=	bone2.Aim( bend2,  target - bend2,  bendDir );

			Color	color = new Color(255,255,255,64);

			skeleton.DebugRender.DrawPoint( origin, 0.02f, color );
			skeleton.DebugRender.DrawPoint( bend1,  0.02f, color );
			skeleton.DebugRender.DrawPoint( bend2,  0.02f, color );
			skeleton.DebugRender.DrawPoint( target, 0.02f, color );
														   
			skeleton.DebugRender.DrawLine( origin, bend1,  color );
			skeleton.DebugRender.DrawLine( bend1,  bend2,  color );
			skeleton.DebugRender.DrawLine( bend2,  target, color );

			skeleton.globalBones[ bone0.Index ] = solution.bone0;
			skeleton.globalBones[ bone1.Index ] = solution.bone1;
			skeleton.globalBones[ bone2.Index ] = solution.bone2;
			//skeleton.globalBones[ boneT

			return solution;
		}


	}
}
