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


		enum LocomotionState { Initial, Idle, Run };
		LocomotionState locomotionState = LocomotionState.Initial;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			tiltForward			=	new BlendSpaceD4( scene, "spine1", "rotation", AnimationBlendMode.Additive );

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( tiltForward );
		}



		public void UpdateLocomotionState( GameTime gameTime, StepComponent step, UserCommandComponent uc )
		{
			bool run		=	new Vector2( uc.MoveForward, uc.MoveRight ).Length() > 0.1f;
			var crossfade	=	TimeSpan.FromSeconds(0.125f);

			switch (locomotionState)
			{
			case LocomotionState.Initial:
				locomotionLayer.Sequence("idle" , SequenceMode.Immediate|SequenceMode.Looped);
				locomotionState = LocomotionState.Idle;
				break;

			case LocomotionState.Idle:
				if (run) 
				{
					locomotionLayer.Sequence("run" , SequenceMode.Immediate|SequenceMode.Looped, crossfade);
					locomotionState = LocomotionState.Run;
				}
				break;

			case LocomotionState.Run:
				if (!run) 
				{
					locomotionLayer.Sequence("idle" , SequenceMode.Immediate|SequenceMode.Looped, crossfade);
					locomotionState = LocomotionState.Idle;
				}
				break;
			}
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, StepComponent step, UserCommandComponent uc, Matrix[] bones )
		{
			UpdateLocomotionState( gameTime, step, uc );

			bool run = step.GroundVelocity.Length()>0.2f;

			var accel = Vector3.TransformNormal( step.LocalAcceleration, Matrix.Invert(worldTransform) );

			tiltForward.Weight	=	1;
			tiltForward.Factor	=	Vector2.MoveTo( tiltForward.Factor, new Vector2( uc.MoveForward, uc.MoveRight ), gameTime.ElapsedSec * 8 );

			composer.Update( gameTime, worldTransform, false, bones );
		}
	}
}
