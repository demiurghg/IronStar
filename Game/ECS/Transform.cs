using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public class Transform : Component
	{
		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ()
		{
			Position	=	Vector3.Zero;
			Rotation	=	Quaternion.Identity;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r )
		{
			Position	=	p;
			Rotation	=	r;
		}

		/// <summary>
		/// Entity position :
		/// </summary>
		public Vector3		Position;

		/// <summary>
		/// Entity rotation
		/// </summary>
		public Quaternion	Rotation;

		/// <summary>
		/// Gets entity transform matrix
		/// </summary>
		public Matrix TransformMatrix 
		{
			get { return Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Position ); }
		}

		/// <summary>
		/// Utility to transform existing ITransformable components of given type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gs"></param>
		public static void UpdateTransformables<T>( GameState gs ) where T: ITransformable, IComponent
		{
			var entities = gs.QueryEntities<T,Transform>();

			foreach ( var e in entities )
			{
				var t = e.GetComponent<Transform>();
				e.GetComponent<T>().SetTransform( t.TransformMatrix );
			}
		}
	}
}
