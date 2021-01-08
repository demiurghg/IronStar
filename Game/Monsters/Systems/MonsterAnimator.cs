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
		LocomotionState2		locomotionState;



		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			locomotionLayer		=	new Sequencer( scene, null, AnimationBlendMode.Override );
			tiltForward			=	new BlendSpaceD4( scene, null, "tilt", AnimationBlendMode.Additive );
			locomotionState		=	new Idle(this);

			composer.Tracks.Add( locomotionLayer );
			composer.Tracks.Add( tiltForward );
		}



		public void UpdateLocomotionState( GameTime gameTime, StepComponent step, UserCommandComponent uc )
		{
			locomotionState	=	locomotionState.Next( gameTime, uc, step );
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, StepComponent step, UserCommandComponent uc, Matrix[] bones )
		{
			UpdateLocomotionState( gameTime, step, uc );

			var run			= step.GroundVelocity.Length()>0.2f;
			var traction	= step.HasTraction;
			var tiltVel		= (traction ? 8 : 2) * gameTime.ElapsedSec;

			var accel = Vector3.TransformNormal( step.LocalAcceleration, Matrix.Invert(worldTransform) );

			tiltForward.Weight	=	1;
			tiltForward.Factor	=	Vector2.MoveTo( tiltForward.Factor, new Vector2( uc.MoveForward, uc.MoveRight ) * (traction?1:0), tiltVel );

			composer.Update( gameTime, worldTransform, false, bones );
		}
	}
}
