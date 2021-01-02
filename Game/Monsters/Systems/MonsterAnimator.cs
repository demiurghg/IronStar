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
		readonly AnimationPose	tiltForward;
		LocomotionStateMachine		locomotionFsm;


		enum LocomotionStates { Idle, Run };
		class LocomotionStateMachine : StateMachine<LocomotionStates, StepComponent>
		{
			Sequencer layer;

			public LocomotionStateMachine(Sequencer layer) : base(LocomotionStates.Idle)
			{
				this.layer	=	layer;	
				layer.Sequence("idle", SequenceMode.Immediate|SequenceMode.Looped, TimeSpan.Zero);
			}

			LocomotionStates Idle(StepComponent step)
			{
				if (step.GroundVelocity.Length()>0.2f)
				{
					return LocomotionStates.Run;
				}
				else
				{
					return LocomotionStates.Idle;
				}
			}

			LocomotionStates Run(StepComponent step)
			{
				if (step.GroundVelocity.Length()>0.2f)
				{
					return LocomotionStates.Run;
				}
				else
				{
					return LocomotionStates.Idle;
				}
			}

			protected override void Transition( LocomotionStates previous, LocomotionStates next )
			{
				var crossfade = TimeSpan.FromSeconds(0.125f);
				if (next==LocomotionStates.Run)  layer.Sequence("run" , SequenceMode.Immediate|SequenceMode.Looped, crossfade);
				if (next==LocomotionStates.Idle) layer.Sequence("idle", SequenceMode.Immediate|SequenceMode.Looped, crossfade);
			}
		}


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			locomotionFsm		=	new LocomotionStateMachine( locomotionLayer );
			tiltForward			=	new AnimationPose( scene, null, "tilt", AnimationBlendMode.Additive );
			tiltForward.Weight	=	0;
			tiltForward.Frame	=	2;

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( tiltForward );
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, StepComponent step, Matrix[] bones )
		{
			bool run = step.GroundVelocity.Length()>0.2f;

			tiltForward.Weight = MathUtil.Clamp( tiltForward.Weight + (run ? 0.1f : -0.1f), 0, 1 );

			composer.Update( gameTime, worldTransform, false, bones );
			bones[1] = Matrix.RotationY( MathUtil.DegreesToRadians(25) ) * bones[1];
			locomotionFsm.Update( step );
		}
	}
}
