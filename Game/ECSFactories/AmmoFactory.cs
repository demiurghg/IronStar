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
	public class AmmoFactory : EntityFactory
	{
		readonly string modelName		=	"";
		readonly float	modelScale		=	1.0f;
		readonly int	ammoCapacity	=	10;
		readonly int	ammoCount		=	10;

		protected AmmoFactory( int capacity, int count, float scale, string model )
		{
			modelName		=	model;
			modelScale		=	scale;
			ammoCapacity	=	capacity;
			ammoCount		=	count;
		}

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PickupComponent() );
			e.AddComponent( new TouchDetector() );
			e.AddComponent( new RenderModel( modelName, Matrix.Scaling( modelScale ), Color.White, 5, RMFlags.None ) );

			e.AddComponent( new DynamicBox( 0.66f, 0.72f, 0.66f, 1.0f ) );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}


	[EntityFactory("AMMO_MACHINEGUN")]
	public class AmmoMachinegunFactory : AmmoFactory
	{
		public AmmoMachinegunFactory():
		base( 50, 50, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_MACHINEGUN2")]
	public class AmmoMachinegun2Factory : AmmoFactory
	{
		public AmmoMachinegun2Factory():
		base( 50, 50, 0.03f, "scenes\\weapon2\\battle_rifle\\battle_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_PLASMAGUN")]
	public class AmmoPlasmagunFactory : AmmoFactory
	{
		public AmmoPlasmagunFactory():
		base( 50, 50, 0.03f, "scenes\\weapon2\\plasma_rifle\\plasma_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_RAILGUN")]
	public class AmmoRailgunFactory : AmmoFactory
	{
		public AmmoRailgunFactory():
		base( 10, 10, 0.06f, "scenes\\items\\medkit\\medkit10" ) {} // <----- WTF?
	}

	[EntityFactory("AMMO_ROCKETLAUNCHER")]
	public class AmmoRocketLauncherFactory : AmmoFactory
	{
		public AmmoRocketLauncherFactory():
		base( 10, 10, 0.042f, "scenes\\weapon2\\rocket_launcher\\rocket_ammo" ) {}
	}

	[EntityFactory("AMMO_SHOTGUN")]
	public class AmmoShotgunFactory : AmmoFactory
	{
		public AmmoShotgunFactory():
		base( 10, 10, 0.03f, "scenes\\weapon2\\canister_rifle\\canister_rifle_ammo" ) {}
	}
}
