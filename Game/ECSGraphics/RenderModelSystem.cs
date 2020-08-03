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
using IronStar.Views;
using IronStar.Items;
using Fusion.Scripting;
using KopiLua;
using IronStar.ECS;

namespace IronStar.SFX2 
{
	public class RenderModelSystem : ProcessingSystem<RenderModelView,Transform,RenderModel>
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;

		
		public RenderModelSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;
		}


		protected override RenderModelView Create( Entity e, Transform t, RenderModel rm )
		{
			return new RenderModelView( e.gs, rm, t );
		}

		
		protected override void Destroy( Entity e, RenderModelView model )
		{
			model?.Dispose();
		}

		
		protected override void Process( Entity e, GameTime gameTime, RenderModelView model, Transform t, RenderModel rm )
		{
			model.SetTransform( t.TransformMatrix );
		}
	}
}
