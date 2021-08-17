using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.CollisionRuleManagement;
using BEPUEntity = BEPUphysics.Entities.Entity;
using RigidTransform = BEPUutilities.RigidTransform;
using IronStar.ECS;

namespace IronStar.ECSPhysics 
{
	public class DynamicCollisionSystem : ProcessingSystem<Box,DynamicBox,KinematicState>
	{
		readonly PhysicsCore physics;

		public DynamicCollisionSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override Box Create( Entity entity, DynamicBox box, KinematicState t )
		{
			var ms				=	new MotionState();
			ms.LinearVelocity	=	MathConverter.Convert( t.LinearVelocity );
			ms.AngularVelocity	=	MathConverter.Convert( t.AngularVelocity );
			ms.Position			=	MathConverter.Convert( t.Position );
			ms.Orientation		=	MathConverter.Convert( t.Rotation );

			var cbox				=	new Box( ms, box.Width, box.Height, box.Depth, box.Mass );
			cbox.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			cbox.Tag				=	entity;

			cbox.CollisionInformation.Events.InitialCollisionDetected +=Events_InitialCollisionDetected;
			cbox.CollisionInformation.CollisionRules.Group = physics.GetCollisionGroup( box.Group );

			physics.Add( cbox );

			return cbox;
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}

		
		protected override void Destroy( Entity entity, Box cbox )
		{
			physics.Remove( cbox );
		}

		
		protected override void Process( Entity entity, GameTime gameTime, Box cbox, DynamicBox box, KinematicState t )
		{
			var impulse	=	entity.GetComponent<ImpulseComponent>();

			if (impulse!=null && impulse.Impulse!=Vector3.Zero)
			{
				cbox.ApplyImpulse( MathConverter.Convert(impulse.Location), MathConverter.Convert(impulse.Impulse) );
				impulse.Location = Vector3.Zero;
				impulse.Impulse  = Vector3.Zero;
			}

			t.LinearVelocity	=	MathConverter.Convert( cbox.LinearVelocity	);
			t.AngularVelocity	=	MathConverter.Convert( cbox.AngularVelocity	);
			t.Position			=	MathConverter.Convert( cbox.BufferedStates.InterpolatedStates.Position );
			t.Rotation			=	MathConverter.Convert( cbox.BufferedStates.InterpolatedStates.Orientation );
		}
	}
}
