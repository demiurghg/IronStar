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


		public override RenderModelView Create( GameState gs, Transform t, RenderModel rm )
		{
			return new RenderModelView( gs, rm, t );
		}

		
		public override void Destroy( GameState gs, RenderModelView model )
		{
			model?.Dispose();
		}

		
		public override void Process( GameState gs, GameTime gameTime, RenderModelView model, Transform t, RenderModel rm )
		{
			model.SetTransform( t.TransformMatrix );
		}
	}
}
