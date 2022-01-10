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
using BEPUphysics.EntityStateManagement;
using Fusion.Core.Configuration;

namespace IronStar.ECSPhysics
{
	public interface ITransformFeeder
	{
		void FeedTransform( IGameState gs, GameTime gameTime );
	}

	/// <summary>
	/// https://github.com/bepu/bepuphysics1/blob/master/Documentation/Isolated%20Demos/AsynchronousUpdateDemo/AsynchronousUpdateGame.cs
	/// </summary>
	[ConfigClass]
	public partial class PhysicsCore : DisposableBase, ISystem
	{
		[Config] public static bool UseDebugDraw { get; set; } = false;
		[Config] public static bool SkipDebugLines { get; set; } = false;
		[Config] public static bool SkipDebugModels { get; set; } = false;

		public delegate RigidTransform TransformCallback( ISpaceObject spaceObject, Transform transform );

		public readonly Game Game;

		readonly Space physSpace;

		internal Space Space 
		{
			get { return physSpace; }
		}

		Stopwatch stopwatch;

		const int MaxPhysObjects = 16384;
		RigidTransform[] transforms = new RigidTransform[MaxPhysObjects];

		ConcurrentQueue<Tuple<Entity,Entity>> touchEvents;

		public readonly BEPUCollisionGroup StaticGroup		=	new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup KinematicGroup	=	new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup DymamicGroup		=	new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup PickupGroup		=	new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup CharacterGroup	=	new BEPUCollisionGroup();
		public readonly BEPUCollisionGroup RagdollGroup		=	new BEPUCollisionGroup();

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

		readonly List<ISpaceObject>						objectList			=	new List<ISpaceObject>();
		readonly ConcurrentQueue<DeferredImpulse>		impulseQueue		=	new ConcurrentQueue<DeferredImpulse>();
		readonly ConcurrentQueue<Action>				actionQueue			=	new ConcurrentQueue<Action>();
		
		public PhysicsCore (Game game)
		{
			this.Game	=	game;
			physSpace	=	new Space();

			touchEvents	=	new ConcurrentQueue<Tuple<Entity,Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup	), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup	), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup	), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup		), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup	), CollisionRule.NoSolver );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( RagdollGroup,	RagdollGroup	), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( RagdollGroup,	CharacterGroup	), CollisionRule.NoSolver );

			stopwatch	=	new Stopwatch();
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
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
				case CollisionGroup.Ragdoll			: return RagdollGroup	;
				default: throw new ArgumentException("group");
			}
		}


		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		public void Update( IGameState gs, GameTime gameTime )
		{
			UpdateGravity(gs);

			if (!gs.Paused)
			{
				UpdateSimulation( gs, gameTime.ElapsedSec );

				UpdateTouchEvents(gs);

				UpdateTransforms( gs, gameTime );
			}
		}

		
		public static void UpdateTransformFromMotionState( MotionState ms, Transform t )
		{
			t.Move( ms );
		}


		/*-----------------------------------------------------------------------------------------------
		 *	Acessor stuff :
		-----------------------------------------------------------------------------------------------*/

		public class SpaceObjectArgs : EventArgs 
		{
			public ISpaceObject SpaceObject;
		}

		public event EventHandler<SpaceObjectArgs>	ObjectAdded;
		public event EventHandler<SpaceObjectArgs>	ObjectRemoved;

		public void Add( ISpaceObject physObj )
		{
			Space.Add( physObj );

			ObjectAdded?.Invoke(this, new SpaceObjectArgs() { SpaceObject = physObj } );
		}


		public void Remove( ISpaceObject physObj )
		{
			Space.Remove( physObj );

			ObjectRemoved?.Invoke(this, new SpaceObjectArgs() { SpaceObject = physObj } );
		}


		public void ApplyImpulse( Entity entity, Vector3 location, Vector3 impulse )
		{
			//	#TODO #PHYSICS #PHYS -- accumulate impulse
			var impulseComponent	= entity.GetComponent<ImpulseComponent>();
			var transformComponent	= entity.GetComponent<ImpulseComponent>();

			if (impulseComponent==null)
			{
				impulseComponent	=	new ImpulseComponent( location, impulse );
				entity.AddComponent( impulseComponent );
			}
		}


		void ApplyDeferredImpulses( IGameState gs )
		{
			Aspect impulseAspect = new Aspect().Include<ImpulseComponent>();

			foreach ( var impulsedEntity in gs.QueryEntities( impulseAspect ) )
			{
				var impulse = impulsedEntity.GetComponent<ImpulseComponent>();

				if (impulse.Applied)
				{
					//	#HACK #PHYSICS -- keep impulse for entire frame but apply once
					impulsedEntity.RemoveComponent<ImpulseComponent>();
				}
				else
				{
					foreach (var physEntity in Space.Entities)
					{
						if (physEntity.Tag==impulsedEntity)
						{
							var p = MathConverter.Convert( impulse.Location );
							var i = MathConverter.Convert( impulse.Impulse );
							physEntity.ApplyImpulse( p, i );
						}
					}

					impulse.Applied = true;
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
		
		void UpdateGravity( IGameState gs )
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



		void UpdateSimulation ( IGameState gs, float elapsedTime )
		{
			Space.TimeStepSettings.TimeStepDuration				=	elapsedTime;
			Space.TimeStepSettings.MaximumTimeStepsPerFrame		=	1;

			Action action;

			//	dequeue actions :
			while (actionQueue.TryDequeue(out action)) 
			{
				action?.Invoke();
			}

			//	apply impulses :
			ApplyDeferredImpulses(gs);

			//	run simulation :
			Space.Update();
		}


		void UpdateTouchEvents( IGameState gs )
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


		void UpdateTransforms( IGameState gs, GameTime gt )
		{
			foreach ( var system in gs.Systems )
			{
				var feeder = system as ITransformFeeder;
				feeder?.FeedTransform( gs, gt );
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
