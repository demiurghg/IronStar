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
		public Vector3 Translation;
		public Quaternion Rotation;
		public Vector3 Scaling;

		public AnimationKey( Matrix transform )
		{
			transform.Decompose( out Scaling, out Rotation, out Translation );
		}


		public AnimationKey( Vector3 t, Quaternion r, Vector3 s )
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
			var s = Vector3		.Lerp ( start.Scaling	 , end.Scaling	  , factor );

			return new AnimationKey( t, r, s );
		}
	}
}
