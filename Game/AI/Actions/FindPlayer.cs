using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;

namespace IronStar.AI.Actions
{
	public class FindPlayer : BTAction
	{
		readonly string outputKey;


		public FindPlayer( string outputKey )
		{
			if (string.IsNullOrWhiteSpace(outputKey)) throw new ArgumentNullException(nameof(outputKey));
			this.outputKey	=	outputKey;
		}


		public override BTStatus Update( GameTime gameTime, Entity entity )
		{
			var playerEntity	=	entity.gs.GetPlayer();

			if (playerEntity!=null)
			{
				var blackboard		=	entity.GetComponent<BehaviorComponent>()?.Blackboard;
				var playerPos		=	playerEntity.Location;

				blackboard.SetEntry( outputKey, playerPos );

				return BTStatus.Success;
			}
			else
			{
				return BTStatus.Failure;
			}
		}
	}
}
