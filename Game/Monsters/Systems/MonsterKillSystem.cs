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
using IronStar.ECSFactories;

namespace IronStar.Monsters.Systems
{
	class MonsterKillSystem : ISystem
	{
		public Aspect GetAspect() { return Aspect.Empty; }
		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}

		readonly Aspect monsterAspect = new Aspect().Include<HealthComponent,UserCommandComponent,Transform>().Single<AIComponent,PlayerComponent>();
		readonly Aspect ragdollAspect = new Aspect().Include<RagdollComponent>();


		public void Update( IGameState gs, GameTime gameTime )
		{
			foreach ( var monsterEntity in gs.QueryEntities(monsterAspect) )
			{
				var health	=	monsterEntity.GetComponent<HealthComponent>();
				var uc		=	monsterEntity.GetComponent<UserCommandComponent>();
				var t		=	monsterEntity.GetComponent<Transform>();

				var isMonster	  = monsterEntity.ContainsComponent<AIComponent>();

				if (health.Health<-50)
				{
					CreatGiblets( gs, t.Position, Vector3.Up );	

					monsterEntity.Kill();
				}

				if (health.Health<=0)
				{
					monsterEntity.RemoveComponent<CharacterController>();

					if (!ragdollAspect.Accept(monsterEntity))
					{
						monsterEntity.AddComponent( new RagdollComponent() );
					}
				}
			}
		}


		void CreatGiblets( IGameState gs, Vector3 position, Vector3 direction )
		{
			var rand	=	MathUtil.Random;
			var physics	=	gs.GetService<PhysicsCore>();

			for (int i=0; i<5; i++)
			{
				var pos0 = position + Vector3.Up;
				var pos1 = position + rand.GaussRadialDistribution( 1, 0.3f ) + Vector3.Up;
				var fact = new GibletFactory();
				fact.Position			=	pos1;
				fact.Rotation			=	Quaternion.RotationYawPitchRoll( rand.NextFloat(0, MathUtil.TwoPi), rand.NextFloat(0, MathUtil.TwoPi), rand.NextFloat(0, MathUtil.TwoPi) );
				fact.LinearVelocity		=	(rand.GaussRadialDistribution( 0, 1 ) + Vector3.Up) * 30;
				fact.AngularVelocity	=	rand.GaussRadialDistribution( 5, 2 );
				var gib	 = gs.Spawn(fact);
			}
		}
	}
}
