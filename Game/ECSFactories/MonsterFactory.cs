﻿using System;
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
using IronStar.Gameplay.Weaponry;
using IronStar.SFX2;

namespace IronStar.ECSFactories
{
	[EntityFactory("MONSTER_MARINE")]
	public class MonsterMarineFactory : EntityFactory
	{
		public Team Team { get; set; } = Team.Monsters;

		void GiveWeapon(IGameState gs, InventoryComponent inventory, WeaponStateComponent state, WeaponType weapon)
		{
			inventory.TryGiveWeapon(weapon);
			state.TrySwitchWeapon(weapon);
		}

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			//	rotate character's model to face along forward vector :
			var transform	=	Matrix.Identity
							;

			//e.AddComponent( new PlayerComponent() );
			e.AddComponent( new RenderModel("scenes\\monsters\\marine\\marine_anim", transform, Color.Red, 7, RMFlags.None ) );
			e.AddComponent( new BoneComponent() );

			e.AddComponent( new CharacterController(6,4,2, 24,9, 20, 10, 2.2f) );
			//e.AddComponent( new RagdollComponent() );

			e.AddComponent( new UserCommandComponent() );
			e.AddComponent( new StepComponent() );
			e.AddComponent( new HealthComponent(50,25) );
			e.AddComponent( new MaterialComponent(MaterialType.Flesh) );

			var inventory	=	new InventoryComponent(InventoryFlags.InfiniteAmmo);
			var weaponState	=	new WeaponStateComponent();
			e.AddComponent( inventory );
			e.AddComponent( weaponState );
			e.AddComponent( new AIComponent() );
			e.AddComponent( new TeamComponent(Team) );

			var weapons = new[]
			{
				WeaponType.Machinegun,
				WeaponType.Plasmagun,
				WeaponType.RocketLauncher,
			};

			GiveWeapon( gs, inventory, weaponState, weapons[ MathUtil.Random.Next(weapons.Length) ] );
		}
	}
}
