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
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer	composer;

		readonly Sequencer		locomotionLayer;
		readonly BlendSpaceD4	tiltForward;
		readonly BlendSpaceD4	rotateTorso;
		LocomotionState		locomotionState;

		float	baseYaw;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics, UserCommandComponent uc )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			baseYaw			=	uc.DesiredYaw;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			tiltForward			=	new BlendSpaceD4( scene, null, "tilt", AnimationBlendMode.Additive );
			rotateTorso			=	new BlendSpaceD4( scene, null, "rotation", AnimationBlendMode.Additive );
			locomotionState		=	new Idle(this, uc);

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( rotateTorso );
			composer.Tracks.Add( tiltForward );
		}



		public void UpdateLocomotionState( GameTime gameTime, Transform t, StepComponent step, UserCommandComponent uc )
		{
			locomotionState	=	locomotionState.NextState( gameTime, t, uc, step );
		}


		public void Update ( GameTime gameTime, Transform transform, StepComponent step, UserCommandComponent uc, Matrix[] bones )
		{
			UpdateLocomotionState( gameTime, transform, step, uc );

			//	update tilt :
			var run			= step.GroundVelocity.Length()>0.2f;
			var traction	= step.HasTraction;
			var tiltVel		= (traction ? 8 : 2) * gameTime.ElapsedSec;

			var accel = Vector3.TransformNormal( step.LocalAcceleration, Matrix.Invert(transform.TransformMatrix) );

			tiltForward.Weight	=	1;
			tiltForward.Factor	=	Vector2.MoveTo( tiltForward.Factor, new Vector2( uc.MoveForward, uc.MoveRight ) * (traction?1:0), tiltVel );

			//	update composer :
			composer.Update( gameTime, transform.TransformMatrix, false, bones );
		}
	}
}
