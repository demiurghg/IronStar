using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;

namespace IronStar.Physics2
{
	public class Gravity : Component
	{
		public float Magnitude = 48;

		public Gravity( float g )
		{
			Magnitude	=	g;
		}
	}
}
