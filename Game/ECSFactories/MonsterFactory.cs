using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.Animation;
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
		void GiveWeapon(GameState gs, InventoryComponent inventory, string weaponName)
		{
			var weapon		=	gs.Spawn(weaponName);

			weapon.RemoveComponent<Transform>();

			inventory.AddItem( weapon );
			inventory.SwitchWeapon( weapon );
		}

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			//return e;

			//	rotate character's model to face along forward vector :
			var transform	=	Matrix.RotationY( MathUtil.Pi ) * Matrix.Scaling(0.1f);

			//e.AddComponent( new PlayerComponent() );
			e.AddComponent( new RenderModel("scenes\\monsters\\marine\\marine_anim", transform, Color.Red, 7, RMFlags.None ) );
			e.AddComponent( new BoneComponent() );

			e.AddComponent( new CharacterController(6,4,2, 24,9, 20, 10, 2.2f) );
			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new Transform() );
			e.AddComponent( new StepComponent() );
			e.AddComponent( new HealthComponent(50,25) );
			e.AddComponent( new MaterialComponent(MaterialType.Flesh) );

			var inventory	=	new InventoryComponent(InventoryFlags.InfiniteAmmo);
			e.AddComponent( inventory );
			e.AddComponent( new BehaviorComponent() );

			var weapons = new[]
			{
				//"WEAPON_RAILGUN",
				"WEAPON_MACHINEGUN",
				"WEAPON_PLASMAGUN",
				//"WEAPON_PLASMAGUN",
				//"WEAPON_PLASMAGUN",
				//"WEAPON_ROCKETLAUNCHER",
				"WEAPON_ROCKETLAUNCHER",
			};

			//GiveWeapon( e, "WEAPON_PLASMAGUN");
			GiveWeapon( gs, inventory, weapons[ MathUtil.Random.Next(weapons.Length) ] );

			return e;
		}
	}
}
