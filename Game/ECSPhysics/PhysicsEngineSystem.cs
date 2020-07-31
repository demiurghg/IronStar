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

		public Aspect GetAspect ()
		{
			return new Aspect()
					.Include<Transform,Velocity>()
					.Single<CharacterController,DynamicBox>();
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

			TeleportDynamicObjects<DynamicBox>(gs);
			TeleportDynamicObjects<CharacterController>(gs);

			UpdateCharacterUserCommands(gs);

			UpdateSimulation( gameTime.ElapsedSec );

			UpdateDynamicObjects<DynamicBox>(gs);
			UpdateDynamicObjects<CharacterController>(gs);

			UpdateTouchEvents();
		}


		void UpdateGravity( GameState gs )
		{
			var gravity					=	gs.QueryComponents<Gravity>().FirstOrDefault();
			var gravityMagnitude		=	gravity==null ? 0 : gravity.Magnitude;
			var gravityVector			=	Vector3.Down * gravityMagnitude;
			Space.ForceUpdater.Gravity	=	MathConverter.Convert( gravityVector );
		}


		void UpdateCharacterUserCommands( GameState gs )
		{
			var entities	=	gs.QueryEntities<CharacterController,UserCommand2>();

			foreach ( var e in entities )
			{
				var uc	=	e.GetComponent<UserCommand2>();
				var ch	=	e.GetComponent<CharacterController>();

				ch.Movement	=	uc.MovementVector;
			}
		}


		void UpdateDynamicObjects<TObject>( GameState gs ) where TObject: IMotionState, IComponent
		{
			var entities	=	gs.QueryEntities<TObject,Transform,Velocity>();

			foreach ( var e in entities ) 
			{
				var obj	=	e.GetComponent<TObject>();
				var t	=	e.GetComponent<Transform>();
				var v	=	e.GetComponent<Velocity>();

				t.Position	=	obj.Position;
				t.Rotation	=	obj.Rotation;
				v.Linear	=	obj.LinearVelocity;
				v.Angular	=	obj.AngularVelocity;
			}
		}


		void TeleportDynamicObjects<TObject>( GameState gs ) where TObject: IMotionState, IComponent
		{
			var entities	=	gs.QueryEntities<Teleport,TObject,Transform,Velocity>();

			foreach ( var e in entities ) 
			{
				var box	=	e.GetComponent<TObject>();
				var t	=	e.GetComponent<Transform>();
				var v	=	e.GetComponent<Velocity>();
				var tp	=	e.GetComponent<Teleport>();

				box.Position		=	t.Position;
				box.Rotation		=	t.Rotation;
				box.LinearVelocity	=	v.Linear;
				box.AngularVelocity	=	v.Angular;

				e.RemoveComponent(tp);
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
			foreach ( var touchEvent in touchEvents ) 
			{
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

		public void Add( GameState gs, Entity e )
		{
			//Log.Message("Entity "
			//throw new Exception( "The method or operation is not implemented." );
		}

		public void Remove( GameState gs, Entity e )
		{
			//throw new Exception( "The method or operation is not implemented." );
		}
	}
}
