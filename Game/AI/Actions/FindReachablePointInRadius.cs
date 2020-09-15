using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;

namespace IronStar.AI.Actions
{
	public class FindReachablePointInRadius : BTAction
	{
		readonly string outputKey;
		readonly float radius;


		public FindReachablePointInRadius( string outputKey, float radius )
		{
			if (string.IsNullOrWhiteSpace(outputKey)) throw new ArgumentNullException(nameof(outputKey));
			if (radius<0) throw new ArgumentOutOfRangeException(nameof(radius));

			this.outputKey	=	outputKey;
			this.radius		=	radius;
		}


		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			var	navSystem		=	entity.gs.GetService<NavigationSystem>();
			var blackboard		=	entity.GetBlackboard();
			var location		=	entity.GetLocation();

			var randomPoint		=	navSystem.GetReachablePointInRadius( location, radius );

			blackboard.SetEntry( outputKey, randomPoint );

			return BTStatus.Success;
		}
	}
}
