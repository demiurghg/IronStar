using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AITarget
	{
		public readonly Entity Entity;
		public readonly Timer ForgettingTimer;
		public Vector3 LastKnownPosition;
		public bool Visible;
		public bool Confirmed; // means, that gapping period always complete with combat chase/attack/move

		public AITarget( Entity e, Vector3 pos, int timeToForget )
		{
			Entity	=	e;
			LastKnownPosition	=	pos;
			ForgettingTimer		=	new Timer();
			ForgettingTimer.Set( timeToForget );
			Visible = true;
			Confirmed = false;
		}
	}
}
