using System;
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

namespace IronStar.AI
{
	class BehaviorSystem : ProcessingSystem<BTNode,BTComponent>
	{
		public BehaviorSystem()
		{
		}

		protected override BTNode Create( Entity entity, BTComponent component1 )
		{
			throw new NotImplementedException();
		}

		protected override void Destroy( Entity entity, BehaviorTree.BTNode resource )
		{
			//	do nothing.
		}

		protected override void Process( Entity entity, GameTime gameTime, BehaviorTree.BTNode resource, BTComponent component1 )
		{
			resource.Tick( gameTime, entity );
		}
	}
}
