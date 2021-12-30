using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.SFX2;
using IronStar.SFX;
using IronStar.Animation;
using Fusion.Engine.Graphics.Scenes;
using BEPUutilities.Threading;
using IronStar.Gameplay.Weaponry;
using IronStar.ECSFactories;

namespace IronStar.Monsters.Systems
{
	class MonsterWeaponBinding
	{
		public MonsterWeaponBinding( Entity weapon, WeaponType type, Vector3 muzzle )
		{
			WeaponEntity	=	weapon;
			WeaponType		=	type;
			MuzzleLocation	=	muzzle;
		}
		public			Entity		WeaponEntity;
		public readonly	WeaponType	WeaponType;
		public readonly	Vector3		MuzzleLocation;
	}

	class MonsterWeaponSystem : ProcessingSystem<MonsterWeaponBinding,InventoryComponent,WeaponStateComponent,RenderModel>
	{
		readonly Game Game;
		readonly FXPlayback fxPlayback;


		public MonsterWeaponSystem( Game game, FXPlayback fxPlayback )
		{
			this.Game		=	game;
			this.fxPlayback	=	fxPlayback;
		}

		protected override MonsterWeaponBinding Create( Entity entity, InventoryComponent inventory, WeaponStateComponent weaponState, RenderModel model )
		{
			return CreateWeaponBinding( entity, weaponState.ActiveWeapon );
		}

		protected override void Destroy( Entity entity, MonsterWeaponBinding binding )
		{
			binding.WeaponEntity?.Kill();
		}

		protected override void Process( Entity entity, GameTime gameTime, MonsterWeaponBinding binding, InventoryComponent inventory, WeaponStateComponent weaponState, RenderModel model )
		{
			var health = entity.GetComponent<HealthComponent>();

			if (health!=null && health.Health<=0)
			{
				binding.WeaponEntity?.RemoveComponent<AttachmentComponent>();
				binding.WeaponEntity = null;
			}

			if ( binding.WeaponType!=weaponState.ActiveWeapon)
			{
				RefreshResource( entity );
			}
		}


		Entity SpawnWeaponEntity( Entity monster, WeaponType weapon )
		{
			var gs = monster.gs;
			switch (weapon)
			{
				case WeaponType.None:				return null;
				case WeaponType.Machinegun:			return gs.Spawn( new WeaponMachinegunFactory() );
				case WeaponType.Machinegun2:		return gs.Spawn( new WeaponMachinegun2Factory() );
				case WeaponType.Shotgun:			return gs.Spawn( new WeaponShotgunFactory() );
				case WeaponType.Plasmagun:			return gs.Spawn( new WeaponPlasmagunFactory() );
				case WeaponType.RocketLauncher:		return gs.Spawn( new WeaponRocketLauncherFactory() );
				case WeaponType.Railgun:			return gs.Spawn( new WeaponRailgunFactory() );
				default: return null;
			}
		}

		MonsterWeaponBinding CreateWeaponBinding( Entity monster, WeaponType weapon )
		{
			Entity weaponEntity = SpawnWeaponEntity(monster, weapon);

			weaponEntity?.AddComponent( new AttachmentComponent() 
			{ 
				AutoAttach		=	false, 
				Bone			=	"weapon", 
				Target			=	monster,
				DropOnKill		=	true,
				LocalTransform	=	Matrix.RotationY( MathUtil.Pi )
			});

			return new MonsterWeaponBinding( weaponEntity, weapon, Vector3.Zero );
		}
	}
}
