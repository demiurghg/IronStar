using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using BEPUphysics.EntityStateManagement;
using System.Diagnostics;

namespace IronStar.ECS
{
	public class Transform : Component
	{
		/// <summary>
		/// Entity position :
		/// </summary>
		public Vector3	Position;

		/// <summary>
		/// Entity rotation
		/// </summary>
		public Quaternion	Rotation = Quaternion.Identity;

		/// <summary>
		/// Entity scaling
		/// </summary>
		public float Scaling = 1;

		/// <summary>
		/// Increased each time when entity is teleported
		/// </summary>
		public byte TeleportCount;

		/// <summary>
		/// Entity linear velocity
		/// </summary>
		public Vector3 LinearVelocity;

		/// <summary>
		/// Entity angular velocity
		/// </summary>
		public Vector3 AngularVelocity;

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
			Position	=	p;
			Rotation	=	r;
			Scaling		=	1;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Matrix t )
		{
			Vector3 s;
			t.Decompose( out s, out Rotation, out Position );
			Scaling = s.X;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r, Vector3 velocity )
		{
			Position		=	p;
			Rotation		=	r;
			Scaling			=	1;
			LinearVelocity	=	velocity;
		}

		public Transform ( Vector3 p, Quaternion r, float scaling, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			Position		=	p;
			Rotation		=	r;
			Scaling			=	scaling;
			LinearVelocity	=	linearVelocity;
			AngularVelocity	=	angularVelocity;
		}

		/// <summary>
		/// Creates new transform
		/// </summary>
		/// <param name="p"></param>
		/// <param name="r"></param>
		public Transform ( Vector3 p, Quaternion r, float s )
		{
			Position	=	p;
			Rotation	=	r;
			Scaling		=	s;
		}

		public void Move( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.Position			=	position;
			this.Rotation			=	rotation;
			this.LinearVelocity		=	linearVelocity;
			this.AngularVelocity	=	angularVelocity;
		}

		public void Move( MotionState motionState )
		{
			this.Position			=	MathConverter.Convert( motionState.Position );
			this.Rotation			=	MathConverter.Convert( motionState.Orientation );
			this.LinearVelocity		=	MathConverter.Convert( motionState.LinearVelocity );
			this.AngularVelocity	=	MathConverter.Convert( motionState.AngularVelocity );
		}

		public void Teleport( Vector3 position, Quaternion rotation, Vector3 linearVelocity, Vector3 angularVelocity )
		{
			this.Position			=	position;
			this.Rotation			=	rotation;
			this.LinearVelocity		=	linearVelocity;
			this.AngularVelocity	=	angularVelocity;

			TeleportCount++;
		}

		public override IComponent Interpolate( IComponent previuous, float factor )
		{
			var prev	=	(Transform)previuous;
			factor		=	(prev.TeleportCount != TeleportCount) ? 1 : factor;
			
			var p	= Vector3	.Lerp ( prev.Position,	Position,	factor );
			var r	= Quaternion.Slerp( prev.Rotation,	Rotation,	factor );
			var s	= MathUtil	.Lerp ( prev.Scaling,	Scaling,	factor );
			var lv	= LinearVelocity;
			var av	= AngularVelocity;

			return new Transform()
			{
				Position		=	p,
				Rotation		=	r,
				Scaling			=	s,
				LinearVelocity	=	lv,
				AngularVelocity	=	av,
				TeleportCount	=	TeleportCount,
			};
		}

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
				Vector3 s;
				value.Decompose( out s, out Rotation, out Position );
				Scaling = s.X;
			}
		}
	}
}
