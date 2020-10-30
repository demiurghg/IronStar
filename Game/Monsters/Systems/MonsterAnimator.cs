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

namespace IronStar.Monsters.Systems
{
	class MonsterAnimator
	{
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;

		readonly AnimationComposer composer;


		public MonsterAnimator( SFX.FXPlayback fxPlayback, RenderModelInstance model, PhysicsCore physics )
		{								
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;

			//composer		=	new AnimationComposer( fxPlayback, 
		}


		public void Update ( GameTime gameTime )
		{
		}
	}
}
