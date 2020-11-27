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
using IronStar.Animation.IK;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.Monsters.Systems
{
	class MonsterAnimator
	{
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer composer;

		readonly FBIKAnimator fbikAnimator;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, Matrix modelTransform, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			fbikAnimator	=	new FBIKAnimator( scene, modelTransform );
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, Vector3 groundVelocity, Matrix[] bones )
		{
			fbikAnimator.Evaluate( gameTime, fxPlayback.Game.RenderSystem.RenderWorld.Debug, worldTransform, bones );
			//pose.Frame		=	3;//(int)(gameTime.Frames % 6);
			//composer.Update( gameTime, worldTransform, false, bones );
		}
	}
}
