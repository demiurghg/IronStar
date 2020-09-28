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

namespace IronStar.UI.HUD
{
	public class HudSystem : ISystem
	{
		readonly Game Game;

		HudFrame hudFrame;

		Frame	crossHair;

		HudIndicator	health;
		HudIndicator	armor;



		public HudSystem ( Game game )
		{
			this.Game	=	game;
			hudFrame	=	(game.GetService<UserInterface>().Instance as ShooterInterface)?.HudFrame;

			int w		=	hudFrame.Width;
			int h		=	hudFrame.Height;
			var ui		=	hudFrame.Frames;

			//	create crosshair :
			crossHair			=	new Frame( ui, w/2-32, h/2-32, 64,64,"", Color.Zero );
			crossHair.Image		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			crossHair.ImageMode	=	FrameImageMode.Centered;
			crossHair.ImageColor=	new Color(192,192,192,255);
			crossHair.Anchor	=	FrameAnchor.None;

			hudFrame.Add( crossHair );


			//	create health bar :
			health	=	new HudIndicator( ui, 64, h-116, 87, 100, "icon_health", Color.Red );
			armor	=	new HudIndicator( ui, 64, h- 80, 34, 100, "icon_armor" , Color.Orange );

			hudFrame.Add( health );
			hudFrame.Add( armor );

		}

		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		public void Update( GameState gs, GameTime gameTime )
		{
		}
	}
}
