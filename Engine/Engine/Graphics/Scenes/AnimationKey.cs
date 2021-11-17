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


		public static void Lerp( ref AnimationKey start, ref AnimationKey end, float factor, ref AnimationKey result )
		{
			result.Translation	=	Vector3		.Lerp ( start.Translation, end.Translation, factor );
			result.Rotation		=	Quaternion	.Slerp( start.Rotation	 , end.Rotation	  , factor );
			result.Scaling		=	MathUtil	.Lerp ( start.Scaling	 , end.Scaling	  , factor );
		}


		public static AnimationKey operator * ( AnimationKey a, AnimationKey b )
		{
			return Multiply(a,b);
		}


		public static void Multiply( ref AnimationKey a, ref AnimationKey b, ref AnimationKey combined )
		{
			combined.Scaling = 1;
			Vector3 intermediate;
			Quaternion.Transform(ref a.Translation, ref b.Rotation, out intermediate);
			Vector3.Add(ref intermediate, ref b.Translation, out combined.Translation);
			Quaternion.Multiply(ref a.Rotation, ref b.Rotation, out combined.Rotation);
		}


		public static void CopyTransforms( AnimationKey[] source, Matrix[] destination )
		{
			int count = Math.Min( source.Length, destination.Length );
			
			for (int i=0; i<count; i++)
			{
				destination[i] = source[i].Transform;
			}
		}


		public static AnimationKey Multiply( AnimationKey a, AnimationKey b )
		{
			var key = new AnimationKey();

			Multiply( ref a, ref b, ref key );

			return key;
		}
	}
}
