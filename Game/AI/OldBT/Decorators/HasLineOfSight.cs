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
	public class HasLineOfSight : Condition
	{
		readonly string keyEntity;

		public HasLineOfSight( string keyEntity, ConditionMode mode, BTNode node ) : base( mode, node )
		{
			this.keyEntity	=	keyEntity;
		}

		public override bool Check( Entity entity )
		{
			var attackerEntity	=	entity;
			var targetEntity	=	entity.GetBlackboard()?.GetEntry<Entity>(keyEntity);

			return attackerEntity.HasLineOfSight( targetEntity );
		}
	}
}
