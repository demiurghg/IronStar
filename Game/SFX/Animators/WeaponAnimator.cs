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
using Fusion;

namespace IronStar.SFX {
	public class WeaponAnimator : Animator {

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

		string SFX_MUZZLE { get { return factory.MuzzleFX; } }


		const int MaxShakeTracks = 4;

		readonly WeaponAnimatorFactory	factory;

		readonly Random rand = new Random();

		AnimationTrack		trackWeapon;
		AnimationTrack		trackBarrel;
		AnimationTrack		trackShake0;
		AnimationTrack		trackShake1;
		AnimationTrack		trackShake2;
		AnimationTrack		trackShake3;
		AnimationPose		poseTilt;


		AnimationTrack[]	shakeTracks;

		bool weaponEvent;
		WeaponState oldWeaponState;
		bool oldTraction = true;
		int stepCounter = 0;
		int stepTimer = 0;
		bool stepFired = false;
		float tiltFactor = 0;

		/// <summary>
		/// 
		/// </summary>
		public WeaponAnimator ( GameWorld world, Entity entity, ModelInstance model, WeaponAnimatorFactory factory ) : base(world, entity,model)
		{
			this.factory	=	factory;

			trackWeapon	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Override );

			trackShake0	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake1	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake2	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );
			trackShake3	=	new AnimationTrack( model.Scene, null, AnimationBlendMode.Additive );

			poseTilt	=	new AnimationPose( model.Scene, null, ANIM_TILT, AnimationBlendMode.Additive );
			poseTilt.Weight = 0;

			shakeTracks	=	new[] { trackShake0, trackShake1, trackShake2, trackShake3 }; 

			composer.Tracks.Add( trackWeapon );
			composer.Tracks.Add( trackShake0 );
			composer.Tracks.Add( trackShake1 );
			composer.Tracks.Add( trackShake2 );
			composer.Tracks.Add( trackShake3 );
			composer.Tracks.Add( poseTilt );

			trackWeapon.Sequence( ANIM_IDLE, true, true );
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

				if ( weaponState == WeaponState.Cooldown || weaponState == WeaponState.Cooldown2 ) {

					trackWeapon.Sequence( ANIM_COOLDOWN, true, false );
					//trackWeapon.Frame ++;

					var shakeName = ANIM_SHAKE + rand.Next(6).ToString();
					var shakeAmpl = Math.Abs(rand.GaussDistribution(0,0.5f));
					RunShakeAnimation( shakeName, shakeAmpl );

					composer.SequenceFX( SFX_MUZZLE, JOINT_MUZZLE, factory.MuzzleFXScale );
				}


				if ( weaponState == WeaponState.Idle ) {
					trackWeapon.Sequence( ANIM_IDLE, false, true );
				}


				if ( weaponState == WeaponState.Raise ) {
					trackWeapon.Sequence( ANIM_RAISE, true, false );
				}


				if ( weaponState == WeaponState.Drop ) {
					trackWeapon.Sequence( ANIM_DROP, true, false );
				}


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
		void UpdateMovements ( GameTime gameTime )
		{
			var dt			=	gameTime.ElapsedSec;
			var state		=	entity.EntityState;

			var newTraction		=	state.HasFlag( EntityState.HasTraction );
			var fallVelocity	=	Math.Abs( entity.LinearVelocity.Y );
			var groundVelocity	=	new Vector3( entity.LinearVelocity.X, 0, entity.LinearVelocity.Z );


			//	landing animation :
			if (oldTraction!=newTraction && newTraction) {
				composer.SequenceSound( SOUND_LANDING );
				Log.Message("{0}", oldVelocity);

				float w = MathUtil.Clamp( oldVelocity / 20.0f, 0, 1 );

				RunShakeAnimation( ANIM_LANDING, w );
			}

			oldVelocity = fallVelocity;

			//	jump animation :
			if (oldTraction!=newTraction && !newTraction) {
				composer.SequenceSound( SOUND_JUMP );
				RunShakeAnimation( ANIM_JUMP, 1 );
			}

			//	tilt :
			float targetTilt	=	0;
			if (state.HasFlag(EntityState.StrafeRight)) targetTilt++;
			if (state.HasFlag(EntityState.StrafeLeft))  targetTilt--;
			if (state.HasFlag(EntityState.TurnRight)) targetTilt++;
			if (state.HasFlag(EntityState.TurnLeft))  targetTilt--;
			targetTilt = MathUtil.Clamp( targetTilt, -1, 1 );

			tiltFactor = MathUtil.Drift( tiltFactor, targetTilt, dt*2, dt*2 );

			poseTilt.Weight	=	Math.Abs( tiltFactor );
			poseTilt.Frame	=	(tiltFactor > 0) ? 1 : 2;

			//	step animation :
			if (newTraction && groundVelocity.Length() > 0.1f ) {

				stepTimer += gameTime.Milliseconds;

				var weight	=	Math.Min( 1, groundVelocity.Length() / 10.0f ) * 0.5f;

				if (stepTimer > 50 && !stepFired) {
					stepCounter++;

					stepFired = true;

					composer.SequenceSound( SOUND_STEP );

					if ((stepCounter & 1) == 0) {
						RunShakeAnimation( ANIM_WALKRIGHT, weight);
					} else {
						RunShakeAnimation( ANIM_WALKLEFT, weight);
					}
				}

				if (stepTimer>300) {
					stepFired = false;
					stepTimer = 0;
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
