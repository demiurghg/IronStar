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
	public class DynamicBox : Component
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

			var ms			=	new MotionState();
			ms.Orientation	=	MathConverter.Convert( entity.Rotation );
			ms.Position		=	MathConverter.Convert( entity.Position );

			box						=	new Box( ms, width, height, depth, mass );
			box.PositionUpdateMode	=	PositionUpdateMode.Continuous;
			box.Tag					=	this;

			box.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected;
			box.CollisionInformation.CollisionRules.Group = physics.DymamicGroup;

			physics.Space.Add( box );
		}


		public override void Removed( GameState gs, Entity entity )
		{
			base.Removed( gs, entity );

			physics.Space.Remove( box );
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}


		public void Teleport ( Vector3 position, Quaternion orient )
		{
			box.Position		=	MathConverter.Convert( position );
			box.Orientation		=	MathConverter.Convert( orient );
			box.AngularVelocity	=	MathConverter.Convert( Vector3.Zero );
			box.LinearVelocity	=	MathConverter.Convert( Vector3.Zero );
		}


		public void Kick ( Vector3 kickImpulse, Vector3 kickPoint )
		{
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			box.ApplyImpulse( p, i );
		}


		public Vector3 Position {
			get {
				return MathConverter.Convert( box.Position );
			}
		}


		public Quaternion Orientation {
			get {
				return MathConverter.Convert( box.Orientation );
			}
		}


		public Vector3 LinearVelocity {
			get {
				return MathConverter.Convert( box.LinearVelocity );
			}
		}


		public Vector3 AngularVelocity {
			get {
				return MathConverter.Convert( box.AngularVelocity );
			}
		}

	}
}
