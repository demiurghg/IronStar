﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using IronStar.Animation;
using Fusion;
using IronStar.SFX2;
using IronStar.Gameplay.Components;
using IronStar.Gameplay.Weaponry;

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

		const string SOUND_NO_AMMO	=	"weapon/noAmmo"		;

		const string JOINT_MUZZLE	=	"muzzle"			;
		const float	 MUZZLE_SCALE	=	1f / 6f;

		string SFX_MUZZLE			=	"";


		const int MaxShakeTracks = 4;

		readonly Random rand = new Random();

		Sequencer		trackWeapon;
		Sequencer		trackBarrel;
		Sequencer		trackShake0;
		Sequencer		trackShake1;
		Sequencer		trackShake2;
		Sequencer		trackShake3;
		BlendSpace2		poseTilt;

		AnimationComposer	composer;

		Sequencer[]	shakeTracks;

		RenderModelInstance	model;
		public readonly Matrix[] Transforms = new Matrix[ RenderSystem.MaxBones ];

		WeaponState oldWeaponState = WeaponState.Overheat;
		float tiltFactor = 0;

		StepComponent	prevStep = null;

		/// <summary>
		/// 
		/// </summary>
		public WeaponAnimator ( SFX.FXPlayback fxPlayback, RenderModelInstance model )
		{
			this.model	=	model;
			composer	=	new AnimationComposer( fxPlayback, model.Scene );

			trackWeapon	=	new Sequencer( model.Scene, null, AnimationBlendMode.Override );

			trackShake0	=	new Sequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake1	=	new Sequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake2	=	new Sequencer( model.Scene, null, AnimationBlendMode.Additive );
			trackShake3	=	new Sequencer( model.Scene, null, AnimationBlendMode.Additive );

			poseTilt	=	new BlendSpace2( model.Scene, null, ANIM_TILT, AnimationBlendMode.Additive, 1, 2 );
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
		public void Update ( GameTime gameTime, Matrix cameraMatrix, WeaponStateComponent weapon, StepComponent steps, UserCommandComponent uc )
		{
			var stepEvents	=	StepComponent.DetectEvents( steps, prevStep );
			prevStep = (StepComponent)steps.Clone();

			UpdateWeaponStates(gameTime, weapon, steps, stepEvents);
			UpdateMovements(gameTime, steps, stepEvents, uc);

			composer.Update( gameTime, model.PreTransform * cameraMatrix, model.IsFPVModel, Transforms ); 
		}



		/// <summary>
		/// 
		/// </summary>
		void UpdateWeaponStates ( GameTime gameTime, WeaponStateComponent state, StepComponent steps, StepEvent stepEvents )
		{
			var weaponState	=	state.State;
			var weapon		=	Arsenal.Get( state.ActiveWeapon );

			var fireEvent		=	oldWeaponState != weaponState;
			var crossfadeFast	=	TimeSpan.FromMilliseconds( 60);
			var crossfadeSlow	=	TimeSpan.FromMilliseconds(300);
			oldWeaponState	=	weaponState;

			bool	recoil	=	fireEvent && ( weaponState == WeaponState.Cooldown || weaponState == WeaponState.Cooldown2 );
			bool	heavy	=	weapon.TimeCooldown > TimeSpan.FromMilliseconds(400);
			steps.RecoilHeavy	=	 recoil && heavy;
			steps.RecoilLight	=	 recoil && !heavy;

			if (fireEvent) 
			{
				//	recoil & cooldown :
				if ( weaponState == WeaponState.Cooldown || weaponState == WeaponState.Cooldown2 ) 
				{
					trackWeapon.Sequence( ANIM_COOLDOWN, SequenceMode.Immediate, crossfadeFast );
					//trackWeapon.Frame ++;

					var shakeName = ANIM_SHAKE + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );

					composer.SequenceFX( weapon.MuzzleFX, JOINT_MUZZLE, MUZZLE_SCALE );
				}

				//	recoil & cooldown :
				if ( weaponState == WeaponState.Throw || weaponState == WeaponState.Throw2 ) 
				{
					//	#TODO #ANIMATION -- tilt weapon a little when grenade is thrown
					//	trackWeapon.Sequence( ANIM_COOLDOWN, SequenceMode.Immediate );

					var shakeName = ANIM_SHAKE + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );

					composer.SequenceFX( Arsenal.HandGrenade.MuzzleFX, JOINT_MUZZLE, MUZZLE_SCALE );
				}

				//	idle animation :
				if ( weaponState == WeaponState.Idle ) {
					trackWeapon.Sequence( ANIM_IDLE, SequenceMode.Looped, crossfadeSlow );
				}

				//	raising
				if ( weaponState == WeaponState.Raise ) {
					trackWeapon.Sequence( ANIM_RAISE, SequenceMode.Immediate|SequenceMode.Hold, TimeSpan.Zero, TimeMode.Frames72 );
				}

				//	dropping
				if ( weaponState == WeaponState.Drop ) {
					trackWeapon.Sequence( ANIM_DROP, SequenceMode.Immediate|SequenceMode.Hold, crossfadeFast, TimeMode.Frames48 );
				}

				//	no ammo animation :
				if ( weaponState == WeaponState.NoAmmo ) 
				{
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
		void UpdateMovements ( GameTime gameTime, StepComponent steps, StepEvent stepEvents, UserCommandComponent uc )
		{
			var dt			=	gameTime.ElapsedSec;

			var fallVelocity	=	Math.Abs(steps.FallVelocity);
			var groundVelocity	=	steps.GroundVelocity;

			//	landing animation :
			if (stepEvents.HasFlag(StepEvent.Landed))
			{
				//Log.Message("{0}", oldVelocity);

				float w = MathUtil.Clamp( oldVelocity / 30.0f, 0, 0.5f );

				RunShakeAnimation( ANIM_LANDING, w );
			}

			oldVelocity = fallVelocity;

			//	jump animation :
			if (stepEvents.HasFlag(StepEvent.Jumped))
			{
				RunShakeAnimation( ANIM_JUMP, 1 );
			}

			//	tilt :
			float targetTilt	=	0;
			if ( uc.Strafe > 0 )	targetTilt++;
			if ( uc.Strafe < 0 )	targetTilt--;
			if ( uc.DYaw < 0 )		targetTilt++;
			if ( uc.DYaw > 0 )		targetTilt--;
			targetTilt = MathUtil.Clamp( targetTilt, -1, 1 );

			tiltFactor = MathUtil.Drift( tiltFactor, targetTilt, dt*2, dt*2 );

			poseTilt.Weight	=	Math.Abs( tiltFactor );
			poseTilt.Factor	=	tiltFactor * -0.5f + 0.5f;

			var stepWeight	=	Math.Min( 1, groundVelocity.Length() / 10.0f ) * 0.5f;

			//	step animation :
			if (stepEvents.HasFlag(StepEvent.LeftStep)) 
			{
				RunShakeAnimation( ANIM_WALKLEFT, stepWeight);
			}

			if (stepEvents.HasFlag(StepEvent.RightStep)) 
			{
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

			if (track!=null) 
			{
				track.Weight = weight;
				track.Sequence( name, SequenceMode.Immediate );
			}
		}
	}
}
