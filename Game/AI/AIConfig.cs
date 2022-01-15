using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AIConfig
	{
		public readonly int		ThinkTime		=	300;	//	normally distributed think interval

		//	idle and roaming settings :
		public readonly float	RoamRadius		=	RenderSystem.MetersToGameUnit(50);	//	roaming radius from current point
		public readonly int		IdleTimeout		=	3000;	//	idle timeout

		//	stun behavior settings :
		public readonly int		StunTimeout		=	1000;	//	stun timeout

		//	movement settings :
		public readonly	float	RotationRate	=	MathUtil.DegreesToRadians(120);

		//	perception settings :
		public readonly float	VisibilityFov	=	MathUtil.DegreesToRadians(60);
		public readonly float	VisibilityRange	=	450.0f;
		public readonly float	BroadcastRange	=	150.0f;
		public readonly float	HearingRange	=	15.0f;

		public readonly int		GapeTimeout		=	1000;
		public readonly int		TimeToForget	=	60*1000;

		//	combat settings :
		public readonly float	Pushiness			=	0.25f;	//	probability to approach to target instead of serching good firing point
		public readonly int		AttackTime			=	1000;
		public readonly float	CombatMoveRadius	=	30.0f;

		//	accuracy settings :
		public readonly float	Accuracy			=	0.10f;	//	#TODO #AI -- weapon dependency, target velocity
		public readonly float	AccuracyThreshold	=	0.05f;	//	#TODO #AI -- weapon dependency, target velocity
		public readonly int		AimTime				=	600;	//	#TODO #AI -- difficulty setting

		//	cover settings :
		public readonly int		CoverTimeout		=	1500;
		public readonly bool	GainHealthInCover	=	true;
	}
}
