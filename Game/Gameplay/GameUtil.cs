using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay
{
	public static class GameUtil
	{
		public static Matrix ComputePovTransform( UserCommandComponent uc, Transform t, CharacterController cc, BobbingComponent bob = null )
		{
			if (uc==null) throw new ArgumentNullException(nameof(uc));
			if (t ==null) throw new ArgumentNullException(nameof(t ));
			if (cc==null) throw new ArgumentNullException(nameof(cc));

			var rotation	= Matrix.Identity;
			var povPostion	= t.Position + cc.PovOffset;

			if (bob==null)
			{
				rotation	=	Matrix.RotationYawPitchRoll( uc.Yaw, uc.Pitch, uc.Roll );
			}
			else
			{
				rotation	=	Matrix.RotationYawPitchRoll( uc.Yaw + bob.BobYaw, uc.Pitch + bob.BobPitch, uc.Roll + bob.BobRoll );
				povPostion	+=	bob.BobUp * Vector3.Up;
			}

			return rotation * Matrix.Translation( povPostion );
		}
	}
}
