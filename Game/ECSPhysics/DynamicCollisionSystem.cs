﻿using System;
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
using IronStar.ECS;

namespace IronStar.ECSPhysics 
{
	public class DynamicCollisionSystem : ProcessingSystem<Box,DynamicBox,Transform,Velocity>
	{
		readonly PhysicsCore physics;

		public DynamicCollisionSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override Box Create( Entity entity, DynamicBox box, Transform t, Velocity v )
		{
			var ms				=	new MotionState();
			ms.LinearVelocity	=	MathConverter.Convert( v.Linear );
			ms.AngularVelocity	=	MathConverter.Convert( v.Angular );
			ms.Position			=	MathConverter.Convert( t.Position );
			ms.Orientation		=	MathConverter.Convert( t.Rotation );

			var cbox				=	new Box( ms, box.Width, box.Height, box.Depth, box.Mass );
			cbox.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			cbox.Tag				=	entity;

			cbox.CollisionInformation.Events.InitialCollisionDetected +=Events_InitialCollisionDetected;
			cbox.CollisionInformation.CollisionRules.Group = physics.GetCollisionGroup( box.Group );

			physics.Space.Add( cbox );

			return cbox;
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}

		
		protected override void Destroy( Entity entity, Box cbox )
		{
			physics.Space.Remove( cbox );
		}

		
		protected override void Process( Entity entity, GameTime gameTime, Box cbox, DynamicBox box, Transform t, Velocity v )
		{
			v.Linear	=	MathConverter.Convert( cbox.LinearVelocity	);
			v.Angular	=	MathConverter.Convert( cbox.AngularVelocity	);
			t.Position	=	MathConverter.Convert( cbox.Position		);
			t.Rotation	=	MathConverter.Convert( cbox.Orientation		);
		}
	}
}