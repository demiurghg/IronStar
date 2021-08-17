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

namespace IronStar.SFX2 
{
	public class RenderModelSystem : ProcessingSystem<RenderModelInstance,KinematicState,RenderModel>
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;

		readonly Aspect skinnedAspect = new Aspect().Include<RenderModelInstance,KinematicState,RenderModel,BoneComponent>();
		readonly Aspect rigidAspect   = new Aspect().Include<RenderModelInstance,KinematicState,RenderModel>()
													.Exclude<BoneComponent>();

		
		public RenderModelSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;
		}


		protected override RenderModelInstance Create( Entity e, KinematicState t, RenderModel rm )
		{
			return new RenderModelInstance( e.gs, rm, t.TransformMatrix );
		}

		
		protected override void Destroy( Entity e, RenderModelInstance model )
		{
			model?.Dispose();
		}

		
		protected override void Process( Entity e, GameTime gameTime, RenderModelInstance model, KinematicState t, RenderModel rm )
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
	}
}
