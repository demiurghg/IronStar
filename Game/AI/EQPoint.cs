using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public enum EQState
	{
		NotReady,
		Exposed,
		Protected,
	}

	public class EQPoint
	{
		public Entity Owner;
		public AIComponent AI;
		public EQState State;
		public readonly Timer Timer;
		public readonly Vector3 Location;
		public readonly Vector3 POV;
		public readonly AITarget Target;

		public EQPoint( Entity owner, AIComponent ai, Vector3 location, Vector3 pov, AITarget target, int timeout )
		{
			Owner		=	owner;
			AI			=	ai;
			State		=	EQState.NotReady;
			Target		=	target;
			Location	=	location;
			POV			=	pov;
			Timer		=	new Timer(timeout);
		}
	}
}
