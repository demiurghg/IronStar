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
using IronStar.Gameplay;

namespace IronStar.ECSPhysics 
{
	public class CharacterControllerSystem : ProcessingSystem<BEPUCharacterController,CharacterController,Transform,Velocity>, ITransformFeeder
	{
		readonly PhysicsCore physics;

		public CharacterControllerSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}


		protected override BEPUCharacterController Create( Entity e, CharacterController cc, Transform t, Velocity v )
		{
			var p	=	t.Position;

			var ch	=	new BEPUCharacterController( MathConverter.Convert(p + cc.offsetStanding*0.5f),
							height			:	cc.heightStanding,
							radius			:	cc.radius,
							crouchingHeight	:	cc.heightCrouching,
							standingSpeed	:	cc.speedStanding,
							crouchingSpeed	:	cc.speedCrouching,
							jumpSpeed		:	cc.speedJump,
							mass			:	cc.mass
						);

			ch.Tag			=	e;
			ch.Body.Tag		=	e;

			physics.Space.Add( ch );

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
			physics.Space.Remove(ch); 
		}


		public void FeedTransform( GameState gs )
		{
			ForEach( gs, GameTime.Zero, FeedControllerData );
		}

		
		protected void FeedControllerData( Entity e, GameTime gameTime, BEPUCharacterController controller, CharacterController cc, Transform t, Velocity v )
		{
			var crouching	=	controller.StanceManager.CurrentStance == Stance.Crouching;
			var traction	=	controller.SupportFinder.HasTraction;
			var offset		=	crouching ? cc.offsetCrouch : cc.offsetStanding;
			var position	=	MathConverter.Convert( controller.Body.Position ) - offset;
			var uc			=	e.GetComponent<UserCommandComponent>();

			cc.IsCrouching	=	crouching;
			cc.HasTraction	=	traction;
			
			if (uc!=null)
			{
				t.Rotation	=	Quaternion.RotationYawPitchRoll( uc.Yaw, 0, 0 );
			}
			
			t.Position	=	position;
			v.Linear	=	MathConverter.Convert( controller.Body.LinearVelocity );
			v.Angular	=	Vector3.Zero;
		}


		protected override void Process( Entity e, GameTime gameTime, BEPUCharacterController controller, CharacterController cc, Transform t, Velocity v )
		{
			var uc	=	e.GetComponent<UserCommandComponent>();

			if (uc!=null)
			{
				Move( controller, cc, uc.MovementVector );
			}
		}


		void Move ( BEPUCharacterController controller, CharacterController cc, Vector3 move )
		{
			var jump	=	move.Y > 0.5f;
			var crouch	=	move.Y < -0.5f;

			controller.HorizontalMotionConstraint.MovementDirection = new BEPUutilities.Vector2( move.X, -move.Z );
			controller.HorizontalMotionConstraint.TargetSpeed	=	crouch ? cc.speedCrouching : cc.speedStanding;

			controller.StanceManager.DesiredStance	=	crouch ? Stance.Crouching : Stance.Standing;

			controller.TryToJump = jump;
		}
	}
}
