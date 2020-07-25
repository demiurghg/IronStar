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

	public class UserCommand2 : Component
	{
		public float Yaw;
		public float Pitch;
		public float Roll;

		public float MoveForward;
		public float MoveRight;
		public float MoveUp;

		public UserAction Action;

		public short Weapon;

		public float DYaw;
		public float DPitch;

		public void SetAnglesFromQuaternion( Quaternion q )
		{
			var m = Matrix.RotationQuaternion(q);
			m.ToAngles( out Yaw, out Pitch, out Roll );

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
	}
}
