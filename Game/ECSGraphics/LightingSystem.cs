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
	public class LightingSystem : ISystem 
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;


		public LightingSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;
		}


		public Aspect GetAspect()
		{
			return Aspect.Empty();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			var rs	=	gs.GetService<RenderSystem>();
			rw.LightSet.DirectLight.Direction	=	-rs.Sky.GetSunDirection();
			rw.LightSet.DirectLight.Intensity	=	 rs.Sky.GetSunIntensity(true);	

			Transform.UpdateTransformables<OmniLight>(gs);
			Transform.UpdateTransformables<SpotLight>(gs);
			Transform.UpdateTransformables<LightProbeBox>(gs);
			Transform.UpdateTransformables<LightProbeSphere>(gs);
		}
	}
}
