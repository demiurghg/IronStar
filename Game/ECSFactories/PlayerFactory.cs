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
using IronStar.Gameplay.Weaponry;

namespace IronStar.ECSFactories
{
	[EntityFactory("PLAYER")]
	public class PlayerFactory : EntityFactory
	{
		public PlayerFactory()
		{
		}

		public PlayerFactory( Vector3 p, Quaternion r )
		{
			Position	=	p;
			Rotation	=	r;
		}

		public static readonly Aspect PlayerAspect = new Aspect().Include<PlayerComponent,Transform>();

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			//	rotate character's model to face along forward vector :
			var transform	=	Matrix.RotationY( MathUtil.Pi ) * Matrix.Scaling(0.1f);

			e.AddComponent( new PlayerComponent() );

			/*e.AddComponent( new RenderModel("scenes\\monsters\\marine\\marine_anim", transform, Color.Red, 7, RMFlags.None ) );
			e.AddComponent( new BoneComponent() ); //*/

			e.AddComponent( new HealthComponent(100,0) );
			e.AddComponent( new CharacterController(6,4,1.5f, 24,8, 20, 10, 2.2f) );
			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new StepComponent() );
			e.AddComponent( new CameraComponent() );
			e.AddComponent( new MaterialComponent(MaterialType.Flesh) );
			e.AddComponent( new BobbingComponent() );

			var inventory	=	new InventoryComponent();
			var weaponState	=	new WeaponStateComponent();
			inventory.TryGiveWeapon( WeaponType.Machinegun );
			weaponState.TrySwitchWeapon( WeaponType.Machinegun );
			
			e.AddComponent( inventory );
			e.AddComponent( weaponState );
		}
	}
}
