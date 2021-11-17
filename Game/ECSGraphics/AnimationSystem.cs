using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using Fusion.Core.Mathematics;
using IronStar.SFX2;
using Fusion.Core;
using IronStar.Animation;
using Fusion.Engine.Graphics.Scenes;

namespace IronStar.ECSGraphics
{
	public class AnimationSystem : ProcessingSystem<Scene,Transform,RenderModel,AnimationComponent,BoneComponent>
	{
		public AnimationSystem()
		{
		}

		protected override Scene Create( Entity entity, Transform transform, RenderModel renderModel, AnimationComponent animation, BoneComponent bones )
		{
			Scene scene;
			if (entity.gs.Content.TryLoad( renderModel.scenePath, out scene ))
			{
				return scene;
			}
			else
			{
				return Scene.CreateEmptyScene();
			}
		}

		protected override void Destroy( Entity entity, Scene scene )
		{
			
		}

		protected override void Process( Entity entity, GameTime gameTime, Scene scene, Transform transform, RenderModel renderModel, AnimationComponent animation, BoneComponent bones )
		{
			scene.
		}
	}
}
