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

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PickupComponent("pickupWeapon") );
			e.AddComponent( new TouchDetector() );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}


	[EntityFactory("WEAPON_MACHINEGUN")]
	public class WeaponMachinegunFactory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("MACHINEGUN") );
			e.AddComponent( WeaponComponent.BeamWeapon( 7, 5.0f, 1, 2.0f,	50,	"AMMO_BULLETS", "*trail_bullet", "machinegunHit", "machinegunMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\assault_rifle\\assault_rifle_view", 0.03f, MachinegunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) );

			return e;
		}
	}


	[EntityFactory("WEAPON_MACHINEGUN2")]
	public class WeaponMachinegun2Factory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("MACHINEGUN2") );
			e.AddComponent( WeaponComponent.BeamWeapon( 5, 30.0f, 1, 1.0f,	50,	"AMMO_BULLETS", "*trail_bullet", "machinegunHit", "machinegunMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\battle_rifle\\battle_rifle_view", 0.03f, MachinegunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) );

			return e;
		}
	}


	[EntityFactory("WEAPON_SHOTGUN")]
	public class WeaponShotgunFactory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("SHOTGUN") );
			e.AddComponent( WeaponComponent.BeamWeapon( 10, 1.0f, 10, 3.0f,	750,	"AMMO_SHELLS", null, "shotgunHit", "shotgunMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\canister_rifle\\canister_rifle_view", 0.03f, ShotgunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.54f, 1.2f, 4.5f, 5.0f ) );

			return e;
		}
	}


	[EntityFactory("WEAPON_PLASMAGUN")]
	public class WeaponPlasmagunFactory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("PLASMAGUN") );
			e.AddComponent( WeaponComponent.ProjectileWeapon( 10, 5, 50, "PLASMA", "AMMO_CELLS", "plasmaMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\plasma_rifle\\plasma_rifle_view", 0.03f, PlasmagunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.9f, 1.2f, 4.5f, 6.0f ) );

			return e;
		}
	}


	[EntityFactory("WEAPON_ROCKETLAUNCHER")]
	public class WeaponRocketLauncherFactory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("ROCKETLAUNCHER") );
			e.AddComponent( WeaponComponent.ProjectileWeapon( 100, 15, 1500, "ROCKET", "AMMO_ROCKETS", "rocketMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\rocket_launcher\\rocket_launcher_view", 0.036f, RocketLauncherColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 0.9f, 1.0f, 6.0f, 7.0f ) );

			return e;
		}
	}


	[EntityFactory("WEAPON_RAILGUN")]
	public class WeaponRailgunFactory : WeaponFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = base.Spawn( gs );

			e.AddComponent( new NameComponent("RAILGUN") );
			e.AddComponent( WeaponComponent.BeamWeapon( 100, 250, 1, 0,	1500,	"AMMO_SLUGS", "*trail_gauss", "railHit", "railMuzzle" ) );
			e.AddComponent( new RenderModel("scenes\\weapon2\\gauss_rifle\\gauss_rifle_view", 0.03f, RailgunColor, GlowIntensity, RMFlags.None ) );
			e.AddComponent( new DynamicBox( 1.8f, 0.9f, 5.1f, 7.0f ) );

			return e;
		}
	}
}
