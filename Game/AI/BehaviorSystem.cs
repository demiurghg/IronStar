﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.BTCore.Actions;
using IronStar.BTCore.Decorators;
using IronStar.AI.Actions;
using IronStar.Gameplay;

namespace IronStar.AI
{
	class BehaviorSystem : ProcessingSystem<BTNode,BehaviorComponent>
	{
		public bool Enabled = true;
		readonly PhysicsCore physics;

		public BehaviorSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}

		protected override BTNode Create( Entity entity, BehaviorComponent component1 )
		{
			var approach = 
				new HasTarget( 
					new Sequence(
						new Print("Got him!"),
						new FindPlayer("playerLocation"),
						new MoveTo("playerLocation")
					)
				);

			var roaming = 
				new Inverter(
					new HasTarget( 
						new Sequence(
							new Print("Searching"),
							new Wait(1500, 2500),
							new FindReachablePointInRadius("roamingPoint", 300),
							new MoveTo("roamingPoint")
						)
					)
				);

			return new Selector( approach, roaming );
			//return new BTBuilder()
			//.Selector()
			//	.Sequence()
			//		.Decorator(
			//	.End()
			//	.Sequence()
			//		.Action( new Print("Searching...") )
			//		.Action(  )
			//		//.Action( new FindPlayer("playerLocation") )
			//		.Action( new FindReachablePointInRadius("roamingPoint", 300) )
			//		.Action( new MoveTo("roamingPoint") )
			//		//.Repeat( 3, new BTBuilder().Sequence().Action( new Wait(100) ).Action( new Print("...") ).End() )
			//		//.Action( new Wait(1000) )
			//	.End()
			//.End();
		}

		protected override void Destroy( Entity entity, BTNode resource )
		{
			//	do nothing.
		}

		protected override void Process( Entity entity, GameTime gameTime, BTNode behaviorTree, BehaviorComponent component1 )
		{
			if (Enabled)
			{
				behaviorTree.Tick( gameTime, entity );
			}
		}
	}
}
