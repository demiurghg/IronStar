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
		public readonly int		ThinkTime		=	300;

		public readonly float	RoamRadius		=	RenderSystem.MetersToGameUnit(50); 
		public readonly int		IdleTimeout		=	3000; 

		public readonly	float	RotationRate	=	MathUtil.DegreesToRadians(600);

		public readonly float	VisibilityFov	=	MathUtil.DegreesToRadians(45);
		public readonly float	VisibilityRange	=	450.0f;
		public readonly float	HearingRange	=	15.0f;

		public readonly int		TimeToForget	=	30*1000;

		public readonly float	Accuracy		=	0.2f;
	}
}
