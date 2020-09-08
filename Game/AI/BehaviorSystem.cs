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
using IronStar.AI.BehaviorTree;
using IronStar.AI.BehaviorNodes;

namespace IronStar.AI
{
	class BehaviorSystem : ProcessingSystem<BTNode,BehaviorComponent>
	{
		public bool Enabled = true;

		public BehaviorSystem()
		{
		}

		protected override BTNode Create( Entity entity, BehaviorComponent component1 )
		{
			var btBuilder = new BTBuilder();

			btBuilder
				.Sequence()
					.Action( new Print("Step1") )
					.Action( new Wait(300) )
					.Action( new Print("Step2") )
					.Action( new Wait(100) )
					.Action( new Print("Step3") )
					.Action( new Wait(500) )
				.End();


			return btBuilder.Build();
		}

		protected override void Destroy( Entity entity, BehaviorTree.BTNode resource )
		{
			//	do nothing.
		}

		protected override void Process( Entity entity, GameTime gameTime, BehaviorTree.BTNode behaviorTree, BehaviorComponent component1 )
		{
			if (Enabled)
			{
				behaviorTree.Tick( gameTime, entity );
			}
		}
	}
}
