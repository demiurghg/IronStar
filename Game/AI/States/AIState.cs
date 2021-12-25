using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.AI
{
	public abstract class AIState
	{
		public abstract AIState Update( GameTime gameTime, Entity entity, AIComponent ai );
	}



	public class Stand : AIState
	{
		TimeSpan timeout;

		public Stand( TimeSpan timeout )
		{
			this.timeout = timeout;
		}

		public override AIState Update( GameTime gameTime, Entity entity, AIComponent ai )
		{
			timeout -= gameTime.Elapsed;

			if (timeout<TimeSpan.Zero)
			{
				return Move
			}
			return this;
		}
	}


	public class Move : AIState
	{
		public Move( Vector3 destination )
		{
		}

		public override AIState Update( GameTime gameTime, Entity entity, AIComponent ai )
		{
			throw new NotImplementedException();
		}
	}
}
