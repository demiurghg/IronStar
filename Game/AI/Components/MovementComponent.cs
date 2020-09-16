using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using Native.NRecast;

namespace IronStar.AI
{
	public class MovementComponent : IComponent
	{
		public BTStatus RoutingStatus = BTStatus.InProgress;

		NavigationRoute route;
		public NavigationRoute Route
		{
			get { return route; }
			set 
			{ 
				if (route!=value)
				{
					route = value;
					RoutingStatus = BTStatus.InProgress;
				}
			}
		}


		public float Velocity = 0;

		public MovementComponent()
		{
		}

		public void Load( GameState gs, Stream stream )
		{
		}

		public void Save( GameState gs, Stream stream )
		{
		}
	}
}
