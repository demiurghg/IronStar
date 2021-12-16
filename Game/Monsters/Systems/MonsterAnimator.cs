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
using IronStar.Gameplay.Weaponry;

namespace IronStar.Monsters.Systems
{
	partial class MonsterAnimator
	{
		readonly Entity monsterEntity;
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer	composer;

		readonly Sequencer		locomotionLayer;
		readonly BlendSpaceD4	tiltForward;
		readonly BlendSpaceD4	rotateTorso;
		readonly Sequencer		torsoLayer;
		LocomotionState		locomotionState;
		WeaponState			prevWeaponState = WeaponState.Idle;

		public readonly Scene Scene;

		float	baseYaw;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Entity e, Scene scene, PhysicsCore physics, UserCommandComponent uc )
		{								
			this.monsterEntity	=	e;
			this.fxPlayback		=	fxPlayback;
			this.physics		=	physics;
			this.Scene			=	scene;

			baseYaw				=	uc.Yaw;

			composer			=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			tiltForward			=	new BlendSpaceD4( scene, null, "tilt", AnimationBlendMode.Additive );
			rotateTorso			=	new BlendSpaceD4( scene, null, "rotation", AnimationBlendMode.Additive );
			torsoLayer			=	new Sequencer( scene, "spine1", AnimationBlendMode.Override );
			locomotionState		=	new Idle(this, uc, false);

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( torsoLayer );
			composer.Tracks.Add( rotateTorso );
			composer.Tracks.Add( tiltForward );
		}


		public void UpdateWeaponState()
		{
			var e				=	monsterEntity;
			var inventory		=	e.GetComponent<InventoryComponent>();
			var weaponState		=	e.GetComponent<WeaponStateComponent>();
			var weapon			=	Arsenal.Get( weaponState.ActiveWeapon );
			var crossfade		=	TimeSpan.FromMilliseconds(50);

			if (weapon!=null)
			{
				if (weaponState.State!=prevWeaponState)
				{
					prevWeaponState	=	weaponState.State;

					if (weaponState.State==WeaponState.Cooldown || weaponState.State==WeaponState.Cooldown2)
					{
						torsoLayer.Sequence("attack", SequenceMode.Immediate|SequenceMode.Hold, crossfade );
					}
					else if (weaponState.State==WeaponState.Drop)
					{
						torsoLayer.Sequence("drop", SequenceMode.Immediate|SequenceMode.Hold, crossfade);
					}
					else if (weaponState.State==WeaponState.Raise)
					{
						torsoLayer.Sequence("raise", SequenceMode.Immediate|SequenceMode.Hold, crossfade);
					}
					else
					{
						torsoLayer.Sequence("aim", SequenceMode.Immediate|SequenceMode.Hold, crossfade);
					}
				}
			}
			
		}



		public void UpdateLocomotionState( GameTime gameTime, Transform t, StepComponent step, UserCommandComponent uc, HealthComponent health )
		{
			var dead  = health==null ? false : health.Health<=0;
			locomotionState	=	locomotionState.NextState( gameTime, t, uc, step, dead );
		}


		Vector2 tiltFactor = Vector2.Zero;

		public void Update ( GameTime gameTime, Transform transform, StepComponent step, UserCommandComponent uc, Matrix[] bones )
		{
			var health		=	monsterEntity.GetComponent<HealthComponent>();

			UpdateLocomotionState( gameTime, transform, step, uc, health );
			UpdateWeaponState();

			//	update tilt :
			var run			= step.GroundVelocity.Length()>0.2f;
			var traction	= step.HasTraction;
			var tiltVel		= (traction ? 8 : 2) * gameTime.ElapsedSec;

			var accel = Vector3.TransformNormal( step.LocalAcceleration, Matrix.Invert(uc.RotationMatrix) );

			tiltForward.Weight	=	1;
			tiltFactor			=	Vector2.MoveTo( tiltFactor, new Vector2( uc.Move, uc.Strafe ) * ((traction && !step.IsCrouching)?1:0), tiltVel );
			tiltForward.Factor	=	new Vector2( SignedSmoothStep( tiltFactor.X ), SignedSmoothStep( tiltFactor.Y ) );

			//	update composer :
			composer.Update( gameTime, transform.TransformMatrix, false, bones );
		}

		float SignedSmoothStep( float x )
		{
			return Math.Sign(x) * AnimationUtils.SmoothStep( Math.Abs(x) );
		}
	}
}
