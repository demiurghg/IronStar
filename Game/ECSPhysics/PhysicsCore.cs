using System;
using System.Collections.Generic;
using BEPUphysics;
using System.Linq;
using BEPUVector3 = BEPUutilities.Vector3;
using IronStar.SFX;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core.Mathematics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using Fusion.Core;
using Fusion;
using IronStar.Gameplay;
using Fusion.Core.Extensions;
using BEPUCollisionGroup = BEPUphysics.CollisionRuleManagement.CollisionGroup;

namespace IronStar.ECSPhysics
{
	public class PhysicsCore : ISystem
	{
		Space physSpace = new Space();

		public Space Space 
		{
			get { return physSpace; }
		}

		HashSet<Tuple<Entity,Entity>> touchEvents;

		public readonly BEPUCollisionGroup StaticGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup KinematicGroup	= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup DymamicGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup PickupGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup CharacterGroup	= new BEPUCollisionGroup();
		
		public PhysicsCore ()
		{
			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );
		}


		public BEPUCollisionGroup GetCollisionGroup( CollisionGroup group )
		{
			switch (group)
			{
				case CollisionGroup.StaticGroup		: return StaticGroup	;
				case CollisionGroup.KinematicGroup	: return KinematicGroup	;
				case CollisionGroup.DymamicGroup	: return DymamicGroup	;
				case CollisionGroup.PickupGroup		: return PickupGroup	;
				case CollisionGroup.CharacterGroup	: return CharacterGroup	;
				default: throw new ArgumentException("group");
			}
		}

		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}


		public void Update( GameState gs, GameTime gameTime )
		{
			UpdateGravity(gs);

			UpdateSimulation( gs, gameTime.ElapsedSec );

			UpdateTransforms(gs);

			UpdateTouchEvents(gs);
		}


		void UpdateGravity( GameState gs )
		{
			var gravityAspect			=	new Aspect().Include<GravityComponent>();
			var gravityEntity			=	gs.QueryEntities(gravityAspect).FirstOrDefault();

			if (gravityEntity!=null)
			{
				var gravityComponent		=	gravityEntity.GetComponent<GravityComponent>();
				var gravityMagnitude		=	gravityComponent.Magnitude;
				var gravityVector			=	Vector3.Down * gravityMagnitude;

				Space.ForceUpdater.Gravity	=	MathConverter.Convert( gravityVector );
			}
		}


		void UpdateSimulation ( GameState gs, float elapsedTime )
		{
			if (elapsedTime==0)
			 {
				physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 1;
				physSpace.TimeStepSettings.TimeStepDuration = 1/1024.0f;
				physSpace.Update(1/1024.0f);
				return;
			}

			var dt	=	elapsedTime;
			physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 5;
			physSpace.TimeStepSettings.TimeStepDuration = 1.0f/60.0f;
			var steps = physSpace.Update(dt);
		}


		void UpdateTransforms( GameState gs )
		{
			foreach ( var transformFeeder in gs.GatherSystems<ITransformFeeder>() )
			{
				transformFeeder.FeedTransform(gs);
			}
		}


		void UpdateTouchEvents( GameState gs )
		{
			var touchAspect = new Aspect().Include<TouchDetector>();

			foreach ( var e in gs.QueryEntities(touchAspect) )
			{
				e.GetComponent<TouchDetector>()?.ClearTouches();
			}

			foreach ( var pair in touchEvents )
			{
				var e1	=	pair.Item1;
				var e2	=	pair.Item2;

				e1.GetComponent<TouchDetector>()?.AddTouch( e2 );
			}

			touchEvents.Clear();
		}


		public void HandleTouch ( CollidablePairHandler pair )
		{
			var entity1 = pair.EntityA?.Tag as Entity;
			var entity2 = pair.EntityB?.Tag as Entity;

			// do not handle touches with null-entities :
			if (entity1==null || entity2==null)	return;

			touchEvents.Add( new Tuple<Entity, Entity>( entity1, entity2 ) );
			touchEvents.Add( new Tuple<Entity, Entity>( entity2, entity1 ) );
		}
	}
}
