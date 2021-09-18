﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay
{
	public class UserCommandComponent : Component
	{
		public float Yaw   { get { return DesiredYaw	+ BobYaw;	 } }
		public float Pitch { get { return DesiredPitch	+ BobPitch;	 } }
		public float Roll  { get { return DesiredRoll	+ BobRoll;	 } }

		public float DesiredYaw;
		public float DesiredPitch;
		public float DesiredRoll;

		public float BobYaw;
		public float BobPitch;
		public float BobRoll;
		public float BobUp;

		public float MoveForward;
		public float MoveRight;
		public float MoveUp;

		public UserAction Action;

		public string Weapon;

		public float DYaw;
		public float DPitch;

		public bool IsMoving { get { return Math.Abs(MoveForward)>0.1f || Math.Abs(MoveRight)>0.1f; } }
		public bool IsRunning { get { return MovementVector.Length() > 0.5f; } }
		public bool IsForward { get { return MoveForward>=0; } }

		public void UpdateFromUserCommand ( float yaw, float pitch, UserAction action )
		{
			ResetControl();

			DesiredYaw		=	yaw;
			DesiredPitch	=	pitch;
			DesiredRoll		=	0;
			Action			=	action;

			if (action.HasFlag( UserAction.MoveForward ))	MoveForward++;
			if (action.HasFlag( UserAction.MoveBackward ))	MoveForward--;
			if (action.HasFlag( UserAction.StrafeRight ))	MoveRight++;
			if (action.HasFlag( UserAction.StrafeLeft ))	MoveRight--;
			if (action.HasFlag( UserAction.Jump ))			MoveUp++;
			if (action.HasFlag( UserAction.Crouch ))		MoveUp--;

			if (action.HasFlag( UserAction.Weapon1 )) Weapon = "MACHINEGUN"		;
			if (action.HasFlag( UserAction.Weapon2 )) Weapon = "MACHINEGUN2"	;
			if (action.HasFlag( UserAction.Weapon3 )) Weapon = "SHOTGUN"		;
			if (action.HasFlag( UserAction.Weapon4 )) Weapon = "PLASMAGUN"		;
			if (action.HasFlag( UserAction.Weapon5 )) Weapon = "ROCKETLAUNCHER"	;
			if (action.HasFlag( UserAction.Weapon6 )) Weapon = "MACHINEGUN"		;
			if (action.HasFlag( UserAction.Weapon7 )) Weapon = "RAILGUN"		;
			if (action.HasFlag( UserAction.Weapon8 )) Weapon = "MACHINEGUN"		;
		}

		public void ResetControl()
		{
			Weapon		=	null;
			Action		=	UserAction.None;
			MoveForward	=	0;
			MoveRight	=	0;
			MoveUp		=	0;
		}

		public void SetAnglesFromQuaternion( Quaternion q )
		{
			var m = Matrix.RotationQuaternion(q);
			float yaw, pitch, roll;
			m.ToAngles( out yaw, out pitch, out roll );

			DesiredYaw		=	yaw;
			DesiredPitch	=	pitch;
			DesiredRoll		=	roll;

			if (float.IsNaN(Yaw)	|| float.IsInfinity(Yaw)	) DesiredYaw	= 0;
			if (float.IsNaN(Pitch)	|| float.IsInfinity(Pitch)	) DesiredPitch	= 0;
			if (float.IsNaN(Roll)	|| float.IsInfinity(Roll)	) DesiredRoll	= 0;
		}

		private Quaternion Rotation
		{
			get { return Quaternion.RotationYawPitchRoll( Yaw, Pitch, Roll ); }
		}

		public Matrix RotationMatrix 
		{
			get { return Matrix.RotationQuaternion( Rotation ); }
		}

		public Matrix ComputePovTransform( Vector3 origin, Vector3 powOffset )
		{
			var bobUp = BobUp * Vector3.Up;
			return RotationMatrix * Matrix.Translation(origin + powOffset + bobUp);
		}

		public Vector3 MovementVector
		{
			get 
			{ 
				var m = Matrix.RotationYawPitchRoll( Yaw, 0, 0 );

				return m.Forward * MoveForward 
					+ m.Right * MoveRight 
					+ Vector3.Up * MoveUp;
			}
		}

		
		public float RotateTo( Vector3 originPoint, Vector3 targetPoint, float maxYawRate, float maxPitchRate )
		{
			if (originPoint==targetPoint) 
			{
				return 0;
			}
			
			var dir				=	( targetPoint - originPoint ).Normalized();
			var desiredYaw		=	(float)Math.Atan2( -dir.X, -dir.Z );
			var desiredPitch	=	(float)Math.Asin( dir.Y );

			var shortestYaw		=	MathUtil.ShortestAngle( Yaw,	desiredYaw, maxYawRate );
			var shortestPitch	=	MathUtil.ShortestAngle( Pitch, desiredPitch, maxPitchRate );

			DesiredYaw		=	DesiredYaw   + shortestYaw;
			DesiredPitch	=	DesiredPitch + shortestPitch;

			return Math.Abs( shortestYaw ) + Math.Abs( shortestPitch );
		}
	}
}
