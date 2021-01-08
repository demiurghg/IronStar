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
	partial class MonsterAnimator
	{
		const string		ANIM_IDLE		=	"idle"	;
		const string		ANIM_WALK		=	"walk"	;
		const string		ANIM_RUN		=	"run"	;
		const string		ANIM_JUMP		=	"jump"	;
		const string		ANIM_LAND		=	"land"	;

		static TimeSpan	ANIM_CROSSFADE	=	TimeSpan.FromMilliseconds(125);
		static TimeSpan	LAND_TIMEOUT	=	TimeSpan.FromMilliseconds(200);

		abstract class LocomotionState2
		{
			protected readonly MonsterAnimator animator;
			protected readonly Sequencer sequencer;

			public LocomotionState2( MonsterAnimator animator )
			{
				this.animator	=	animator;
				sequencer		=	animator.locomotionLayer;
			}

			public abstract LocomotionState2 Next( GameTime gameTime, UserCommandComponent uc, StepComponent step );
		}


		class Idle : LocomotionState2
		{
			public Idle( MonsterAnimator animator ) : base(animator)
			{
				sequencer.Sequence(	ANIM_IDLE, SequenceMode.Looped|SequenceMode.Immediate, ANIM_CROSSFADE );
			}

			public override LocomotionState2 Next( GameTime gameTime, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;

				if (move && trac) return new Move(animator, fwd);
				if (!trac) return new Jump(animator);

				return this;
			}
		}


		class Move : LocomotionState2
		{
			readonly bool forward;

			public Move( MonsterAnimator animator, bool forward ) : base(animator)
			{
				this.forward	=	forward;

				var flags =  forward ? SequenceMode.Looped|SequenceMode.Immediate : SequenceMode.Looped|SequenceMode.Immediate|SequenceMode.Reverse;

				sequencer.Sequence(	ANIM_RUN, flags, ANIM_CROSSFADE );
			}

			public override LocomotionState2 Next( GameTime gameTime, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;

				if (forward!=fwd) return new Move(animator, fwd);
				if (!move && trac) return new Idle(animator);
				if (!trac) return new Jump(animator);

				return this;
			}
		}


		class Jump : LocomotionState2
		{
			public Jump( MonsterAnimator animator ) : base(animator)
			{
				sequencer.Sequence("jump" , SequenceMode.Immediate|SequenceMode.Hold);
			}

			public override LocomotionState2 Next( GameTime gameTime, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;

				if (trac) return new Land(animator);

				return this;
			}
		}


		class Land : LocomotionState2
		{
			TimeSpan timeout;

			public Land( MonsterAnimator animator ) : base(animator)
			{
				sequencer.Sequence("land" , SequenceMode.Immediate);
				timeout	=	LAND_TIMEOUT;
			}

			public override LocomotionState2 Next( GameTime gameTime, UserCommandComponent uc, StepComponent step )
			{
				if (timeout<=TimeSpan.Zero) return new Idle(animator);
				timeout -= gameTime.Elapsed;
				
				return this;
			}
		}
	}
}
