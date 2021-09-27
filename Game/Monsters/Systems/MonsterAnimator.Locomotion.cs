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
		const string		ANIM_CR_IDLE	=	"crouch_idle"	;
		const string		ANIM_CR_WALK	=	"crouch_walk"	;

		const string		ANIM_IDLE		=	"idle"	;
		const string		ANIM_TURN		=	"turn"	;
		const string		ANIM_WALK		=	"walk"	;
		const string		ANIM_RUN		=	"run"	;
		const string		ANIM_JUMP		=	"jump"	;
		const string		ANIM_LAND		=	"land"	;
		const string		ANIM_DEATH		=	"death1";
		const float			YAW_THRESHOLD	=	MathUtil.PiOverFour; // 45 degrees
		const float			TURN_RATE		=	MathUtil.Pi * 2;

		static TimeSpan	ANIM_CROSSFADE	=	TimeSpan.FromMilliseconds(200);

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

			public LocomotionState NextState( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step, bool dead )
			{
				if (allowRotation)
				{
					float targetYaw = uc.Yaw;

					if (step.HasTraction && uc.IsMoving)
					{
						var sign  = ( uc.Move < 0 ) ? -1 : 1;
						var coeff = ( uc.Move !=0 ) ? 0.25f : 0.33f;
						if (uc.Strafe>0) targetYaw -= sign * MathUtil.Pi * coeff;
						if (uc.Strafe<0) targetYaw += sign * MathUtil.Pi * coeff;
					}

					var delta	=	MathUtil.ShortestAngle( currentYaw, targetYaw, turnRate * gameTime.ElapsedSec );
					currentYaw	+=	delta;
				}

				//	update torso rotation :
				var pitchFactor	=	MathUtil.Clamp( uc.Pitch	/ MathUtil.PiOverTwo, -1f, 1f );
				var yawDelta	=	MathUtil.ShortestAngle( currentYaw, uc.Yaw, MathUtil.PiOverTwo );
				var yawFactor	=	MathUtil.Clamp( -yawDelta / MathUtil.PiOverTwo, -1f, 1f );

				var rotation	=	Quaternion.RotationYawPitchRoll( currentYaw, 0, 0 );
				t.Move( t.Position, rotation, t.LinearVelocity, t.AngularVelocity );

				rotateTorso.Weight	=	1;
				rotateTorso.Factor	=	new Vector2( yawFactor, pitchFactor );

				if (dead && GetType()!=typeof(Dead))
				{
					return new Dead(animator, uc);
				}

				return Next( gameTime, t, uc, step );
			}

			protected abstract LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step );
		}


		class Idle : LocomotionState
		{
			float baseYaw;
			readonly bool crouch;

			public Idle( MonsterAnimator animator, UserCommandComponent uc, bool crouch ) : base(animator, uc, uc.Yaw)
			{
				this.crouch		=	crouch;
				allowRotation	=	false;
				baseYaw = uc.Yaw;
				sequencer.Sequence(	crouch ? ANIM_CR_IDLE : ANIM_IDLE, SequenceMode.Looped|SequenceMode.Immediate, ANIM_CROSSFADE );
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;
				var cr		=	step.IsCrouching;
				var run		=	uc.IsRunning;

				var arc		=	MathUtil.ShortestAngle( baseYaw, uc.Yaw );

				if (Math.Abs(arc)>=YAW_THRESHOLD)
				{
				//	return new Turn(animator, uc, baseYaw); 
				}

				if (crouch!=cr) return new Idle(animator, uc, cr);
				if (move && trac) return new Move(animator, uc, run, fwd, cr);
				if (!trac) return new Jump(animator, uc);

				return this;
			}
		}


		class Turn : LocomotionState
		{
			TimeSpan timeout;
			float baseYaw;

			public Turn( MonsterAnimator animator, UserCommandComponent uc, float baseYaw ) : base(animator, uc, uc.Yaw)
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
				var cr		=	step.IsCrouching;
				var run		=	uc.IsRunning;

				if (move && trac) return new Move(animator, uc, run, fwd, cr);
				if (!trac) return new Jump(animator, uc);

				if (timeout<=TimeSpan.Zero) return new Idle(animator, uc, cr);
				timeout -= gameTime.Elapsed;
				
				return this;
			}
		}


		class Move : LocomotionState
		{
			readonly bool forward;
			readonly bool crouch;
			readonly bool run;

			public Move( MonsterAnimator animator, UserCommandComponent uc, bool run, bool forward, bool crouch ) : base(animator, uc, uc.Yaw)
			{
				this.forward	=	forward;
				this.crouch		=	crouch;
				this.run		=	!crouch && run;

				var flags =  forward ? SequenceMode.Looped|SequenceMode.Immediate : SequenceMode.Looped|SequenceMode.Immediate|SequenceMode.Reverse;

				var animName	=	crouch ? ANIM_CR_WALK : ( run ? ANIM_RUN : ANIM_WALK );

				sequencer.Sequence(	animName, flags, ANIM_CROSSFADE );
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				var move	=	uc.IsMoving;
				var trac	=	step.HasTraction;
				var fwd		=	uc.IsForward;
				var cr		=	step.IsCrouching;
				var run		=	!cr && uc.IsRunning;

				if (forward!=fwd || crouch!=cr || this.run!=run) return new Move(animator, uc, run, fwd, cr);
				if (!move && trac) return new Idle(animator, uc, cr);
				if (!trac) return new Jump(animator, uc);

				return this;
			}
		}


		class Jump : LocomotionState
		{
			public Jump( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.Yaw)
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

			public Land( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.Yaw)
			{
				timeout	=	sequencer.GetTakeLength(ANIM_LAND);
				sequencer.Sequence(ANIM_LAND , SequenceMode.Immediate|SequenceMode.Hold, TimeSpan.FromMilliseconds(100));
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				if (timeout<=TimeSpan.Zero) return new Idle(animator, uc, step.IsCrouching);
				timeout -= gameTime.Elapsed;
				
				return this;
			}
		}


		class Dead : LocomotionState
		{
			public Dead( MonsterAnimator animator, UserCommandComponent uc ) : base(animator, uc, uc.Yaw)
			{
				sequencer.Sequence(ANIM_DEATH , SequenceMode.Immediate|SequenceMode.Hold, TimeSpan.FromMilliseconds(100));
			}

			protected override LocomotionState Next( GameTime gameTime, Transform t, UserCommandComponent uc, StepComponent step )
			{
				return this;
			}
		}
	}
}
