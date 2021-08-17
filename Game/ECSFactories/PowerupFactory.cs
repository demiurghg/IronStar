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
	public class PowerupFactory : EntityFactory
	{
		readonly string modelName	=	"";
		readonly string pickupFx	=	"";
		readonly float	modelScale	=	1.0f;
		readonly int	health		=	10;
		readonly int	armor		=	10;

		protected PowerupFactory( int health, int armor, string fx, float scale, string model )
		{
			modelName	=	model;
			modelScale	=	scale;
			this.health	=	health;
			this.armor	=	armor;
			pickupFx	=	fx;
		}

		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new PickupComponent( pickupFx ) );
			e.AddComponent( new PowerupComponent( health, armor ) );
			e.AddComponent( new TouchDetector() );
			e.AddComponent( new RenderModel( modelName, Matrix.Scaling( modelScale ), Color.White, 5, RMFlags.None ) );

			e.AddComponent( new DynamicBox( 1.2f, 1.2f, 3.0f, 5.0f ) { Group = CollisionGroup.PickupGroup } );

			e.AddComponent( new KinematicState() );

			return e;
		}
	}


	[EntityFactory("POWERUP_MEDKIT")]
	public class PowerupMedkitFactory : PowerupFactory
	{
		public PowerupMedkitFactory():
		base( 50, 0, "pickupHealth", 0.3f, "scenes\\items\\medkit\\medkit10" ) {}
	}


	[EntityFactory("POWERUP_ARMOR")]
	public class PowerupArmorFactory : PowerupFactory
	{
		public PowerupArmorFactory():
		base( 0, 50, "pickupArmor", 0.3f, "scenes\\items\\armor\\armor_lo" ) {}
	}

}
