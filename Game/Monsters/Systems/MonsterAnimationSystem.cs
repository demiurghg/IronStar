﻿using System;
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
using BEPUutilities.Threading;

namespace IronStar.Monsters.Systems
{
	class MonsterAnimationSystem : ProcessingSystem<MonsterAnimator,CharacterController,RenderModel,StepComponent,UserCommandComponent>
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
			return base.GetAspect().Include<BoneComponent,Transform>().Exclude<RagdollComponent>();
		}

		protected override MonsterAnimator Create( Entity entity, CharacterController ch, RenderModel rm, StepComponent step, UserCommandComponent uc )
		{
			var scene		=	entity.gs.Content.Load( rm.scenePath, Scene.Empty );
			var transform	=	rm.transform;
			var animator	=	new MonsterAnimator( fxPlayback, entity, scene, physics, uc );

			return animator;
		}

		protected override void Destroy( Entity entity, MonsterAnimator animator )
		{
		}

		
		protected override void Process( Entity entity, GameTime gameTime, MonsterAnimator animator, CharacterController ch, RenderModel rm, StepComponent step, UserCommandComponent uc )
		{
			if (true)
			{
				var dr = physics.Game.RenderSystem.RenderWorld.Debug.Async;

				foreach ( var node in animator.Scene.Nodes )
				{
					//dr.DrawBasis( node.BindPose, 1.0f, 2 );
				}

				var bones		=	entity.GetComponent<BoneComponent>();
				var transform	=	entity.GetComponent<Transform>();
				animator?.Update( gameTime, transform, step, uc, bones.Bones );

				foreach ( var bone in bones.Bones )
				{
					dr.DrawBasis( bone, 0.7f, 3 );
				}
			}
		}
	}
}
