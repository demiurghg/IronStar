using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using IronStar.AI;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using IronStar.ECSPhysics;
using IronStar.Gameplay;
using IronStar.SFX2;
using IronStar.SFX;
using IronStar.Animation;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.Monsters.Systems
{
	class MonsterAnimator
	{
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer	composer;

		readonly Sequencer		locomotionLayer;
		readonly BlendSpaceD4	tiltForward;
		//LocomotionStateMachine		locomotionFsm;


		enum LocomotionState { Initial, Idle, Run, Jump, Landing };
		LocomotionState locomotionState = LocomotionState.Initial;
		TimeSpan locomotionTimer;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			tiltForward			=	new BlendSpaceD4( scene, null, "tilt", AnimationBlendMode.Additive );

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( tiltForward );
		}



		public void UpdateLocomotionState( GameTime gameTime, StepComponent step, UserCommandComponent uc )
		{
			var run			=	new Vector2( uc.MoveForward, uc.MoveRight ).Length() > 0.1f;
			var traction	=	step.HasTraction;
			var crossfade	=	TimeSpan.FromSeconds(0.125f);
			var landTime	=	TimeSpan.FromSeconds(0.2f);
			var timeout		=	false;
			var endAnim		=	!locomotionLayer.IsPlaying;
			
			if (locomotionTimer>TimeSpan.Zero)
			{
				locomotionTimer -= gameTime.Elapsed;
				timeout = locomotionTimer <= TimeSpan.Zero;
			}

			switch (locomotionState)
			{
			case LocomotionState.Initial:
				locomotionLayer.Sequence("idle" , SequenceMode.Immediate|SequenceMode.Looped);
				locomotionState = LocomotionState.Idle;
				break;

			case LocomotionState.Idle:
				if (run && traction) 
				{
					locomotionLayer.Sequence("run" , SequenceMode.Immediate|SequenceMode.Looped, crossfade);
					locomotionState = LocomotionState.Run;
				}
				if (!traction) 
				{
					locomotionLayer.Sequence("jump" , SequenceMode.Immediate|SequenceMode.Hold);
					locomotionState = LocomotionState.Jump;
				}
				break;

			case LocomotionState.Run:
				if (!run && traction) 
				{
					locomotionLayer.Sequence("idle" , SequenceMode.Immediate|SequenceMode.Looped, crossfade);
					locomotionState = LocomotionState.Idle;
				}
				if (!traction)
				{
					locomotionLayer.Sequence("jump" , SequenceMode.Immediate|SequenceMode.Hold);
					locomotionState = LocomotionState.Jump;
				}
				break;

			case LocomotionState.Jump:
				if (traction)
				{
					locomotionLayer.Sequence("land" , SequenceMode.Immediate);
					locomotionState = LocomotionState.Landing;
					locomotionTimer = landTime;
				}
				break;

			case LocomotionState.Landing:
				if (timeout)
				{
					locomotionLayer.Sequence("idle" , SequenceMode.Immediate, crossfade);
					locomotionState = LocomotionState.Idle;
				}
				break;
			}


		}


		public void Update ( GameTime gameTime, Matrix worldTransform, StepComponent step, UserCommandComponent uc, Matrix[] bones )
		{
			UpdateLocomotionState( gameTime, step, uc );

			var run			= step.GroundVelocity.Length()>0.2f;
			var traction	= step.HasTraction;

			var accel = Vector3.TransformNormal( step.LocalAcceleration, Matrix.Invert(worldTransform) );

			tiltForward.Weight	=	1;
			tiltForward.Factor	=	Vector2.MoveTo( tiltForward.Factor, new Vector2( uc.MoveForward, uc.MoveRight ) * (traction?1:0), gameTime.ElapsedSec * 8 );

			composer.Update( gameTime, worldTransform, false, bones );
		}
	}
}
