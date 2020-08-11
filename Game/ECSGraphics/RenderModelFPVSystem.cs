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
using IronStar.ECSGraphics;

namespace IronStar.SFX2 
{
	public class RenderModelFPVSystem : ProcessingSystem<RenderModelView,FPVComponent,RenderModel>
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;

		
		public RenderModelFPVSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;
		}


		protected override RenderModelView Create( Entity e, FPVComponent t, RenderModel rm )
		{
			return new RenderModelView( e.gs, rm, rw.Camera.CameraMatrix );
		}

		
		protected override void Destroy( Entity e, RenderModelView model )
		{
			model?.Dispose();
		}

		
		protected override void Process( Entity e, GameTime gameTime, RenderModelView model, FPVComponent t, RenderModel rm )
		{
			model.SetTransform( rw.Camera.CameraMatrix );
		}
	}
}
