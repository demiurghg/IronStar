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
	public class WeaponFactory : EntityFactory
	{
		readonly string modelName		=	"";
		readonly float	modelScale		=	1.0f;
		readonly int	ammoCapacity	=	10;
		readonly int	ammoCount		=	10;

		protected WeaponFactory( int capacity, int count, float scale, string model )
		{
			modelName		=	model;
			modelScale		=	scale;
			ammoCapacity	=	capacity;
			ammoCount		=	count;
		}

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PickupComponent("pickupWeapon") );
			e.AddComponent( new TouchDetector() );
			e.AddComponent( new RenderModel( modelName, Matrix.Scaling( modelScale ), Color.White, 5, RMFlags.None ) );

			e.AddComponent( new DynamicBox( 0.66f, 0.72f, 0.66f, 1.0f ) );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}


	[EntityFactory("WEAPON_MACHINEGUN")]
	public class WeaponMachinegunFactory : AmmoFactory
	{
		public WeaponMachinegunFactory():
		base( 50, 50, 0.03f, "scenes\\weapon2\\assault_rifle\\assault_rifle_ammo" ) {}
	}
}
