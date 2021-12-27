using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.AI;

namespace IronStar.AI
{
	public enum GoalType
	{
		Roaming,
		Chasing,
	}

	public struct AIGoal
	{
		public AIGoal( GoalType goalType, Vector3 location, int timeout )
		{
			GoalType	=	goalType;
			Location	=	location;
			Timer		=	new Timer(timeout);
			Failed		=	false;
			Succeed		=	false;
			
		}

		public GoalType	GoalType;
		public Vector3	Location;
		public Timer	Timer;
		public bool		Failed;
		public bool		Succeed;
	}
}
