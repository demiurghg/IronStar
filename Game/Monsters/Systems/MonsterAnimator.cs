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
	class MonsterAnimator
	{
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer composer;

		readonly AnimationPose pose;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, Scene scene, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			composer		=	new AnimationComposer( fxPlayback, scene );

			pose			=	new AnimationPose( scene, null, "Take 001", AnimationBlendMode.Override );

			composer.Tracks.Add( pose );
		}


		public void Update ( GameTime gameTime, Matrix worldTransform, Matrix[] bones )
		{
			pose.Frame		=	0;//(int)(gameTime.Frames % 6);
			composer.Update( gameTime, worldTransform, false, bones );
		}
	}
}
