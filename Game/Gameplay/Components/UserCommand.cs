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
	public enum UserAction : uint 
	{
		None			=	0,
							
		Zoom			=	0x00000001,
		Attack			=	0x00000002,
		Use				=	0x00000004,
		SwitchWeapon	=	0x00000008,
		ReloadWeapon	=	0x00000010,
		ThrowGrenade	=	0x00000020,
		MeleeAtack		=	0x00000040,
						//	0x00080000,

		MoveForward		=	0x00000100,
		MoveBackward	=	0x00000200,
		StrafeRight		=	0x00000400,
		StrafeLeft		=	0x00000800,

		Jump			=	0x00001000,
		Crouch			=	0x00002000,
		Walk			=	0x00004000,
						//	0x00080000,
					
		Weapon1			=	0x00200000,
		Weapon2			=	0x00400000,
		Weapon3			=	0x00800000,
		Weapon4			=	0x01000000,
		Weapon5			=	0x02000000,
		Weapon6			=	0x04000000,
		Weapon7			=	0x08000000,
		Weapon8			=	0x10000000,
	}	


	public struct UserCommand
	{
		public UserAction	Action;
		public float		Yaw;
		public float		Pitch;

		public static UserCommand FromTransform( Transform t )
		{
			var uc = new UserCommand();

			if (t!=null)
			{
				var m = Matrix.RotationQuaternion(t.Rotation);
				float yaw, pitch, roll;
				m.ToAngles( out yaw, out pitch, out roll );

				uc.Yaw		=	yaw;
				uc.Pitch	=	pitch;

				if (float.IsNaN(uc.Yaw)		|| float.IsInfinity(uc.Yaw)		) uc.Yaw	= 0;
				if (float.IsNaN(uc.Pitch)	|| float.IsInfinity(uc.Pitch)	) uc.Pitch	= 0;
			}

			return uc;
		}


		public static UserCommand MergeCommand( UserCommand latest, UserCommand previous )
		{
			var uc = new UserCommand();
			uc.Action	=	latest.Action | previous.Action;
			uc.Yaw		=	latest.Yaw;
			uc.Pitch	=	latest.Pitch;
			return uc;
		}
	}
}
