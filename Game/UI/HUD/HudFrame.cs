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
using Fusion.Engine.Frames.Layouts;

namespace IronStar.UI.HUD 
{
	public class HudFrame : Frame 
	{
		public Frame			CrossHair	{ get { return crossHair; } }
		public HudIndicator		Health		{ get { return health; } }
		public HudIndicator		Armor		{ get { return armor; } }
		public HudIndicator		Ammo		{ get { return ammo; } }

		Frame			crossHair;
		Frame			compass;
		Frame			mission;
		Frame			stats;
		Frame			message;
		Frame			subtitles;
		Frame			target;

		HudIndicator	health;
		HudIndicator	armor;
		HudIndicator	ammo;
		HudItem			key0;
		HudItem			key1;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public HudFrame ( FrameProcessor ui ) : base( ui )
		{
			this.BackColor		=	Color.Zero;
			this.BorderColor	=	Color.Zero;
			this.Border			=	0;
			this.PaddingLeft	=	64;
			this.PaddingRight	=	64;
			this.PaddingTop		=	32;
			this.PaddingBottom	=	32;

			this.X			=	0;
			this.Y			=	0;
			this.Width		=	ui.RootFrame.Width;
			this.Height		=	ui.RootFrame.Height;

			// #TODO #UI -- warning USE PAGE LAYOUT!
			//var layout		=	new PageLayout();
			//this.Layout		=	layout;

			int w			=	ui.RootFrame.Width;
			int h			=	ui.RootFrame.Height;

			this.Anchor		=	FrameAnchor.All;

			this.Ghost		=	true;

			//	create placeholders :
			compass				=	CreatePlaceholder( 384,  48, 512,  32, "COMPASS" );
			mission				=	CreatePlaceholder(  64, 128, 320, 128, "MISSION GOALS" );
			stats				=	CreatePlaceholder( 896, 128, 320,  64, "STATS" );
			message				=	CreatePlaceholder( 448, 256, 384,  64, "MESSAGE" );
			subtitles			=	CreatePlaceholder( 384, 544, 512,  96, "SUBTITLES" );
			target				=	CreatePlaceholder( 448, 400, 384,  64, "TARGET" );

			//	create crosshair :
			crossHair			=	new Frame( ui, w/2-32, h/2-32, 64,64,"", Color.Zero );
			crossHair.Image		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			crossHair.ImageMode	=	FrameImageMode.Centered;
			crossHair.ImageColor=	new Color(192,192,192,255);
			crossHair.Anchor	=	FrameAnchor.None;

			//	create indicators :
			health	=	new HudIndicator( ui,  HudAlignment.Left,   128, 604,  87, 100, @"ui\icons\icon_health"	, HudColors.HealthColor );
			armor	=	new HudIndicator( ui,  HudAlignment.Left,   128, 640,  34, 100, @"ui\icons\icon_armor"	, HudColors.ArmorColor );
			ammo	=	new HudIndicator( ui,  HudAlignment.Right, 1024, 604, 156, 200, @"ui\icons\icon_bullets", HudColors.WeaponColor );

			key0	=	new HudItem( ui,  HudAlignment.Left, 64, 360,	"RED\nKEYCARD", "USE TOOPEN\nRED DOORS", @"ui\icons\icon_keycard" , Color.Red );
			key1	=	new HudItem( ui,  HudAlignment.Left, 64, 396,	"BLUE\nKEYCARD", "USE TOOPEN\nBLUE DOORS", @"ui\icons\icon_keycard" , Color.Blue );

			Add( crossHair );
			Add( health );
			Add( armor );
			Add( ammo );
			Add( key0 );
			Add( key1 );
		}


		Frame CreatePlaceholder( int x, int y, int w, int h, string name )
		{
			var panel = new Frame( Frames, x, y, w, h, name, Color.Zero ) {
				Border		=	1,
				ForeColor	=	new Color(255,255,255, 0*64),
				BorderColor	=	new Color(255,255,255, 0*64),
			};

			Add( panel );

			return panel;
		}
	}
}
