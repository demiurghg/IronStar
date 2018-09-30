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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;
using IronStar.Entities.Players;

namespace IronStar.Views {
	public class HudFrame : Frame {

		readonly GameWorld	world;

		public Player Player { 
			get { return player; }
			set {
				if (player!=value) {
					player = value;
				}
			}
		}
		Player player;

		HudHealth hudHealth;
		HudWeapon hudWeapon;

		Frame	crossHair;

		Frame	warning;
		Frame	message;
		Frame	objective;

			//crosshair		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			//hudFont			=	Game.Content.Load<SpriteFont>(@"hud\hudFont");
			//hudFontSmall	=	Game.Content.Load<SpriteFont>(@"hud\hudFontSmall");
			//hudFontMicro	=	Game.Content.Load<SpriteFont>(@"hud\hudFontMicro");
			//iconMachinegun	=	Game.Content.Load<DiscTexture>(@"hud\machinegun");

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public HudFrame ( Frame parent ) : base( parent.Frames )
		{
			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.Padding		=	0;

			int w				=	parent.Width;
			int h				=	parent.Height;

			this.X				=	0;
			this.Y				=	0;
			this.Width			=	parent.Width;
			this.Height			=	parent.Height;

			this.Anchor			=	FrameAnchor.All;

			this.Ghost			=	true;

			hudHealth		=	new HudHealth( this,         40, h - 64 ); 
			hudWeapon		=	new HudWeapon( this, w - 40-200, h - 64 );


			crossHair			=	new Frame( Frames, w/2-32, h/2-32, 64,64,"", Color.Zero );
			crossHair.Image		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			crossHair.ImageMode	=	FrameImageMode.Centered;
			crossHair.ImageColor=	new Color(192,192,192,255);
			crossHair.Anchor	=	FrameAnchor.None;


			warning				=	new Frame( Frames, w/2-360, h/2+80, 720, 8, "", Color.Zero );
			warning.Anchor		=	FrameAnchor.None;
			warning.ForeColor	=	HudColors.WarningColor;
			warning.ShadowColor	=	HudColors.ShadowColor;
			warning.Text		=	"Warning: Low health!";
			warning.TextAlignment=	Alignment.MiddleCenter;
			warning.ShadowOffset=	new Vector2(1,1);

			message				=	new Frame( Frames, w/2-360, h/2-88, 720, 8, "", Color.Zero );
			message.Anchor		=	FrameAnchor.None;
			message.ForeColor	=	HudColors.MessageColor;
			message.ShadowColor	=	HudColors.ShadowColor;
			message.Text		=	"You need blue key";
			message.TextAlignment=	Alignment.MiddleCenter;
			message.ShadowOffset=	new Vector2(1,1);


			this.Add( hudHealth );
			this.Add( hudWeapon );

			this.Add( warning );
			this.Add( message );
			warning.Visible = true;
			message.Visible = true;

			this.Add( crossHair );


		
			parent.Add(this);
		}


		public void SetPlayer ( Player player )
		{
			this.player			=	player;
			hudHealth.Player	=	player;
			hudWeapon.Player	=	player;
		}
	}
}
