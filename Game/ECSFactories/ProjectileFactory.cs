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
using IronStar.Gameplay.Weaponry;

namespace IronStar.ECSFactories
{
	public class PlasmaFactory : IFactory
	{
		AttackData ad;

		public PlasmaFactory( AttackData attackData )
		{
			ad = attackData;
		}

		public void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new ProjectileComponent( ad.Attacker, ad.Origin, ad.Rotation, ad.Direction, ad.DeltaTime, 500, 3, 10, "plasmaExplosion", ad.Damage, ad.Impulse) );
			e.AddComponent( new FXComponent("plasmaTrail", true) );
		}
	}


	public class RocketFactory : IFactory
	{
		AttackData ad;

		public RocketFactory( AttackData attackData )
		{
			ad = attackData;
		}

		public void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new ProjectileComponent( ad.Attacker, ad.Origin, ad.Rotation, ad.Direction, ad.DeltaTime, 300, 12, 10, "rocketExplosion", ad.Damage, ad.Impulse) );
			e.AddComponent( new FXComponent("rocketTrail", true) );
		}
	}
}
