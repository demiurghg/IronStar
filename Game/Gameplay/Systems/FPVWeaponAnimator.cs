using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.Core;
using IronStar.Animation;
using Fusion;
using IronStar.SFX2;
using IronStar.Gameplay.Components;

namespace IronStar.Gameplay.Systems 
{
	public class WeaponAnimator 
	{
		const string ANIM_TILT		=	"tilt"			;
		const string ANIM_IDLE		=	"idle"			;
		const string ANIM_WARMUP	=	"warmup"		;
		const string ANIM_COOLDOWN	=	"cooldown"		;
		const string ANIM_LANDING	=	"landing"		;
		const string ANIM_JUMP		=	"jump"			;
		const string ANIM_SHAKE		=	"shake"			;
		const string ANIM_WALKLEFT	=	"step_left"		;
		const string ANIM_WALKRIGHT	=	"step_right"	;
		const string ANIM_FIRSTLOOK	=	"examine"		;
		const string ANIM_RAISE		=	"raise"			;
		const string ANIM_DROP		=	"drop"			;

		const string SOUND_LANDING	=	"player/landing"	;
		const string SOUND_STEP		=	"player/step"		;
		const string SOUND_JUMP		=	"player/jump"		;
		const string SOUND_NO_AMMO	=	"weapon/noAmmo"		;

		const string JOINT_MUZZLE	=	"muzzle"			;

		string SFX_MUZZLE			=	"";


		const int MaxShakeTracks = 4;

		readonly Random rand = new Random();

		TakeSequencer		trackWeapon;
		TakeSequencer		trackBarrel;
		TakeSequencer		trackShake0;
		TakeSequencer		trackShake1;
		TakeSequencer		trackShake2;
		TakeSequencer		trackShake3;
		AnimationPose		poseTilt;

		AnimationComposer	composer;

		TakeSequencer[]	shakeTracks;

		RenderModelInstance	model;

		WeaponState oldWeaponState = WeaponState.Overheat;
		int stepCounter = 0;
		int stepTimer = 0;
		bool stepFired = false;
		float tiltFactor = 0;

		/// <summary>
		/// 
		/// </summary>
		public WeaponAnimator ( SFX.FXPlayback fxPlayback, RenderModelInstance model )
		{
			this.model	=	model;
			composer	=	new AnimationComposer( fxPlayback, model, model.Scene );

			trackWeapon	=	new TakeSequencer( model.Scene, null, AnimationBlendMode.Override );

			trackShake0	=	new TakeSequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake1	=	new TakeSequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake2	=	new TakeSequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake3	=	new TakeSequencer( model.Scene, null, AnimationBlendMode.Additive );

			poseTilt	=	new AnimationPose( model.Scene, null, ANIM_TILT, AnimationBlendMode.Additive );
			poseTilt.Weight = 0;

			shakeTracks	=	new[] { trackShake0, trackShake1, trackShake2, trackShake3 }; 

			composer.Tracks.Add( trackWeapon );
			composer.Tracks.Add( trackShake0 );
			composer.Tracks.Add( trackShake1 );
			composer.Tracks.Add( trackShake2 );
			composer.Tracks.Add( trackShake3 );
			composer.Tracks.Add( poseTilt );

			trackWeapon.Sequence( ANIM_IDLE, SequenceMode.Immediate|SequenceMode.Looped );
		}



		/// <summary>
		/// 
		/// </summary>
		public void Update ( GameTime gameTime, WeaponComponent weapon, StepComponent steps )
		{
			UpdateWeaponStates(gameTime, weapon, steps);
			UpdateMovements(gameTime, steps);

			composer.Update( gameTime, model.FlattenTransforms ); 
			model.CommitJointTransform();
		}



		/// <summary>
		/// 
		/// </summary>
		void UpdateWeaponStates ( GameTime gameTime, WeaponComponent weapon, StepComponent steps )
		{
			var weaponState	=	weapon.State;

			var fireEvent	=	oldWeaponState != weaponState;
			oldWeaponState	=	weaponState;

			bool	recoil	=	fireEvent && ( weaponState == WeaponState.Cooldown || weaponState == WeaponState.Cooldown2 );
			bool	heavy	=	weapon.TimeCooldown > TimeSpan.FromMilliseconds(400);
			steps.RecoilHeavy	=	 recoil && heavy;
			steps.RecoilLight	=	 recoil && !heavy;

			if (fireEvent) 
			{
				Log.Message("{0}", weaponState );

				//	hide inactive weapon :
				model.Visible = weaponState!=WeaponState.Inactive;

				//	recoil & cooldown :
				if ( weaponState == WeaponState.Cooldown || weaponState == WeaponState.Cooldown2 ) 
				{

					trackWeapon.Sequence( ANIM_COOLDOWN, SequenceMode.Immediate );
					//trackWeapon.Frame ++;

					var shakeName = ANIM_SHAKE + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );

					composer.SequenceFX( weapon.MuzzleFX, JOINT_MUZZLE, 1 );
				}

				//	idle animation :
				if ( weaponState == WeaponState.Idle ) {
					trackWeapon.Sequence( ANIM_IDLE, SequenceMode.Looped );
				}

				//	raising
				if ( weaponState == WeaponState.Raise ) {
					trackWeapon.Sequence( ANIM_RAISE, SequenceMode.Immediate );
				}

				//	dropping
				if ( weaponState == WeaponState.Drop ) {
					trackWeapon.Sequence( ANIM_DROP, SequenceMode.Immediate );
				}

				//	no ammo animation :
				if ( weaponState == WeaponState.NoAmmo ) {

					composer.SequenceSound( SOUND_NO_AMMO );

					var shakeName = ANIM_SHAKE + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );
				}
			}
		}


		float oldVelocity;

		/// <summary>
		/// 
		/// </summary>
		void UpdateMovements ( GameTime gameTime, StepComponent steps )
		{
			var dt			=	gameTime.ElapsedSec;

			var fallVelocity	=	Math.Abs(steps.FallVelocity);
			var groundVelocity	=	steps.GroundVelocity;


			//	landing animation :
			if (steps.Landed) 
			{
				composer.SequenceSound( SOUND_LANDING );
				Log.Message("{0}", oldVelocity);

				float w = MathUtil.Clamp( oldVelocity / 30.0f, 0, 0.5f );

				RunShakeAnimation( ANIM_LANDING, w );
			}

			oldVelocity = fallVelocity;

			//	jump animation :
			if (steps.Jumped) 
			{
				composer.SequenceSound( SOUND_JUMP );
				RunShakeAnimation( ANIM_JUMP, 1 );
			}

			//	tilt :
			float targetTilt	=	0;
			//if (state.HasFlag(EntityState.StrafeRight)) targetTilt++;
			//if (state.HasFlag(EntityState.StrafeLeft))  targetTilt--;
			//if (state.HasFlag(EntityState.TurnRight)) targetTilt++;
			//if (state.HasFlag(EntityState.TurnLeft))  targetTilt--;
			targetTilt = MathUtil.Clamp( targetTilt, -1, 1 );

			tiltFactor = MathUtil.Drift( tiltFactor, targetTilt, dt*2, dt*2 );

			poseTilt.Weight	=	Math.Abs( tiltFactor );
			poseTilt.Frame	=	(tiltFactor > 0) ? 1 : 2;

			var stepWeight	=	Math.Min( 1, groundVelocity.Length() / 10.0f ) * 0.5f;

			//	step animation :
			if (steps.LeftStep) 
			{
				composer.SequenceSound( SOUND_STEP );
				RunShakeAnimation( ANIM_WALKLEFT, stepWeight);
			}

			if (steps.RightStep)
			{
				composer.SequenceSound( SOUND_STEP );
				RunShakeAnimation( ANIM_WALKRIGHT, stepWeight);
			}
		}
		


		/// <summary>
		/// Runs single additive animation on one of the free tracks.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="weight"></param>
		void RunShakeAnimation ( string name, float weight )
		{
			var track	=	shakeTracks.FirstOrDefault( tr => !tr.IsPlaying );

			if (track!=null) {
				track.Weight = weight;
				track.Sequence( name, SequenceMode.Immediate );
			}
		}
	}
}
