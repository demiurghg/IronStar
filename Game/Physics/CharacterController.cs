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
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;


namespace IronStar.Physics {
	public class CharacterController {

		readonly Space space;
		readonly Entity entity;

		BEPUCharacterController controller;
		Vector3		oldVelocity = Vector3.Zero;
		readonly	float heightStanding;
		readonly	float heightCrouch;
		readonly	Vector3 offsetCrouch;
		readonly	Vector3 offsetStanding;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="initialHealth"></param>
		/// <param name="maxHealth"></param>
		public CharacterController ( Entity entity, GameWorld world, float heightStand, float heightCrouch, float radius, float speedStand, float speedCrouch, float speedJump, float mass, float stepHeight )
		{
			this.entity		=	entity;
			this.space		=	world.PhysSpace;

			offsetCrouch	=	Vector3.Up * heightCrouch / 2;
			offsetStanding	=	Vector3.Up * heightStanding / 2;

			var pos = MathConverter.Convert( entity.Position + offsetStanding );

			controller = new BEPUCharacterController( pos, 
						height			:	heightStand,
						crouchingHeight	:	heightCrouch,
						standingSpeed	:	speedStand,
						crouchingSpeed	:	speedCrouch,
						jumpSpeed		:	speedJump,
						mass			:	mass
					);

			controller.StepManager.MaximumStepHeight	=	stepHeight;
			controller.Body.Tag	=	entity;
			controller.Tag		=	entity;

			space.Add( controller );
		}



		/// <summary>
		/// 
		/// </summary>
		public void Destroy ()
		{
			space.Remove( controller );
		}



		/// <summary>
		/// 
		/// </summary>
		public bool Crouching {
			get {
				return (controller.StanceManager.CurrentStance == Stance.Crouching);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="forward"></param>
		/// <param name="right"></param>
		/// <param name="up"></param>
		public void Move ( float forward, float right, float up )
		{
			var m		=	Matrix.RotationQuaternion( entity.Rotation );

			var move	=	Vector3.Zero;
			var jump	=	up >  0.5f;
			var crouch	=	up < -0.5f;

			move		+=	m.Forward * forward;
			move		+=	m.Right * right;

			if (controller==null) {
				return;
			}

			controller.HorizontalMotionConstraint.MovementDirection = new BEPUutilities.Vector2( move.X, -move.Z );
			controller.HorizontalMotionConstraint.TargetSpeed	=	8.0f;

			controller.StanceManager.DesiredStance	=	crouch ? Stance.Crouching : Stance.Standing;

			controller.TryToJump = jump;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		public void Update ()
		{
			var c = controller;
			var e = entity;

			var crouching		=	(c.StanceManager.CurrentStance==Stance.Crouching);
			var height			=	crouching ? heightCrouch : heightStanding;

			var offset			=	crouching ? offsetCrouch : offsetStanding;

			e.Position			=	MathConverter.Convert( c.Body.Position ) - offset; 
			e.LinearVelocity	=	MathConverter.Convert( c.Body.LinearVelocity );
			e.AngularVelocity	=	MathConverter.Convert( c.Body.AngularVelocity );

			if (c.SupportFinder.HasTraction) {
				e.State |= EntityState.HasTraction;
			} else {
				e.State &= ~EntityState.HasTraction;
			}

			if (c.StanceManager.CurrentStance==Stance.Crouching) {
				e.State |= EntityState.Crouching;
			} else {
				e.State &= ~EntityState.Crouching;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="kickImpulse"></param>
		/// <param name="kickPoint"></param>
		public void ApplyImpulse ( Vector3 kickImpulse, Vector3 kickPoint )
		{
			var c = controller;
			var e = entity;

			c.SupportFinder.ClearSupportData();
			var i = MathConverter.Convert( kickImpulse );
			var p = MathConverter.Convert( kickPoint );
			c.Body.ApplyImpulse( p, i );
		}




		void UpdateWalkSFX ( Entity e, float elapsedTime )
		{					
			//stepCounter -= elapsedTime;
			//if (stepCounter<=0) {
			//	stepCounter = StepRate;
			//	rlStep = !rlStep;

			//	bool step	=	e.UserCtrlFlags.HasFlag( UserCtrlFlags.Forward )
			//				|	e.UserCtrlFlags.HasFlag( UserCtrlFlags.Backward )
			//				|	e.UserCtrlFlags.HasFlag( UserCtrlFlags.StrafeLeft )
			//				|	e.UserCtrlFlags.HasFlag( UserCtrlFlags.StrafeRight );

			//	if (step && controller.SupportFinder.HasTraction) {
			//		if (rlStep) {
			//			World.SpawnFX("PlayerFootStepR", e.ID, e.Position );
			//		} else {
			//			World.SpawnFX("PlayerFootStepL", e.ID, e.Position );
			//		}
			//	}
			//}
		}



		void UpdateFallSFX ( Entity e, float elapsedTime )
		{
			//bool newTraction = controller.SupportFinder.HasTraction;
			
			//if (oldTraction!=newTraction && newTraction) {
			//	//if (((ShooterServer)World.GameServer).ShowFallings) {
			//	//	Log.Verbose("{0} falls : {1}", e.ID, oldVelocity.Y );
			//	//}

			//	if (oldVelocity.Y<-10) {
			//		//	medium landing :
			//		World.SpawnFX( "PlayerLanding", e.ID, e.Position, oldVelocity, Quaternion.Identity );
			//	} else {
			//		//	light landing :
			//		World.SpawnFX( "PlayerFootStepL", e.ID, e.Position );
			//	}
			//}

			//oldTraction = newTraction;
			//oldVelocity = MathConverter.Convert(controller.Body.LinearVelocity);
		}

	}
}
