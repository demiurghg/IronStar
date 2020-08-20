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
using IronStar.SFX;

namespace IronStar.ECSFactories
{
	[EntityFactory("PLASMA")]
	public class PlasmaFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new ProjectileComponent(150, 0, 10, "plasmaExplosion") );
			e.AddComponent( new FXComponent("plasmaTrail", true) );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}


	[EntityFactory("ROCKET")]
	public class RocketFactory : EntityFactory
	{
		public override Entity Spawn( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new ProjectileComponent(150, 12, 10, "rocketExplosion") );
			e.AddComponent( new FXComponent("rocketTrail", true) );

			e.AddComponent( new Transform() );
			e.AddComponent( new Velocity() );

			return e;
		}
	}
}
