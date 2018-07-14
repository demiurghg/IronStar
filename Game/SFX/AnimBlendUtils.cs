using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.SFX {
	static class AnimBlendUtils {

		public static void Blend ( Matrix[] frame1, Matrix[] frame2, float weight, Matrix[] destination )
		{
			if (frame1.Length!=frame2.Length) {
				throw new ArgumentException("frame1.Length!=frame2.Length");
			}
			
			if (frame1.Length!=destination.Length) {
				throw new ArgumentException("frame1.Length!=destination.Length");
			}

			int length = frame1.Length;


			for ( int i=0; i<length; i++ ) {

				var x0	=	frame1[i];
				var x1	=	frame2[i];

				Quaternion q0, q1;
				Vector3 t0, t1;
				Vector3 s0, s1;

				x0.Decompose( out s0, out q0, out t0 );
				x1.Decompose( out s1, out q1, out t1 );

				var q	=	Quaternion.Slerp( q0, q1, weight );
				var t	=	Vector3.Lerp( t0, t1, weight );
				var s	=	Vector3.Lerp( s0, s1, weight );

				var x	=	Matrix.Scaling( s ) * Matrix.RotationQuaternion( q ) * Matrix.Translation( t );

				destination[i]	=	x;
			}
		}

	}
}
