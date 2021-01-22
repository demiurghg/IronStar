using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.Animation;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using IronStar.SFX2;

namespace IronStar.ECSFactories
{
	[EntityFactory("PLAYER")]
	public class PlayerFactory : EntityFactory
	{
		public static readonly Aspect PlayerAspect = new Aspect().Include<PlayerComponent,Transform>();

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			//	rotate character's model to face along forward vector :
			var transform	=	Matrix.RotationY( MathUtil.Pi ) * Matrix.Scaling(0.1f);

			e.AddComponent( new PlayerComponent() );

			e.AddComponent( new RenderModel("scenes\\monsters\\marine\\marine_anim", transform, Color.Red, 7, RMFlags.None ) );
			e.AddComponent( new BoneComponent() ); //*/

			e.AddComponent( new HealthComponent(100,0) );
			e.AddComponent( new CharacterController(6,4,2, 24,8, 20, 10, 2.2f) );
			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );
			e.AddComponent( new StepComponent() );
			e.AddComponent( new MaterialComponent(MaterialType.Flesh) );

			e.AddComponent( new InventoryComponent() );

			return e;
		}
	}
}
