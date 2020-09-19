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
	public class HasBlackboardValue<T> : Condition
	{
		readonly string key;

		public HasBlackboardValue( string key, ConditionMode mode, BTNode node ) : base( mode, node )
		{
			this.key	=	key;
		}

		public override bool Check( Entity entity )
		{
			T temp;

			return entity.GetBlackboard().TryGet(key, out temp);
		}
	}
}
