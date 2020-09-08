using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using IronStar.SFX2;

namespace IronStar.ECSFactories
{
	[EntityFactory("MONSTER_MARINE")]
	public class MonsterMarineFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			//e.AddComponent( new PlayerComponent() );
			e.AddComponent( new RenderModel("scenes\\monsters\\marine\\marine", 0.1f, Color.Red, 7, RMFlags.None ) );
			e.AddComponent( new CharacterController(6,4,2, 24,9, 20, 10, 2.2f) );
			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );
			e.AddComponent( new StepComponent() );

			e.AddComponent( new BehaviorComponent() );

			e.AddComponent( new InventoryComponent() );

			return e;
		}
	}
}
