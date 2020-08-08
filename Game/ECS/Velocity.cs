using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public class Velocity : Component
	{
		/// <summary>
		/// Linear velocity 
		/// </summary>
		public Vector3	Linear	=	Vector3.Zero;

		/// <summary>
		/// Angular velocity
		/// </summary>
		public Vector3	Angular	=	Vector3.Zero;


		public Velocity()
		{
		}


		public Velocity( Vector3 v )
		{
			Linear	=	v;
			Angular	=	Vector3.Zero;
		}
	}
}
