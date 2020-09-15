using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.ECS;
using Fusion.Core.Mathematics;

namespace IronStar.BTCore.Actions
{
	public class Wait : BTAction
	{
		TimeSpan timeToWait;
		TimeSpan timer;
		readonly int minWaitTime;
		readonly int maxWaitTime;

		public Wait( int minTime, int maxTime )
		{
			if (minTime<0) throw new ArgumentOutOfRangeException(nameof(minTime));
			if (maxTime<0) throw new ArgumentOutOfRangeException(nameof(maxTime));
			if (maxTime<minTime) throw new ArgumentOutOfRangeException(nameof(maxTime));

			minWaitTime	=	minTime;
			maxWaitTime	=	maxTime;
		}

		
		public override bool Initialize(Entity entity)
		{
			timeToWait	=	TimeSpan.FromMilliseconds( MathUtil.Random.NextDouble( minWaitTime, maxWaitTime ) );
			timer		=	TimeSpan.Zero;
			return true;
		}

		
		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			timer += gameTime.Elapsed;

			if (cancel)
			{
				return BTStatus.Failure;
			}

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
