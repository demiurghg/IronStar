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
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;

namespace IronStar.UI.HUD 
{
	public class HudFrame : Frame 
	{
		//HudHealth hudHealth;
		//HudWeapon hudWeapon;

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
		public HudFrame ( FrameProcessor fp ) : base( fp )
		{
			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.Padding		=	0;

			this.X				=	0;
			this.Y				=	0;
			this.Width			=	fp.RootFrame.Width;
			this.Height			=	fp.RootFrame.Height;

			#warning USE PAGE LAYOUT!
			int w				=	fp.RootFrame.Width;
			int h				=	fp.RootFrame.Height;

			this.Anchor			=	FrameAnchor.All;

			this.Ghost			=	true;

			//crossHair			=	new Frame( Frames, w/2-32, h/2-32, 64,64,"", Color.Zero );
			//crossHair.Image		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			//crossHair.ImageMode	=	FrameImageMode.Centered;
			//crossHair.ImageColor=	new Color(192,192,192,255);
			//crossHair.Anchor	=	FrameAnchor.None;


			//warning				=	new Frame( Frames, w/2-360, h/2+80, 720, 8, "", Color.Zero );
			//warning.Anchor		=	FrameAnchor.None;
			//warning.ForeColor	=	HudColors.WarningColor;
			//warning.ShadowColor	=	HudColors.ShadowColor;
			//warning.Text		=	"";//"Warning: Low health!";
			//warning.TextAlignment=	Alignment.MiddleCenter;
			//warning.ShadowOffset=	new Vector2(1,1);

			//message				=	new Frame( Frames, w/2-360, h/2-88, 720, 8, "", Color.Zero );
			//message.Anchor		=	FrameAnchor.None;
			//message.ForeColor	=	HudColors.MessageColor;
			//message.ShadowColor	=	HudColors.ShadowColor;
			//message.Text		=	"";//"You need blue key";
			//message.TextAlignment=	Alignment.MiddleCenter;
			//message.ShadowOffset=	new Vector2(1,1);


			////this.Add( hudHealth );
			////this.Add( hudWeapon );

			//this.Add( warning );
			//this.Add( message );
			//warning.Visible = true;
			//message.Visible = true;

			////this.Add( crossHair );
		}
	}
}
