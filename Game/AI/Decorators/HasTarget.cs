﻿using System;
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
		public HasTarget( ConditionMode mode, BTNode node ) : base( mode, node )
		{
		}

		public override bool Check( Entity entity )
		{
			var targetEntity = entity.GetBlackboard()?.GetEntry<Entity>("TargetEntity");

			return targetEntity!=null;
		}
	}
}
