using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Gameplay
{
	[Flags]
	public enum UserAction : byte 
	{
		None			=	0x00,
		Zoom			=	0x01,
		Attack			=	0x02,
		Use				=	0x04,
		SwitchWeapon	=	0x08,
		ReloadWeapon	=	0x10,
		ThrowGrenade	=	0x20,
		MeleeAtack		=	0x40,
		Jump			=	0x80,
	}	

	public class UserCommandComponent : Component
	{
		public float Yaw;
		public float Pitch;
		public float Roll;

		public float MoveForward;
		public float MoveRight;
		public float MoveUp;

		public UserAction Action;

		public string Weapon;

		public float DYaw;
		public float DPitch;

		public void SetAnglesFromQuaternion( Quaternion q )
		{
			var m = Matrix.RotationQuaternion(q);
			float yaw, pitch, roll;
			m.ToAngles( out yaw, out pitch, out roll );

			Yaw		=	yaw;
			Pitch	=	pitch;
			Roll	=	roll;

			if (float.IsNaN(Yaw)	|| float.IsInfinity(Yaw)	) Yaw	= 0;
			if (float.IsNaN(Pitch)	|| float.IsInfinity(Pitch)	) Pitch	= 0;
			if (float.IsNaN(Roll)	|| float.IsInfinity(Roll)	) Roll	= 0;
		}

		public Quaternion Rotation
		{
			get { return Quaternion.RotationYawPitchRoll( Yaw, Pitch, Roll ); }
		}

		public Matrix RotationMatrix 
		{
			get { return Matrix.RotationQuaternion( Rotation ); }
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

		
		float ShortestAngle( float start, float end, float maxValue = -1 )
		{
			start			=	MathUtil.RadiansToDegrees(start);
			end				=	MathUtil.RadiansToDegrees(end);
			var shortest	=	((((end - start) % 360) + 540) % 360) - 180;

			shortest		=	MathUtil.DegreesToRadians( shortest );

			if (maxValue>=0)
			{
				shortest = Math.Sign(shortest) * Math.Min( maxValue, Math.Abs( shortest ) );
			}

			return shortest;
		}


		public void RotateTo( Vector3 originPoint, Vector3 targetPoint, float maxYawRate, float maxPitchRate )
		{
			if (originPoint==targetPoint) 
			{
				return;
			}
			
			var dir				=	( targetPoint - originPoint ).Normalized();
			var desiredYaw		=	(float)Math.Atan2( -dir.X, -dir.Z );
			var desiredPitch	=	(float)Math.Asin( dir.Y );

			Yaw		=	Yaw   + ShortestAngle( Yaw,	desiredYaw, maxYawRate );
			Pitch	=	Pitch + ShortestAngle( Pitch, desiredPitch, maxPitchRate );
		}
	}
}
