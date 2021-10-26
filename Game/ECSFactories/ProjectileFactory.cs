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
	public class PlasmaFactory : EntityFactory
	{
		int		damage;
		float	impulse;
		Entity	attacker;

		public PlasmaFactory( Vector3 position, Quaternion rotation, Vector3 dir, float lag, Entity attacker, int damage, float impulse )
		{
			this.Position	=	position + dir * 500 * lag;
			this.Rotation	=	rotation;
			this.damage		=	damage;
			this.impulse	=	impulse;
			this.attacker	=	attacker;
		}

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );
			e.AddComponent( new ProjectileComponent( attacker, 500, 3, 10, "plasmaExplosion", damage, impulse) );
			e.AddComponent( new FXComponent("plasmaTrail", true) );
		}
	}


	public class RocketFactory : EntityFactory
	{
		int		damage;
		float	impulse;
		Entity	attacker;

		public RocketFactory( Vector3 position, Quaternion rotation, Vector3 dir, float lag, Entity attacker, int damage, float impulse )
		{
			this.Position	=	position + dir * 300 * lag;
			this.Rotation	=	rotation;
			this.damage		=	damage;
			this.impulse	=	impulse;
			this.attacker	=	attacker;
		}

		public override void Construct( Entity e, IGameState gs )
		{
			base.Construct( e, gs );
			e.AddComponent( new ProjectileComponent( attacker, 300, 12, 10, "rocketExplosion", damage, impulse) );
			e.AddComponent( new FXComponent("rocketTrail", true) );
		}
	}
}
