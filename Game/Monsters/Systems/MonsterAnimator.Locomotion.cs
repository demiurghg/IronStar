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
		const string		ANIM_TURN		=	"turn"	;
		const string		ANIM_WALK		=	"walk"	;
		const string		ANIM_RUN		=	"run"	;
		const string		ANIM_JUMP		=	"jump"	;
		const string		ANIM_LAND		=	"land"	;
		const float			YAW_THRESHOLD	=	MathUtil.PiOverFour; // 45 degrees
		const float			TURN_RATE		=	MathUtil.Pi;

		static TimeSpan	ANIM_CROSSFADE	=	TimeSpan.FromMilliseconds(125);
		static TimeSpan	LAND_TIMEOUT	=	TimeSpan.FromMilliseconds(200);

		abstract class LocomotionState
		{
			protected readonly MonsterAnimator animator;
			protected readonly Sequencer sequencer;
			protected readonly BlendSpaceD4 rotateTorso;
			protected bool allowRotation = true;
			protected float turnRate = TURN_RATE;

			float currentYaw;

			public LocomotionState( MonsterAnimator animator, UserCommandComponent uc, float yaw )
			{
				currentYaw		=	yaw;
				this.animator	=	animator;
				sequencer		=	animator.locomotionLayer;
				rotateTorso		=	animator.rotateTorso;
			}

			public LocomotionState NextState( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				if (allowRotation)
				{
					float targetYaw = uc.DesiredYaw;

					if (step.HasTraction && uc.IsMoving)
					{
						var sign  = ( uc.MoveForward < 0 ) ? -1 : 1;
						var coeff = ( uc.MoveForward !=0 ) ? 0.25f : 0.33f;
						if (uc.MoveRight>0) targetYaw -= sign * MathUtil.Pi * coeff;
						if (uc.MoveRight<0) targetYaw += sign * MathUtil.Pi * coeff;
					}

					var delta	=	MathUtil.ShortestAngle( currentYaw, targetYaw, turnRate * gameTime.ElapsedSec );
					currentYaw	+=	delta;
				}

				//	update torso rotation :
				var pitchFactor	=	MathUtil.Clamp( uc.DesiredPitch	/ MathUtil.PiOverTwo, -1f, 1f );
				var yawDelta	=	MathUtil.ShortestAngle( currentYaw, uc.DesiredYaw, MathUtil.PiOverTwo );
				var yawFactor	=	MathUtil.Clamp( -yawDelta / MathUtil.PiOverTwo, -1f, 1f );

				t.Rotation	=	Quaternion.RotationYawPitchRoll( currentYaw, 0, 0 );

				rotateTorso.Weight	=	1;
				rotateTorso.Factor	=	new Vector2( yawFactor, pitchFactor );

				return Next( gameTime, t, uc, step );
			}

			protected abstract LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step );
		}


		class Idle : LocomotionState
		{
			float baseYaw;

			public Idle( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.DesiredYaw)
			{
				allowRotation	=	false;
				baseYaw = uc.DesiredYaw;
				sequencer.Sequence(	ANIM_IDLE, SequenceMode.Looped|SequenceMode.Immediate, ANIM_CROSSFADE );
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;

				var arc		=	MathUtil.ShortestAngle( baseYaw, uc.DesiredYaw );

				if (Math.Abs(arc)>=YAW_THRESHOLD)
				{
				//	return new Turn(animator, uc, baseYaw); 
				}

				if (move && trac) return new Move(animator, uc, fwd);
				if (!trac) return new Jump(animator, uc);

				return this;
			}
		}


		class Turn : LocomotionState
		{
			TimeSpan timeout;
			float baseYaw;

			public Turn( MonsterAnimator animator, UserCommandComponent uc, float baseYaw ) : base(animator, uc, uc.DesiredYaw)
			{
				this.baseYaw	=	baseYaw;
				timeout			=	sequencer.GetTakeLength( ANIM_TURN );

				turnRate = YAW_THRESHOLD / (float)timeout.TotalSeconds;
				sequencer.Sequence(	ANIM_TURN, SequenceMode.Immediate|SequenceMode.Hold, ANIM_CROSSFADE );
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;

				if (move && trac) return new Move(animator, uc, fwd);
				if (!trac) return new Jump(animator, uc);

				if (timeout<=TimeSpan.Zero) return new Idle(animator, uc);
				timeout -= gameTime.Elapsed;
				
				return this;
			}
		}


		class Move : LocomotionState
		{
			readonly bool forward;

			public Move( MonsterAnimator animator, UserCommandComponent uc, bool forward ) : base(animator, uc, uc.DesiredYaw)
			{
				this.forward	=	forward;

				var flags =  forward ? SequenceMode.Looped|SequenceMode.Immediate : SequenceMode.Looped|SequenceMode.Immediate|SequenceMode.Reverse;

				sequencer.Sequence(	ANIM_RUN, flags, ANIM_CROSSFADE );
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;

				if (forward!=fwd) return new Move(animator, uc, fwd);
				if (!move && trac) return new Idle(animator, uc);
				if (!trac) return new Jump(animator, uc);

				return this;
			}
		}


		class Jump : LocomotionState
		{
			public Jump( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.DesiredYaw)
			{
				sequencer.Sequence("jump" , SequenceMode.Immediate|SequenceMode.Hold);
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;

				if (trac) return new Land(animator, uc);

				return this;
			}
		}


		class Land : LocomotionState
		{
			TimeSpan timeout;

			public Land( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.DesiredYaw)
			{
				sequencer.Sequence("land" , SequenceMode.Immediate|SequenceMode.Hold);
				timeout	=	LAND_TIMEOUT;
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				if (timeout<=TimeSpan.Zero) return new Idle(animator, uc);
				timeout -= gameTime.Elapsed;
				
				return this;
			}
		}
	}
}
