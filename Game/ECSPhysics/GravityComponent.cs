using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class GravityComponent : Component
	{
		public float Magnitude = 48;

		public GravityComponent( float g )
		{
			Magnitude	=	g;
		}
	}
}
