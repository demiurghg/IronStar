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
using IronStar.ECS;


namespace IronStar.Physics2 
{
	public class CharacterControllerComponent : Component
	{
		BEPUCharacterController controller;

		float	heightStanding;
		float	heightCrouching;
		float	radius;
		float	speedStanding;
		float	speedCrouching;
		float	speedJump;
		float	stepHeight;
		float	mass;
		Vector3 offsetCrouch	{ get { return Vector3.Up * heightCrouching	/ 2; } }
		Vector3 offsetStanding	{ get { return Vector3.Up * heightStanding	/ 2; } }

		PhysicsEngineSystem	physics;


		public CharacterControllerComponent ( float heightStanding, float heightCrouching, float radius, float speedStanding, float speedCrouching, float speedJump, float mass, float stepHeight )
		{
			this.heightStanding		=	heightStanding	;
			this.heightCrouching	=	heightCrouching	;
			this.radius				=	radius			;
			this.speedStanding		=	speedStanding	;
			this.speedCrouching		=	speedCrouching	;
			this.speedJump			=	speedJump		;
			this.stepHeight			=	stepHeight		;
			this.mass				=	mass			;
		}


		public override void Added( GameState gs, Entity e )
		{
			base.Added( gs, e );

			physics		=	gs.GetService<PhysicsEngineSystem>();

			var pos		=	MathConverter.Convert( e.Position + heightStanding );

			controller = new BEPUCharacterController( pos, 
						height			:	heightStanding,
						radius			:	radius,
						crouchingHeight	:	heightCrouching,
						standingSpeed	:	speedStanding,
						crouchingSpeed	:	speedCrouching,
						jumpSpeed		:	speedJump,
						mass			:	mass
					);

			controller.StepManager.MaximumStepHeight	=	stepHeight;

			controller.Body.Tag	=	this;
			controller.Tag		=	this;

			controller.Body.CollisionInformation.Events.InitialCollisionDetected +=Events_InitialCollisionDetected;
			controller.Body.CollisionInformation.CollisionRules.Group = physics.CharacterGroup;

			physics.Space.Add( controller );
		}


		public override void Removed( GameState gs, Entity e )
		{
			base.Removed( gs, e );

			physics.Space.Remove( controller );
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}


		public bool IsCrouching {
			get {
				return (controller.StanceManager.CurrentStance == Stance.Crouching);
			}
		}


		public bool HasTraction {
			get {
				return controller.SupportFinder.HasTraction;
			}
		}


		public void Teleport ( Vector3 position, Vector3 velocity )
		{
			var offset = IsCrouching ? offsetCrouch : offsetStanding;

			controller.Body.Position		=	MathConverter.Convert( position + offset );
			controller.Body.LinearVelocity	=	MathConverter.Convert( velocity );

			//	https://forum.bepuentertainment.com/viewtopic.php?f=4&t=2389
			controller.SupportFinder.ClearSupportData();
		}


		public void Move ( Vector3 move )
		{
			var jump	=	move.Y > 0.5f;
			var crouch	=	move.Y < -0.5f;

			if (controller==null) {
				return;
			}

			controller.HorizontalMotionConstraint.MovementDirection = new BEPUutilities.Vector2( move.X, -move.Z );
			controller.HorizontalMotionConstraint.TargetSpeed	=	crouch ? speedCrouching : speedStanding;

			controller.StanceManager.DesiredStance	=	crouch ? Stance.Crouching : Stance.Standing;

			controller.TryToJump = jump;
		}


		public Vector3 Position 
		{
			get { return MathConverter.Convert( controller.Body.Position ) - (IsCrouching ? offsetCrouch : offsetStanding); }
		}


		public Vector3 LinearVelocity 
		{
			get { return MathConverter.Convert( controller.Body.LinearVelocity ); }
		}


		public Vector3 AngularVelocity 
		{
			get { return MathConverter.Convert( controller.Body.AngularVelocity ); }
		}


		public void ApplyImpulse ( Vector3 kickImpulse, Vector3 kickPoint )
		{
			var c = controller;

			c.SupportFinder.ClearSupportData();
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			c.Body.ApplyImpulse( p, i );
		}
	}
}
