using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.ECSPhysics;
using IronStar.Gameplay;

namespace IronStar.Monsters.Systems
{
	class MonsterKillSystem : ISystem
	{
		public Aspect GetAspect() { return Aspect.Empty; }
		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}

		public void Update( GameState gs, GameTime gameTime )
		{
			var monsterAspect = new Aspect().Include<HealthComponent,UserCommandComponent>().Single<BehaviorComponent,PlayerComponent>();

			foreach ( var monsterEntity in gs.QueryEntities(monsterAspect) )
			{
				var health	=	monsterEntity.GetComponent<HealthComponent>();
				var uc		=	monsterEntity.GetComponent<UserCommandComponent>();

				var isMonster	  = monsterEntity.ContainsComponent<BehaviorComponent>();

				if (health.Health<-50)
				{
					CreatGiblets( gs, monsterEntity.GetLocation(), Vector3.Up );	

					gs.Kill( monsterEntity );
				}

				if (health.Health<=0)
				{
					monsterEntity.RemoveComponent<BehaviorComponent>();
				}
			}
		}


		void CreatGiblets( GameState gs, Vector3 position, Vector3 direction )
		{
			var rand = MathUtil.Random;

			for (int i=0; i<5; i++)
			{
				var pos0 = position + Vector3.Up;
				var pos1 = position + rand.GaussRadialDistribution( 1, 0.3f ) + Vector3.Up;
				var gib	 = gs.Spawn("GIBLET", pos1, Quaternion.Identity);
				var imp  = rand.GaussRadialDistribution( 10, 5 );

				PhysicsCore.ApplyImpulse( gib, pos0, imp );
			}
		}
	}
}
