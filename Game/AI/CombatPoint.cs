using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.AI
{
	public class CombatPoint
	{
		public bool Dirty = true;
		public readonly Timer Timer;
		public readonly Vector3 Location;
		public bool IsExposed;

		public CombatPoint( Vector3 location, int timeout = 3000 )
		{
			Location = location;
			Timer = new Timer(timeout);
		}
	}
}
