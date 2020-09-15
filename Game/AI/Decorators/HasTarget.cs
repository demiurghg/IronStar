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
	public class HasTarget : Condition
	{
		public HasTarget( BTNode node ) : base( node )
		{
		}

		public HasTarget( bool inverse, bool continuous, BTNode node ) : base( node )
		{
			InverseCondition	=	inverse;
			Continuous			=	continuous;
		}

		public override bool Check( Entity entity )
		{
			var targetEntity = entity.GetBlackboard()?.GetEntry<Entity>("TargetEntity");

			return targetEntity!=null;
		}
	}
}
