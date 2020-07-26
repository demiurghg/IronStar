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
using IronStar.ECS;

namespace IronStar.Physics2 
{
	public class DynamicBox : Component, IMotionState
	{
		float	width;
		float	height;
		float	depth;
		float	mass;

		PhysicsEngineSystem	physics;
		Box box;

		public DynamicBox ( float width, float height, float depth, float mass )
		{
			this.width	=	width	;
			this.height	=	height	;
			this.depth	=	depth	;
			this.mass	=	mass	;
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );

			physics	=	gs.GetService<PhysicsEngineSystem>();

			var ms					=	new MotionState();

			box						=	new Box( ms, width, height, depth, mass );
			box.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			box.Tag					=	this;

			box.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;
			box.CollisionInformation.CollisionRules.Group = physics.DymamicGroup;

			physics.Space.Add( box );
		}


		public override void Removed( GameState gs )
		{
			base.Removed( gs );

			physics.Space.Remove( box );
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}


		public void Kick ( Vector3 kickImpulse, Vector3 kickPoint )
		{
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			box.ApplyImpulse( p, i );
		}


		public Vector3 Position 
		{
			get { return MathConverter.Convert( box.Position ); }
			set { box.Position = MathConverter.Convert( value ); }
		}


		public Quaternion Rotation 
		{
			get { return MathConverter.Convert( box.Orientation ); }
			set { box.Orientation = MathConverter.Convert( value ); }
		}


		public Vector3 LinearVelocity 
		{
			get { return MathConverter.Convert( box.LinearVelocity ); }
			set { box.LinearVelocity = MathConverter.Convert( value ); }
		}


		public Vector3 AngularVelocity 
		{
			get { return MathConverter.Convert( box.AngularVelocity ); }
			set { box.AngularVelocity = MathConverter.Convert( value ); }
		}

	}
}
