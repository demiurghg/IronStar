using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace IronStar.Animation.IK
{
	public sealed class Effector
	{
		public Vector3 Position;
		public Quaternion Rotation;

		readonly int	jointIndex;
		readonly Matrix	effectorToJoint;

		public Matrix EffectorMatrix { get { return Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Position ); } }

		
		public Effector( int jointIndex, Matrix defaultEffectorMatrix, Matrix defaultJointMatrix )
		{
			this.jointIndex		=	jointIndex;

			//effectorToJoint		=	

			Position			=	defaultEffectorMatrix.TranslationVector;
			Rotation			=	Quaternion.RotationMatrix( defaultEffectorMatrix );
		}


		public void DebugDraw( DebugRender dr, Matrix modelMatrix )
		{
			var matrix = EffectorMatrix * modelMatrix;

			dr.DrawBasis( matrix, 2.0f, 1 );
			dr.DrawBox( new BoundingBox(3,3,3), matrix, Color.Orange, 1 );
		}
	}
}
