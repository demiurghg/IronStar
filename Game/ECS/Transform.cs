using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using BEPUphysics.EntityStateManagement;

namespace IronStar.ECS
{
	public class Transform : Component
	{
		Vector3		position		=	Vector3.Zero;
		Quaternion	rotation		=	Quaternion.Identity;
		Vector3		scaling			=	Vector3.One;
		Vector3		linearVelocity	=	Vector3.Zero;
		Vector3		angularVelocity	=	Vector3.Zero;
		byte		teleportCount	=	0;

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ()
		{
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r )
		{
			position	=	p;
			rotation	=	r;
			scaling		=	Vector3.One;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Matrix t )
		{
			t.Decompose( out scaling, out rotation, out position );
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r, Vector3 velocity )
		{
			position		=	p;
			rotation		=	r;
			scaling			=	new Vector3(1,1,1);
			linearVelocity	=	velocity;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r, float s )
		{
			position	=	p;
			rotation	=	r;
			scaling		=	new Vector3(s,s,s);
		}

		public void Move( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.position			=	position;
			this.rotation			=	rotation;
			this.linearVelocity		=	linearVelocity;
			this.angularVelocity	=	angularVelocity;
		}

		public void Move( MotionState motionState )
		{
			this.position			=	MathConverter.Convert( motionState.Position );
			this.rotation			=	MathConverter.Convert( motionState.Orientation );
			this.linearVelocity		=	MathConverter.Convert( motionState.LinearVelocity );
			this.angularVelocity	=	MathConverter.Convert( motionState.AngularVelocity );
		}

		public void Teleport( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.position			=	position;
			this.rotation			=	rotation;
			this.linearVelocity		=	linearVelocity;
			this.angularVelocity	=	angularVelocity;

			teleportCount++;
		}

		/// <summary>
		/// Entity position :
		/// </summary>
		public Vector3	Position { get { return position; } }

		/// <summary>
		/// Entity rotation
		/// </summary>
		public Quaternion	Rotation { get { return rotation; } }

		/// <summary>
		/// Entity scaling
		/// </summary>
		public Vector3	Scaling { get { return scaling; } }

		/// <summary>
		/// Increased each time when entity is teleported
		/// </summary>
		public byte TeleportCount { get { return teleportCount; } }

		/// <summary>
		/// Entity linear velocity
		/// </summary>
		public Vector3 LinearVelocity { get { return linearVelocity; } }

		/// <summary>
		/// Entity angular velocity
		/// </summary>
		public Vector3 AngularVelocity { get { return angularVelocity; } }

		/// <summary>
		/// Gets entity transform matrix
		/// </summary>
		public Matrix TransformMatrix 
		{
			get 
			{ 
				return Matrix.Scaling( Scaling ) * Matrix.RotationQuaternion( Rotation ) * Matrix.Translation( Position ); 
			}
			set 
			{
				value.Decompose( out scaling, out rotation, out position );
			}
		}
	}
}
