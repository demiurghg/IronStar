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

		public CollisionGroup StaticGroup		= new CollisionGroup();
		public CollisionGroup KinematicGroup	= new CollisionGroup();
		public CollisionGroup DymamicGroup		= new CollisionGroup();
		public CollisionGroup PickupGroup		= new CollisionGroup();
		public CollisionGroup CharacterGroup	= new CollisionGroup();
		
		public PhysicsCore ()
		{
			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );
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

			UpdateSimulation( gameTime.ElapsedSec );
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


		void UpdateSimulation ( float elapsedTime )
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
	}
}
