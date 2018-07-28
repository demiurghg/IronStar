﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion;

namespace IronStar.SFX {
	public class WeaponAnimator : Animator {

		const int MaxShakeTracks = 4;

		Random rand = new Random();

		AnimationTrack		trackWeapon;
		AnimationTrack		trackBarrel;
		AnimationTrack		trackShake0;
		AnimationTrack		trackShake1;
		AnimationTrack		trackShake2;
		AnimationTrack		trackShake3;
		AnimationTrack		trackTilt;

		AnimationTrack[]	shakeTracks;

		bool weaponEvent;
		WeaponState oldWeaponState;
		bool oldTraction = true;
		int stepCounter = 0;
		int stepTimer = 0;
		float tiltFactor = 0;

		/// <summary>
		/// 
		/// </summary>
		public WeaponAnimator ( GameWorld world, Entity entity, ModelInstance model ) : base(world, entity,model)
		{
			trackWeapon	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Override );

			trackShake0	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake1	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake2	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake3	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );

			trackTilt	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackTilt.TimeScale = 0;
			trackTilt.Weight = 0;

			shakeTracks	=	new[] { trackShake0, trackShake1, trackShake2, trackShake3 }; 

			composer.Tracks.Add( trackWeapon );
			composer.Tracks.Add( trackShake0 );
			composer.Tracks.Add( trackShake1 );
			composer.Tracks.Add( trackShake2 );
			composer.Tracks.Add( trackShake3 );
			composer.Tracks.Add( trackTilt );

			trackWeapon.Sequence( "anim_idle", true, true );
			trackTilt.Sequence("anim_tilt", true, true );
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Update ( GameTime gameTime, Matrix[] destination )
		{
			UpdateWeaponStates(gameTime);
			UpdateMovements(gameTime);

			composer.Update( gameTime, destination ); 
		}



		/// <summary>
		/// 
		/// </summary>
		void UpdateWeaponStates (GameTime gameTime)
		{
			var weaponState	=	entity.WeaponState;

			var fireEvent	=	oldWeaponState != weaponState;
			oldWeaponState	=	weaponState;

			if (fireEvent) {

				Log.Message("{0}", entity.WeaponState );

				if ( weaponState == WeaponState.Cooldown ) {

					trackWeapon.Sequence( "anim_recoil", true, false );
					trackWeapon.Frame ++;

					var shakeName = "anim_shake" + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );

					composer.SequenceFX( "machinegunMuzzle", "muzzle" );
					//composer.SequenceFX( "testTracer", "muzzle" );
				}


				if ( weaponState == WeaponState.Idle ) {
					trackWeapon.Sequence( "anim_idle", false, true );
				}


				if ( weaponState == WeaponState.Raise ) {
					trackWeapon.Sequence( "anim_takeout", true, false );
				}


				if ( weaponState == WeaponState.Drop ) {
					trackWeapon.Sequence( "anim_putdown", true, false );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void UpdateMovements ( GameTime gameTime )
		{
			var dt			=	gameTime.ElapsedSec;
			var state		=	entity.EntityState;

			var newTraction	=	state.HasFlag( EntityState.HasTraction );

			//	landing animation :
			if (oldTraction!=newTraction && newTraction) {
				RunShakeAnimation("anim_landing", 0.5f);
			}

			//	tilt :
			float targetTilt	=	0;
			if (state.HasFlag(EntityState.StrafeRight)) targetTilt++;
			if (state.HasFlag(EntityState.StrafeLeft))  targetTilt--;
			if (state.HasFlag(EntityState.TurnRight)) targetTilt++;
			if (state.HasFlag(EntityState.TurnLeft))  targetTilt--;
			targetTilt = MathUtil.Clamp( targetTilt, -1, 1 );

			tiltFactor = MathUtil.Drift( tiltFactor, targetTilt, dt*2, dt*2 );

			trackTilt.Weight	=	Math.Abs( tiltFactor );
			trackTilt.Frame		=	(tiltFactor > 0) ? 1 : 2;

			//	step animation :
			var groundVelocity = new Vector3( entity.LinearVelocity.X, 0, entity.LinearVelocity.Z );

			if (newTraction && groundVelocity.Length() > 1 ) {

				stepTimer += gameTime.Milliseconds;

				var weight	=	Math.Min( 1, groundVelocity.Length() / 10.0f ) * 0.5f;

				if (stepTimer > 400) {
					stepTimer = 0;
					stepCounter++;

					if ((stepCounter & 1) == 0) {
						RunShakeAnimation("anim_walk_right", weight);
					} else {
						RunShakeAnimation("anim_walk_left", weight);
					}

				}

			} else {
				stepTimer = 0;
			}

			oldTraction = newTraction;
		}
		


		/// <summary>
		/// Runs single additive animation on one of the free tracks.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="weight"></param>
		void RunShakeAnimation ( string name, float weight )
		{
			var track	=	shakeTracks.FirstOrDefault( tr => !tr.Busy );

			if (track!=null) {
				track.Weight = weight;
				track.Sequence( name, true, false );
			}
		}
	}
}