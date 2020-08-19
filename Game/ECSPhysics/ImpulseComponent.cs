using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.ECSPhysics
{
	public class ImpulseComponent : Component
	{
		public Vector3 Impulse;
		public Vector3 Location;

		public ImpulseComponent( Vector3 location, Vector3 impulse )
		{
			Impulse		=	impulse;
			Location	=	location;
		}
	}
}
