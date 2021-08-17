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
using BEPUutilities.Threading;
using System.Threading;
using System.Diagnostics;
using RigidTransform = BEPUutilities.RigidTransform;
using System.Collections.Concurrent;

namespace IronStar.ECSPhysics
{
	/// <summary>
	/// https://github.com/bepu/bepuphysics1/blob/master/Documentation/Isolated%20Demos/AsynchronousUpdateDemo/AsynchronousUpdateGame.cs
	/// </summary>
	public partial class PhysicsCore : DisposableBase, ISystem
	{
		Space physSpace;

		private Space Space 
		{
			get { return physSpace; }
		}

		Thread physicsThread;
		Stopwatch stopwatch;
		bool stopRequest = false;

		public bool Enabled { get; set; } = true;

		const int MaxPhysObjects = 16384;
		RigidTransform[] transforms = new RigidTransform[MaxPhysObjects];

		HashSet<Tuple<Entity,Entity>> touchEvents;

		public readonly BEPUCollisionGroup StaticGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup KinematicGroup	= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup DymamicGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup PickupGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup CharacterGroup	= new BEPUCollisionGroup();

		ConcurrentQueue<ISpaceObject>	creationQueue		= new ConcurrentQueue<ISpaceObject>();
		ConcurrentQueue<ISpaceObject>	removalQueue		= new ConcurrentQueue<ISpaceObject>();
		
		public PhysicsCore ()
		{
			physSpace		=	new Space();
			physSpace.BufferedStates.Enabled = true;
			physSpace.BufferedStates.InterpolatedStates.Enabled = true;

			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );

			stopwatch			=	new Stopwatch();

			physicsThread				=	new Thread(PhysicsLoop);
			physicsThread.IsBackground	=	true;
			physicsThread.Name			=	"PhysicsThread";
			physicsThread.Start();
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				stopRequest	=	true;
				physicsThread.Join();
			}
			
			base.Dispose( disposing );
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

			if (Enabled)
			{
				UpdateSimulation( gs, gameTime.ElapsedSec );

				UpdateTransforms(gs);

				UpdateTouchEvents(gs);
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Parallel stuff :
		-----------------------------------------------------------------------------------------------*/
		
		void PhysicsLoop()
		{
			double dt;
			double time;
			double previousTime = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency; //Give the engine a reasonable starting point.

			while (!stopRequest)
			{
				ISpaceObject spaceObj;

				time = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency; //compute the current time
				dt = time - previousTime; //find the time passed since the previous frame
				previousTime = time;

				while (creationQueue.TryDequeue(out spaceObj))
				{
					Space.Add(spaceObj);
				}

				if (Enabled)
				{
					Space.Update((float)dt);
				}

				while (removalQueue.TryDequeue(out spaceObj))
				{
					Space.Remove(spaceObj);
				}

				Thread.Sleep(0); //Explicitly give other threads (if any) a chance to execute
			}
		}


		public void Add( ISpaceObject physObj )
		{
			creationQueue.Enqueue( physObj );
		}


		public void Remove( ISpaceObject physObj )
		{
			removalQueue.Enqueue( physObj );
		}


		/*-----------------------------------------------------------------------------------------------
		 *	
		-----------------------------------------------------------------------------------------------*/
		
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
			/*if (elapsedTime==0)
			 {
				physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 1;
				physSpace.TimeStepSettings.TimeStepDuration = 1/1024.0f;
				physSpace.Update(1/1024.0f);
				return;
			}

			var dt	=	elapsedTime;
			physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 5;
			physSpace.TimeStepSettings.TimeStepDuration = 1.0f/60.0f;
			var steps = physSpace.Update(dt);  */
		}


		void UpdateTransforms( GameState gs )
		{
			int count = physSpace.BufferedStates.Entities.Count;
			//physSpace.BufferedStates.InterpolatedStates.GetStates( transforms );
			physSpace.BufferedStates.InterpolatedStates.FlipBuffers();

			foreach ( var transformFeeder in gs.GatherSystems<ITransformFeeder>() )
			{
				transformFeeder.FeedTransform(gs, transforms);
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


		public static void ApplyImpulse( Entity entity, Vector3 location, Vector3 impulse )
		{
			if (entity==null) return;

			if (!entity.ContainsComponent<ImpulseComponent>())
			{
				entity.AddComponent( new ImpulseComponent(location, impulse) );
			}
			else
			{
				var impulseComponent		= 	entity.GetComponent<ImpulseComponent>();
				impulseComponent.Impulse	=	impulse;
				impulseComponent.Location	=	location;
			}
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
