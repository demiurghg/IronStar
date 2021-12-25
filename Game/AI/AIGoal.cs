using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.AI
{
	public struct AIGoal
	{
		public AIGoal( Vector3 location, int timeout )
		{
			Location	=	location;
			Timer		=	new Timer(timeout);
			Failed		=	false;
			Succeed		=	false;
		}

		public Vector3	Location;
		public Timer	Timer;
		public bool		Failed;
		public bool		Succeed;
	}
}
