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
using RSOmniLight = Fusion.Engine.Graphics.OmniLight;
using RSSpotLight = Fusion.Engine.Graphics.SpotLight;
using RSLightProbe = Fusion.Engine.Graphics.LightProbe;

namespace IronStar.SFX2 
{
	public class LightingSystem : ISystem
	{
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}


		public void Update( IGameState gs, GameTime gameTime )
		{
			var rs	=	gs.Game.GetService<RenderSystem>();
			rs.RenderWorld.LightSet.DirectLight.Direction	=	-rs.Sky.GetSunDirection();
			rs.RenderWorld.LightSet.DirectLight.Intensity	=	 rs.Sky.GetSunIntensity(true);	
		}
	}
}
