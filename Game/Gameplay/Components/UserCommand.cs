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

		Jump			=	0x00001000,
		Crouch			=	0x00002000,
					
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
		public float		Move;
		public float		Strafe;
		public float		Yaw;
		public float		Pitch;
		public float		Roll;
		public float		DeltaYaw;
		public float		DeltaPitch;

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
				uc.Roll		=	roll;

				if (float.IsNaN(uc.Yaw)		|| float.IsInfinity(uc.Yaw)		) uc.Yaw	= 0;
				if (float.IsNaN(uc.Pitch)	|| float.IsInfinity(uc.Pitch)	) uc.Pitch	= 0;
				if (float.IsNaN(uc.Roll)	|| float.IsInfinity(uc.Roll)	) uc.Roll	= 0;
			}

			return uc;
		}


		public override string ToString()
		{
			return string.Format("{0} : {1}, {2} : {3}, {4}, {5}]", Action, Move, Strafe, Yaw, Pitch, Roll);
		}
	}
}
