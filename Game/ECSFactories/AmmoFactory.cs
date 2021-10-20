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
		readonly string ammoName		=	"";

		protected AmmoFactory( string name, int capacity, int count, float scale, string model )
		{
			modelName		=	model;
			modelScale		=	scale;
			ammoCapacity	=	capacity;
			ammoCount		=	count;
			ammoName		=	name;
		}

		public override void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new PickupComponent("pickupAmmo") );
			e.AddComponent( new TouchDetector() );
			e.AddComponent( new RenderModel( modelName, Matrix.Scaling( modelScale ), Color.White, 5, RMFlags.None ) );

			e.AddComponent( new DynamicBox( 0.66f, 0.72f, 0.66f, 3.0f ) { Group = CollisionGroup.PickupGroup } );
			e.AddComponent( new AmmoComponent(ammoCount, ammoCapacity) );
			e.AddComponent( new NameComponent(ammoName) );

			e.AddComponent( new Transform() );
		}
	}


	[EntityFactory("AMMO_BULLETS")]
	public class AmmoMachinegunFactory : AmmoFactory
	{
		public AmmoMachinegunFactory():
		base( "AMMO_BULLETS", 200, 50, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_CELLS")]
	public class AmmoPlasmagunFactory : AmmoFactory
	{
		public AmmoPlasmagunFactory():
		base( "AMMO_CELLS", 200, 50, 0.03f, "scenes\\weapon2\\plasma_rifle\\plasma_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_SLUGS")]
	public class AmmoRailgunFactory : AmmoFactory
	{
		public AmmoRailgunFactory():
		base( "AMMO_SLUGS", 50, 10, 0.06f, "scenes\\items\\medkit\\medkit10" ) {} // <----- WTF?
	}

	[EntityFactory("AMMO_ROCKETS")]
	public class AmmoRocketLauncherFactory : AmmoFactory
	{
		public AmmoRocketLauncherFactory():
		base( "AMMO_ROCKETS", 50, 10, 0.042f, "scenes\\weapon2\\rocket_launcher\\rocket_ammo" ) {}
	}

	[EntityFactory("AMMO_SHELLS")]
	public class AmmoShotgunFactory : AmmoFactory
	{
		public AmmoShotgunFactory():
		base( "AMMO_SHELLS", 50, 10, 0.03f, "scenes\\weapon2\\canister_rifle\\canister_rifle_ammo" ) {}
	}
}
