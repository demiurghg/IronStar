using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using BEPUphysics.BroadPhaseEntries;
using Fusion.Scripting;
using KopiLua;
using IronStar.ECS;
using IronStar.Animation;
using System.Collections.Concurrent;

namespace IronStar.SFX2 
{
	public class RenderModelSystem : ProcessingSystem<RenderModelInstance,Transform,RenderModel>, IRenderer
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;

		readonly Aspect skinnedAspect = new Aspect().Include<RenderModelInstance,Transform,RenderModel,BoneComponent>();
		readonly Aspect rigidAspect   = new Aspect().Include<RenderModelInstance,Transform,RenderModel>()
													.Exclude<BoneComponent>();

		readonly ConcurrentQueue<RenderModelInstance>	creationQueue;
		readonly ConcurrentQueue<RenderModelInstance>	detroyQueue;

		
		public RenderModelSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;

			creationQueue	=	new ConcurrentQueue<RenderModelInstance>();
			detroyQueue		=	new ConcurrentQueue<RenderModelInstance>();
		}


		protected override RenderModelInstance Create( Entity e, Transform t, RenderModel rm )
		{
			var model = new RenderModelInstance( e.gs, rm, t.TransformMatrix );
			creationQueue.Enqueue( model );
			return model;
		}

		
		protected override void Destroy( Entity e, RenderModelInstance model )
		{
			detroyQueue.Enqueue( model );
		}

		
		protected override void Process( Entity e, GameTime gameTime, RenderModelInstance model, Transform t, RenderModel rm )
		{
			model.SetTransform( t.TransformMatrix );

			if (skinnedAspect.Accept(e))
			{
				var bones = e.GetComponent<BoneComponent>()?.Bones;

				if (bones!=null)
				{
					model.SetBoneTransforms( e.GetComponent<BoneComponent>().Bones );
				}
			}
		}

		public void Render( GameState gs, GameTime gameTime )
		{
			RenderModelInstance model;
			
			while (creationQueue.TryDequeue(out model))
			{
				model.AddInstances();
			}
			
			while (detroyQueue.TryDequeue(out model))
			{
				model.RemoveInstances();
			}
		}
	}
}
