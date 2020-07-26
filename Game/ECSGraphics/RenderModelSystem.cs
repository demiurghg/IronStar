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
	public class RenderModelSystem : ISystem
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


		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities<RenderModel,Transform>();

			foreach ( var e in entities )
			{
				var rm	=	e.GetComponent<RenderModel>();
				var	t	=	e.GetComponent<Transform>();

				rm.SetTransform( t.TransformMatrix );
			}
		}
	}
}
