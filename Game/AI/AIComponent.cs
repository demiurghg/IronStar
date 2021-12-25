using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public class AIComponent : IComponent
	{
		public Timer		ThinkTimer;
		public DMNode		DMNode = DMNode.Idle;
		public Timer		IdleTimer;

		public AIGoal		Goal;
		public Vector3[]	ActiveRoute	=	null;



		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
		}

		public void Load( GameState gs, BinaryReader reader )
		{
		}
	}
}
