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
using Fusion;

namespace IronStar.Monsters.Systems
{
	class MonsterWeaponBinding
	{
		public MonsterWeaponBinding( Entity weapon, WeaponType type, Matrix muzzle, string fx )
		{
			WeaponEntity	=	weapon;
			WeaponType		=	type;
			MuzzleTransform	=	muzzle;
			MuzzleFX		=	fx;
		}
		public	Entity		WeaponEntity;
		public	WeaponType	WeaponType;
		public	WeaponState	WeaponState = WeaponState.Inactive;
		public	Matrix		MuzzleTransform;
		public	string		MuzzleFX;
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

			if (binding.WeaponState!=weaponState.State)
			{
				var newState = weaponState.State;
				binding.WeaponState = newState;
				
				if (newState==WeaponState.Cooldown || newState==WeaponState.Cooldown2)
				{
					PlayMuzzleFX( binding );
				}
			}
		}


		void PlayMuzzleFX( MonsterWeaponBinding binding )
		{
			var weaponEntity = binding.WeaponEntity;

			if (weaponEntity!=null)
			{
				var gs = weaponEntity.gs;
				var transform =	weaponEntity.GetComponent<Transform>();

				if (transform!=null)
				{
					if (binding.MuzzleFX!=null)
					{
						var worldMuzzleTransform = binding.MuzzleTransform * transform.TransformMatrix;

						FXPlayback.AttachFX(gs, weaponEntity, binding.MuzzleFX, worldMuzzleTransform.TranslationVector, worldMuzzleTransform.Forward );
					}
				}
			}
		}


		EntityFactory GetWeaponFactory( WeaponType weapon )
		{
			switch (weapon)
			{
				case WeaponType.None:				return null;
				case WeaponType.Machinegun:			return new WeaponMachinegunFactory();
				case WeaponType.Machinegun2:		return new WeaponMachinegun2Factory();
				case WeaponType.Shotgun:			return new WeaponShotgunFactory();
				case WeaponType.Plasmagun:			return new WeaponPlasmagunFactory();
				case WeaponType.RocketLauncher:		return new WeaponRocketLauncherFactory();
				case WeaponType.Railgun:			return new WeaponRailgunFactory();
				default: return null;
			}
		}

		MonsterWeaponBinding CreateWeaponBinding( Entity monster, WeaponType weapon )
		{
			var gs = monster.gs;
			var weaponFactory	= GetWeaponFactory(weapon); 

			if (weaponFactory!=null)
			{
				//	#TODO #ECS #HACK -- ugly hack, create null entity, construct manually to get component data immediately
				var weaponEntity	=	gs.Spawn(null);
				var weaponMuzzle	=	Matrix.Identity;
				var weaponHandle	=	Matrix.Identity;
			
				weaponFactory.Construct( weaponEntity, gs );

				var rm = weaponEntity.GetComponent<RenderModel>();

				var scene = rm.LoadScene(gs);

				if (!scene.TryGetNodeTransform("muzzle", out weaponMuzzle)) Log.Warning("Missing {0} muzzle: {1}", weapon, rm.scenePath);
				if (!scene.TryGetNodeTransform("handle", out weaponHandle)) Log.Warning("Missing {0} handle: {1}", weapon, rm.scenePath);

				weaponEntity?.AddComponent( new AttachmentComponent() 
				{ 
					AutoAttach		=	false, 
					Bone			=	"weapon", 
					Target			=	monster,
					LocalTransform	=	Matrix.Invert(weaponHandle * rm.transform)
				});

				var muzzleFx = Arsenal.Get(weapon).MuzzleFX;

				return new MonsterWeaponBinding( weaponEntity, weapon, weaponMuzzle * rm.transform, muzzleFx );
			}
			else
			{
				return new MonsterWeaponBinding( null, weapon, Matrix.Identity, null );
			}
		}
	}
}
