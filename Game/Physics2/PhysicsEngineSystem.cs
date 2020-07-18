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

namespace IronStar.Physics2
{
	public class PhysicsEngineSystem : ISystem 
	{
		Space physSpace = new Space();

		HashSet<Tuple<Entity,Entity>> touchEvents;

		public CollisionGroup StaticGroup		= new CollisionGroup();
		public CollisionGroup KinematicGroup	= new CollisionGroup();
		public CollisionGroup DymamicGroup		= new CollisionGroup();
		public CollisionGroup PickupGroup		= new CollisionGroup();
		public CollisionGroup CharacterGroup	= new CollisionGroup();

		public Space Space {
			get {
				return physSpace;
			}
		}


		public PhysicsEngineSystem ()
		{
			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );
		}


		public void Intialize( GameState gs ) { /* add static meshes here */ }
		public void Shutdown( GameState gs ) {}


		public void Update( GameState gs, GameTime gameTime )
		{
			UpdateGravity( gs );

			UpdateSimulation( gameTime.ElapsedSec );

			UpdateEntityPositions( gs );

			UpdateTouchEvents();
		}


		void UpdateGravity( GameState gs )
		{
			var gravity					=	gs.QueryComponents<Gravity>().FirstOrDefault();
			var gravityMagnitude		=	gravity==null ? 0 : gravity.Gravity;
			var gravityVector			=	Vector3.Down * gravityMagnitude;
			Space.ForceUpdater.Gravity	=	MathConverter.Convert( gravityVector );
		}


		void UpdateEntityPositions( GameState gs )
		{
			var chars	=	gs.QueryComponents<CharacterController>();
			var boxes	=	gs.QueryComponents<DynamicBox>();

			foreach ( var ch in chars ) 
			{
				ch.Entity.Position	=	ch.Position;
				ch.Entity.Rotation	=	Quaternion.Identity;
			}

			foreach ( var box in boxes ) 
			{
				box.Entity.Position	=	box.Position;
				box.Entity.Rotation	=	box.Orientation;
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


		public void UpdateTouchEvents()
		{
			foreach ( var touchEvent in touchEvents ) {
				Log.Warning("UpdateTouchEvents -- not implemented");
				//touchEvent.Item1?.Touch( touchEvent.Item2, Vector3.Zero );
			}
			touchEvents.Clear();
		}


		public void HandleTouch ( CollidablePairHandler pair )
		{
			var entity1 = pair.EntityA?.Tag as Entity;
			var entity2 = pair.EntityB?.Tag as Entity;

			touchEvents.Add( new Tuple<Entity, Entity>( entity1, entity2 ) );
			touchEvents.Add( new Tuple<Entity, Entity>( entity2, entity1 ) );
		}
	}
}
