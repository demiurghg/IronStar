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
		readonly Space physSpace;

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

		ConcurrentQueue<Tuple<Entity,Entity>> touchEvents;

		public readonly BEPUCollisionGroup StaticGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup KinematicGroup	= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup DymamicGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup PickupGroup		= new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup CharacterGroup	= new BEPUCollisionGroup();

		struct DeferredImpulse
		{
			public readonly Entity Entity;
			public readonly Vector3 Position;
			public readonly Vector3 Impulse;

			public DeferredImpulse( Entity entity, Vector3 position, Vector3 impulse )
			{
				this.Entity		=	entity;
				this.Position	=	position;
				this.Impulse	=	impulse;
			}
		}

		readonly ConcurrentQueue<ISpaceObject>		creationQueue	=	new ConcurrentQueue<ISpaceObject>();
		readonly ConcurrentQueue<ISpaceObject>		removalQueue	=	new ConcurrentQueue<ISpaceObject>();
		readonly List<ISpaceObject>					objectList		=	new List<ISpaceObject>();
		readonly ConcurrentQueue<DeferredImpulse>	impulseQueue	=	new ConcurrentQueue<DeferredImpulse>();
		readonly ConcurrentQueue<Action>			actionQueue		=	new ConcurrentQueue<Action>();
		
		public PhysicsCore ()
		{
			physSpace		=	new Space();
			physSpace.BufferedStates.Enabled = false;
			physSpace.BufferedStates.InterpolatedStates.Enabled = false;

			touchEvents	=	new ConcurrentQueue<Tuple<Entity,Entity>>();

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
				Action action;

				time = (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency; //compute the current time
				dt = time - previousTime; //find the time passed since the previous frame
				previousTime = time;

				lock (Space)
				{
					while (creationQueue.TryDequeue(out spaceObj)) Space.Add(spaceObj);

					while (actionQueue.TryDequeue(out action)) action?.Invoke();

					ApplyDeferredImpulses();

					if (Enabled)
					{
						Space.Update((float)dt);
					}

					while (removalQueue.TryDequeue(out spaceObj))
					{
						try 
						{
							Space.Remove(spaceObj);
						} 
						catch (Exception e)
						{
							Log.Warning(e.Message);
						}
					}
				}

				//Thread.Sleep(100); //Explicitly give other threads (if any) a chance to execute
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


		public void ApplyImpulse( Entity entity, Vector3 position, Vector3 impulse )
		{
			impulseQueue.Enqueue( new DeferredImpulse( entity, position, impulse ) );
		}


		void ApplyDeferredImpulses()
		{
			DeferredImpulse impulse;

			while (impulseQueue.TryDequeue(out impulse))
			{
				foreach (var physEntity in Space.Entities)
				{
					if (physEntity.Tag==impulse.Entity)
					{
						var p = MathConverter.Convert( impulse.Position );
						var i = MathConverter.Convert( impulse.Impulse );
						physEntity.ApplyImpulse( p, i );
					}
				}
			}
		}


		public void Invoke( Action action )
		{
			actionQueue.Enqueue( action );
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

			Tuple<Entity,Entity> touch;

			while ( touchEvents.TryDequeue( out touch ))
			{
				var e1	=	touch.Item1;
				var e2	=	touch.Item2;

				e1.GetComponent<TouchDetector>()?.AddTouch( e2 );
			}
		}


		public void HandleTouch ( CollidablePairHandler pair )
		{
			var entity1 = pair.EntityA?.Tag as Entity;
			var entity2 = pair.EntityB?.Tag as Entity;

			// do not handle touches with null-entities :
			if (entity1==null || entity2==null)	return;

			touchEvents.Enqueue( new Tuple<Entity, Entity>( entity1, entity2 ) );
			touchEvents.Enqueue( new Tuple<Entity, Entity>( entity2, entity1 ) );
		}
	}
}
