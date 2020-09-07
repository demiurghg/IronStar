using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.AI.BehaviorTree;
using IronStar.ECS;

namespace IronStar.AI.BehaviorNodes
{
	public class Wait : BTAction
	{
		readonly TimeSpan timeToWait;
		TimeSpan timer;

		
		public Wait( int milliseconds )
		{
			timeToWait	=	TimeSpan.FromMilliseconds( milliseconds );
		}

		
		public override void Initialize()
		{
			timer = TimeSpan.Zero;
		}

		
		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			timer += gameTime.Elapsed;

			if (timer>=timeToWait)
			{
				return BTStatus.Success;
			}
			else
			{
				return BTStatus.InProgress;
			}
		}
	}
}
