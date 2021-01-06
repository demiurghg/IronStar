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
	class MonsterAnimationSystem : ProcessingSystem<MonsterAnimator,CharacterController,RenderModel,StepComponent>
	{
		readonly Game Game;
		readonly FXPlayback fxPlayback;
		readonly PhysicsCore physics;


		public MonsterAnimationSystem( Game game, FXPlayback fxPlayback, PhysicsCore physics )
		{								
			this.Game		=	game;
			this.fxPlayback	=	fxPlayback;
			this.physics	=	physics;
		}

		public override Aspect GetAspect()
		{
			return base.GetAspect().Include<BoneComponent,Transform>();
		}

		protected override MonsterAnimator Create( Entity entity, CharacterController ch, RenderModel rm, StepComponent step )
		{
			var scene		=	entity.gs.Content.Load( rm.scenePath, Scene.Empty );
			var transform	=	rm.transform;
			var animator	=	new MonsterAnimator( fxPlayback, scene, physics );

			return animator;
		}

		protected override void Destroy( Entity entity, MonsterAnimator animator )
		{
		}

		
		protected override void Process( Entity entity, GameTime gameTime, MonsterAnimator animator, CharacterController ch, RenderModel rm, StepComponent step )
		{
			var bones		=	entity.GetComponent<BoneComponent>();
			var transform	=	entity.GetComponent<Transform>();
			var uc			=	entity.GetComponent<UserCommandComponent>();
			animator?.Update( gameTime, transform.TransformMatrix, step, uc, bones.Bones );
		}
	}
}
