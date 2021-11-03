using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.Gameplay.Components;
using IronStar.SFX2;
using IronStar.Gameplay.Weaponry;

namespace IronStar.ECSFactories
{
	public abstract class WeaponFactory : EntityFactory
	{
		public static readonly Color MachinegunColor		=	new Color( 250, 80, 20 ); 
		public static readonly Color ShotgunColor			=	new Color( 250, 80, 20 ); 
		public static readonly Color RocketLauncherColor	=	new Color( 250, 80, 20 ); 
		public static readonly Color RailgunColor			=	new Color( 107, 136, 255 );
		public static readonly Color PlasmagunColor			=	new Color( 107, 136, 255 );

		public static readonly float GlowIntensity			=	7;

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PickupComponent("pickupWeapon") );
			e.AddComponent( new TouchDetector() );
		}
	}


	[EntityFactory("WEAPON_MACHINEGUN")]
	public class WeaponMachinegunFactory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.Machinegun, AmmoType.Bullets, 50 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\assault_rifle\\assault_rifle_view", 0.03f, MachinegunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}


	[EntityFactory("WEAPON_MACHINEGUN2")]
	public class WeaponMachinegun2Factory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.Machinegun2, AmmoType.Bullets, 50 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\battle_rifle\\battle_rifle_view", 0.03f, MachinegunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}


	[EntityFactory("WEAPON_SHOTGUN")]
	public class WeaponShotgunFactory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.Shotgun, AmmoType.Shells, 10 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\canister_rifle\\canister_rifle_view", 0.03f, ShotgunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}


	[EntityFactory("WEAPON_PLASMAGUN")]
	public class WeaponPlasmagunFactory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.Plasmagun, AmmoType.Cells, 50 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\plasma_rifle\\plasma_rifle_view", 0.03f, PlasmagunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.9f, 1.2f, 4.5f, 6.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}


	[EntityFactory("WEAPON_ROCKETLAUNCHER")]
	public class WeaponRocketLauncherFactory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.RocketLauncher, AmmoType.Rockets, 10 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\rocket_launcher\\rocket_launcher_view", 0.036f, RocketLauncherColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.9f, 1.0f, 6.0f, 7.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}


	[EntityFactory("WEAPON_RAILGUN")]
	public class WeaponRailgunFactory : WeaponFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PowerupComponent( WeaponType.Railgun, AmmoType.Slugs, 10 ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\gauss_rifle\\gauss_rifle_view", 0.03f, RailgunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 1.8f, 0.9f, 5.1f, 7.0f ) { Group = CollisionGroup.PickupGroup } );
		}
	}
}
