using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.SFX2;
using Native.NRecast;
using System.ComponentModel;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.BTCore.Actions;
using IronStar.BTCore.Decorators;
using IronStar.AI.Actions;
using IronStar.Gameplay;

namespace IronStar.AI
{
	class BehaviorSystem : ProcessingSystem<BTNode,BehaviorComponent>
	{
		public bool Enabled = true;
		readonly PhysicsCore physics;

		AITokenPool tokenPool = new AITokenPool(2, TimeSpan.FromMilliseconds(500) );

		public BehaviorSystem(PhysicsCore physics)
		{
			this.physics	=	physics;
		}


		public const string KEY_TARGET_ENTITY	=	"targetEntity";
		public const string KEY_ROAMING_POINT	=	"roamingPoint";
		public const string KEY_PLAYER_LOCATION	=	"playerLocation";
		public const string KEY_COMBAT_LOCATION	=	"combatLocation";


		protected override BTNode Create( Entity entity, BehaviorComponent component1 )
		{
			var approach = 
				new Success(
					new HasLineOfSight( KEY_TARGET_ENTITY, ConditionMode.Continuous|ConditionMode.Inverse,
						new Selector(
							new Sequence(
								new FindPlayer( KEY_PLAYER_LOCATION ),
								new MoveTo( KEY_PLAYER_LOCATION )
							),
							new Sequence(
								new FindReachablePointInRadius( KEY_COMBAT_LOCATION, 60),
								new MoveTo( KEY_COMBAT_LOCATION ),
								new Wait(500, 700)
							)
						)
					)
				);


			var tacticalMove = new Sequence(
					new FindReachablePointInRadius( KEY_COMBAT_LOCATION, 30),
					new MoveTo( KEY_COMBAT_LOCATION ),
					new Wait(200,500)
				);

			var attack = 
				new HasLineOfSight( KEY_TARGET_ENTITY, ConditionMode.Continuous,
					new Sequence(
						new Attack( KEY_TARGET_ENTITY, 350, 750, 1.5f ),
						new Wait( 300, 700 ),
						tacticalMove
					)
				);

			var roaming = 
				new Sequence(
					new FindReachablePointInRadius( KEY_ROAMING_POINT, 300 ),
					new MoveTo( KEY_ROAMING_POINT ),
					new Wait( 1500, 2500 )
				);

			return 
				new Selector( 
					new HasBlackboardValue<Entity>( KEY_TARGET_ENTITY, ConditionMode.Continuous, 
						new Selector(
							new AcquireToken(tokenPool,
								new Sequence(
									approach, 
									new Wait(50,150),
									attack
								)
							),
							tacticalMove
						)
					),
					new HasBlackboardValue<Entity>( KEY_TARGET_ENTITY, ConditionMode.Continuous|ConditionMode.Inverse, roaming )
				);
			//return new BTBuilder()
			//.Selector()
			//	.Sequence()
			//		.Decorator(
			//	.End()
			//	.Sequence()
			//		.Action( new Print("Searching...") )
			//		.Action(  )
			//		//.Action( new FindPlayer("playerLocation") )
			//		.Action( new FindReachablePointInRadius("roamingPoint", 300) )
			//		.Action( new MoveTo("roamingPoint") )
			//		//.Repeat( 3, new BTBuilder().Sequence().Action( new Wait(100) ).Action( new Print("...") ).End() )
			//		//.Action( new Wait(1000) )
			//	.End()
			//.End();
		}

		protected override void Destroy( Entity entity, BTNode resource )
		{
			tokenPool.RestoreTokens(entity);
		}


		
		public override void Update( GameState gs, GameTime gameTime )
		{
			base.Update( gs, gameTime );

			tokenPool.Update( gameTime );

			DebugDrawTokens( gs );
		}


		protected override IEnumerable<Entity> OrderEntities( IEnumerable<Entity> entities )
		{
			return entities.Shuffle( MathUtil.Random );
		}


		protected override void Process( Entity entity, GameTime gameTime, BTNode behaviorTree, BehaviorComponent behavior )
		{
			if (Enabled)
			{
				behaviorTree.Tick( gameTime, entity, false );
			}
		}



		void DebugDrawTokens( GameState gs )
		{
			var dr	=	gs.GetService<RenderSystem>().RenderWorld.Debug;

			foreach ( var token in tokenPool )
			{
				if (token.Owner!=null)
				{
					dr.DrawRing( Matrix.Translation(token.Owner.Location), 2, Color.Orange, 32, 2, 1 );
				}
			}
		}
	}
}
