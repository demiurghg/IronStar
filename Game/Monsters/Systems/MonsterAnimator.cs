﻿using System;
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

		readonly TakeSequencer		locomotionLayer;
		LocomotionStateMachine		locomotionFsm;


		enum LocomotionStates { Idle, Run };
		class LocomotionStateMachine : StateMachine<LocomotionStates, StepComponent>
		{
			TakeSequencer layer;

			public LocomotionStateMachine(TakeSequencer layer) : base(LocomotionStates.Idle)
			{
				this.layer	=	layer;	
				layer.Sequence("idle", SequenceMode.Immediate|SequenceMode.Looped);
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
				if (next==LocomotionStates.Run)  layer.Sequence("run" , SequenceMode.Immediate|SequenceMode.Looped);
				if (next==LocomotionStates.Idle) layer.Sequence("idle", SequenceMode.Immediate|SequenceMode.Looped);
			}
		}


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new TakeSequencer( scene, null, AnimationBlendMode.Override );
			locomotionFsm		=	new LocomotionStateMachine( locomotionLayer );

			composer.Tracks.Add( locomotionLayer );
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, StepComponent step, Matrix[] bones )
		{
			composer.Update( gameTime, worldTransform, false, bones );
			locomotionFsm.Update( step );
		}
	}
}
