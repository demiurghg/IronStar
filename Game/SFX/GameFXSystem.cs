using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSFactories;
using IronStar.Gameplay.Components;

namespace IronStar.UI.HUD
{
	public class GameFXSystem : IDrawSystem
	{
		readonly Game Game;

		public GameFXSystem ( Game game )
		{
			Game	=	game;
			Game.RenderSystem.GameFX.ClearFX();
			Game.RenderSystem.DofFilter.Enabled = false;
		}

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		public void Update( GameState gs, GameTime gameTime ) {}

		public void Draw( GameState gs, GameTime gameTime )
		{
			var player	=	gs.QueryEntities(PlayerFactory.PlayerAspect).LastOrDefault();

			if (player!=null)
			{
				UpdateDamageEffect( gs, gameTime, player );
			}
		}


		void UpdateDamageEffect( GameState gs, GameTime gameTime, Entity player )
		{
			var health	=	player?.GetComponent<HealthComponent>();
			var gameFx	=	Game.RenderSystem.GameFX;

			if (health.LastDamage>0) 
			{
				gameFx.RunPainFX ( (health.LastDamage < health.MaxHealth / 3) ? 0.5f : 1.0f );
			}

			if (health.Health<=0)
			{
				gameFx.RunDeathFX();
				Game.RenderSystem.DofFilter.Enabled = true;
			}
		}
	}
}
