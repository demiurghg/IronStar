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
	}	

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
		public bool IsForward { get { return MoveForward>0; } }

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

		private Matrix RotationMatrix 
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


		public float RotateTo( Vector3 originPoint, Vector3 targetPoint, float maxYawRate, float maxPitchRate )
		{
			if (originPoint==targetPoint) 
			{
				return 0;
			}
			
			var dir				=	( targetPoint - originPoint ).Normalized();
			var desiredYaw		=	(float)Math.Atan2( -dir.X, -dir.Z );
			var desiredPitch	=	(float)Math.Asin( dir.Y );

			var shortestYaw		=	ShortestAngle( Yaw,	desiredYaw, maxYawRate );
			var shortestPitch	=	ShortestAngle( Pitch, desiredPitch, maxPitchRate );

			DesiredYaw		=	DesiredYaw   + shortestYaw;
			DesiredPitch	=	DesiredPitch + shortestPitch;

			return Math.Abs( shortestYaw ) + Math.Abs( shortestPitch );
		}
	}
}
