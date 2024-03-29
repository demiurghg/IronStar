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
using RigidTransform = BEPUutilities.RigidTransform;
using IronStar.ECS;
using IronStar.Gameplay;
using BEPUutilities.Threading;

namespace IronStar.ECSPhysics 
{
	public class CharacterControllerSystem : ProcessingSystem<BEPUCharacterController,CharacterController,Transform>, ITransformFeeder
	{
		readonly PhysicsCore physics;

		public CharacterControllerSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override BEPUCharacterController Create( Entity e, CharacterController cc, Transform t )
		{
			var p	=	t.Position;

			var ch	=	new BEPUCharacterController( MathConverter.Convert(p + cc.offsetStanding),
							height				:	cc.height				,	
							crouchingHeight		:	cc.crouchingHeight		,
							proneHeight			:	cc.proneHeight			,
							radius				:	cc.radius				,
							margin				:	cc.margin				,
							mass				:	cc.mass					,
							maximumTractionSlope:	cc.maximumTractionSlope	,
							maximumSupportSlope	:	cc.maximumSupportSlope	,
							standingSpeed		:	cc.standingSpeed		,
							crouchingSpeed		:	cc.crouchingSpeed		,
							proneSpeed			:	cc.proneSpeed			,
							tractionForce		:	cc.tractionForce		,
							slidingSpeed		:	cc.slidingSpeed			,
							slidingForce		:	cc.slidingForce			,
							airSpeed			:	cc.airSpeed				,
							airForce			:	cc.airForce				,
							jumpSpeed			:	cc.jumpSpeed			,
							slidingJumpSpeed	:	cc.slidingJumpSpeed		,
							maximumGlueForce	:	cc.maximumGlueForce
						);

			ch.StepManager.MaximumStepHeight	=	cc.stepHeight;

			ch.Tag			=	e;
			ch.Body.Tag		=	e;

			physics.Add( ch );

			ch.Body.CollisionInformation.Events.InitialCollisionDetected +=Events_InitialCollisionDetected;
			ch.Body.CollisionInformation.CollisionRules.Group = physics.CharacterGroup;

			return ch;
		}


		private void Events_InitialCollisionDetected( EntityCollidable sender, Collidable other, CollidablePairHandler pair )
		{
			physics.HandleTouch( pair );
		}


		protected override void Destroy( Entity e, BEPUCharacterController ch )
		{
			physics.Remove(ch); 
		}


		protected override void Process( Entity e, GameTime gameTime, BEPUCharacterController controller, CharacterController cc, Transform t )
		{
			var uc	=	e.GetComponent<UserCommandComponent>();

			if (uc!=null)
			{
				Move( controller, cc, uc );
			}
		}


		BEPUutilities.Vector2 moveVectorDebug = new BEPUutilities.Vector2(0,0);
		float yawDebug = 0;
		float dYawDebug = 0;

		void Move ( BEPUCharacterController controller, CharacterController cc, UserCommandComponent uc )
		{
			var jump		=	uc.Action.HasFlag( UserAction.Jump );
			var crouch		=	uc.Action.HasFlag( UserAction.Crouch );
			var moveVector	=	uc.MovementVector;

			if (jump && crouch) 
			{
				jump = crouch = false;
			}

			var moveDir		=	new BEPUutilities.Vector2( moveVector.X, -moveVector.Z );
			var velScale	=	moveDir.Length();

			moveVectorDebug	=	moveDir;
			yawDebug		=	uc.Yaw;
			dYawDebug		=	uc.DYaw;

			var standingSpeed	=	cc.standingSpeed * velScale;
			var crouchingSpeed	=	cc.crouchingSpeed * velScale;
			var proneSpeed		=	cc.proneSpeed * velScale;

			controller.StandingSpeed	=	standingSpeed	;
			controller.CrouchingSpeed	=	crouchingSpeed	;
			controller.ProneSpeed		=	proneSpeed		;

			controller.HorizontalMotionConstraint.MovementDirection	=	moveDir;

			controller.StanceManager.DesiredStance	=	crouch ? Stance.Crouching : Stance.Standing;

			controller.TryToJump = jump;
		}


		public void FeedTransform( IGameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, FeedEntityTransform );
		}


		void FeedEntityTransform( Entity e, GameTime gameTime, BEPUCharacterController controller, CharacterController cc, Transform t )
		{
			var crouching	=	controller.StanceManager.CurrentStance == Stance.Crouching;
			var traction	=	controller.SupportFinder.HasTraction;
			var offset		=	crouching ? cc.offsetCrouch : cc.offsetStanding;
			var position	=	MathConverter.Convert( controller.Body.Position );
			var velocity	=	MathConverter.Convert( controller.Body.LinearVelocity );

			t.Move( position - offset, t.Rotation, velocity, Vector3.Zero );

			cc.IsCrouching	=	crouching;
			cc.HasTraction	=	traction;
		}
	}
}
