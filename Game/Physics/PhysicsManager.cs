using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUVector3 = BEPUutilities.Vector3;
using BEPUTransform = BEPUutilities.AffineTransform;
using IronStar.Core;
using Fusion.Engine.Common;
using IronStar.SFX;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using Fusion;

namespace IronStar.Physics {
	public class PhysicsManager {

		Space physSpace = new Space();

		LinkedList<KinematicModel> kinematics = new LinkedList<KinematicModel>();

		public readonly Game	Game;
		public readonly GameWorld World;

		HashSet<Tuple<Entity,Entity>> touchEvents;

		public CollisionGroup StaticGroup		= new CollisionGroup();
		public CollisionGroup KinematicGroup	= new CollisionGroup();
		public CollisionGroup DymamicGroup		= new CollisionGroup();
		public CollisionGroup PickupGroup		= new CollisionGroup();
		public CollisionGroup CharacterGroup	= new CollisionGroup();


		public Space PhysSpace {
			get {
				return physSpace;
			}
		}
		

		public float Gravity {
			get {
				return -physSpace.ForceUpdater.Gravity.Y;
			}
			set {
				physSpace.ForceUpdater.Gravity = new BEPUVector3(0, -value, 0);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public PhysicsManager ( GameWorld world, float gravity )
		{
			this.World	=	world;
			Game		=	world.Game;

			Gravity		=	gravity;

			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="pair"></param>
		public void HandleTouch ( CollidablePairHandler pair )
		{
			var entity1 = pair.EntityA?.Tag as Entity;
			var entity2 = pair.EntityB?.Tag as Entity;

			touchEvents.Add( new Tuple<Entity, Entity>( entity1, entity2 ) );
			touchEvents.Add( new Tuple<Entity, Entity>( entity2, entity1 ) );

			//entity1?.Touch( entity2, Vector3.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		public void Update ( float elapsedTime )
		{
			//	update kinematics :
			foreach ( var k in kinematics ) {
				k.Update();
			}

			//	update physical space :
			if (elapsedTime==0) {
				physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 1;
				physSpace.TimeStepSettings.TimeStepDuration = 1/1024.0f;
				physSpace.Update(1/1024.0f);
				return;
			}

			var dt	=	elapsedTime;
			physSpace.TimeStepSettings.MaximumTimeStepsPerFrame = 5;
			physSpace.TimeStepSettings.TimeStepDuration = 1.0f/60.0f;
			var steps = physSpace.Update(dt);

			if (steps!=1) 
			{
				//Log.Warning("{0} steps of physics simulation", steps);
			}

			//	update touch events :
			foreach ( var touchEvent in touchEvents ) {
				touchEvent.Item1?.Touch( touchEvent.Item2, Vector3.Zero );
			}
			touchEvents.Clear();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelAtom"></param>
		public KinematicModel AddKinematicModel ( short modelAtom, Entity entity )
		{
			var modelName	=	World.Atoms[modelAtom];

			var modelDesc	=	World.Content.Load<ModelFactory>( @"models\" + modelName );

			var scene		=	World.Content.Load<Scene>( modelDesc.ScenePath );

			var model		=	new KinematicModel( this, modelDesc, scene, entity ); 

			kinematics.AddLast( model );

			return model;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="staticModel"></param>
		/// <returns></returns>
		public bool Remove ( KinematicModel staticModel )
		{
			staticModel.Destroy();
			return kinematics.Remove( staticModel );
		}
	}
}
