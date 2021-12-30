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
using IronStar.Gameplay.Weaponry;
using IronStar.SFX2;

namespace IronStar.ECSFactories
{
	public class AmmoFactory : EntityFactory
	{
		readonly AmmoType	ammoType	;
		readonly int		ammoCount	;
		readonly string		modelName	;
		readonly float		modelScale	;

		protected AmmoFactory( AmmoType type, int count, float scale, string model )
		{
			ammoType	=	type;
			ammoCount	=	count;
			modelName	=	model;
			modelScale	=	scale;
		}

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );

			e.AddComponent( new PickupComponent("pickupAmmo") );
			e.AddComponent( new TouchDetector() );
			e.AddComponent( new RenderModel( modelName, Matrix.Scaling( modelScale ), Color.White, 5, RMFlags.None ) );

			e.AddComponent( new DynamicBox( 0.66f, 0.72f, 0.66f, 3.0f ) { Group = CollisionGroup.PickupGroup } );
			e.AddComponent( new PowerupComponent(ammoType, ammoCount) );
		}
	}


	[EntityFactory("AMMO_BULLETS")]
	public class AmmoMachinegunFactory : AmmoFactory
	{
		public AmmoMachinegunFactory():
		base( AmmoType.Bullets, 50, 1, "scenes\\weapon2\\assault_rifle\\assault_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_CELLS")]
	public class AmmoPlasmagunFactory : AmmoFactory
	{
		public AmmoPlasmagunFactory():
		base(  AmmoType.Cells, 50, 1, "scenes\\weapon2\\plasma_rifle\\plasma_rifle_ammo" ) {}
	}

	[EntityFactory("AMMO_SLUGS")]
	public class AmmoRailgunFactory : AmmoFactory
	{
		public AmmoRailgunFactory():
		base(  AmmoType.Slugs, 10, 1, "scenes\\items\\medkit\\medkit10" ) {} // <----- WTF?
	}

	[EntityFactory("AMMO_ROCKETS")]
	public class AmmoRocketLauncherFactory : AmmoFactory
	{
		public AmmoRocketLauncherFactory():
		base(  AmmoType.Rockets, 10, 1, "scenes\\weapon2\\rocket_launcher\\rocket_ammo" ) {}
	}

	[EntityFactory("AMMO_SHELLS")]
	public class AmmoShotgunFactory : AmmoFactory
	{
		public AmmoShotgunFactory():
		base(  AmmoType.Shells, 10, 1, "scenes\\weapon2\\canister_rifle\\canister_rifle_ammo" ) {}
	}
}
