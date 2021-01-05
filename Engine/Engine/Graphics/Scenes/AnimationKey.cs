using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace Fusion.Engine.Graphics.Scenes
{
	public struct AnimationKey
	{
		public Quaternion Rotation;
		public Vector3 Translation;
		public float Scaling;


		public static readonly AnimationKey Identity = new AnimationKey( Matrix.Identity );


		public AnimationKey( Matrix transform )
		{
			transform.DecomposeUniformScale( out Scaling, out Rotation, out Translation );
		}


		public AnimationKey( Vector3 t, Quaternion r, float s )
		{
			Translation	=	t;
			Scaling		=	s;
			Rotation	=	r;
		}


		public Matrix Transform 
		{
			get 
			{
				return Matrix.Scaling( Scaling ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Translation );
			}
		}


		public static AnimationKey Lerp( AnimationKey start, AnimationKey end, float factor )
		{
			var t = Vector3		.Lerp ( start.Translation, end.Translation, factor );
			var r = Quaternion	.Slerp( start.Rotation	 , end.Rotation	  , factor );
			var s = MathUtil	.Lerp ( start.Scaling	 , end.Scaling	  , factor );

			return new AnimationKey( t, r, s );
		}


		public static AnimationKey operator * ( AnimationKey a, AnimationKey b )
		{
			return Multiply(a,b);
		}


		public static AnimationKey Multiply( AnimationKey a, AnimationKey b )
		{
			return new AnimationKey( a.Transform * b.Transform );

			/*var t	=	a.Translation	+	b.Translation;
			var r	=	a.Rotation		*	b.Rotation;
			var s	=	a.Scaling		+	b.Scaling;

			return new AnimationKey(t,r,s);	 */
		}
	}
}
