﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Frames;
using Fusion.Core.Input;
using Fusion.Engine.Frames.Layouts;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using Fusion.Core;
using IronStar.Core;
using Fusion.Engine.Common;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Graphics;
using IronStar.UI.Controls;
using System.Net;

namespace IronStar.UI {

	public class MainMenu : Frame {

	#if DEBUG
		const string textConfig			=	"Debug";
	#else
		const string textConfig			=	"Release";
	#endif

		const string textFooterLeft		=	"[ENTER] Select\r\n[ESC] Exit";
		const string textFooterCenter	=	"vk.com/ironstar_game";
		const string textFooterRight	=	"Demo Version\r\n" + textConfig;

		public MainMenu( FrameProcessor frames ) : base(frames)
		{
			MenuTheme.BigFont		=	frames.Game.Content.Load<SpriteFont>(@"fonts\amdrtg100");
			MenuTheme.NormalFont	=	frames.Game.Content.Load<SpriteFont>(@"fonts\armata24");
			MenuTheme.SmallFont		=	frames.Game.Content.Load<SpriteFont>(@"fonts\armata14");

			Anchor	=	FrameAnchor.All;

			BackColor	=	Color.Black;

			Image		=	frames.Game.Content.Load<DiscTexture>(@"ui\background");
			ImageColor	=	new Color( 64,64,64,255 );

			X		=	0;
			Y		=	0;
			Width	=	frames.RootFrame.Width;
			Height	=	frames.RootFrame.Height;
			
			var pageLayout		=	new PageLayout();
			pageLayout.Margin	=	0;
			pageLayout.AddRow(  0.5f, new float[] {  -1			} );
			pageLayout.AddRow( -1.0f, new float[] { 320, -1		} );
			pageLayout.AddRow( 80.0f, new float[] {  -1, -1, -1	} );

			this.Layout	=	pageLayout;

			//	create menu :

			Add( CreateLogo() );

			Add( CreateMenu() );
			Add( CreateEmptyFrame(frames) );

			Add( CreateFooter( textFooterLeft,		Alignment.MiddleLeft ) );
			Add( CreateFooter( textFooterCenter,	Alignment.MiddleCenter ) );
			Add( CreateFooter( textFooterRight,		Alignment.MiddleRight ) );
		}



		Frame CreateLogo ()
		{
			var frame = new Frame(Frames);

			frame.BackColor		=	MenuTheme.Transparent;
			frame.Font			=	MenuTheme.BigFont;
			frame.Text			=	"ironstar";
			frame.TextAlignment	=	Alignment.BottomLeft;
			frame.ForeColor		=	MenuTheme.TextColorNormal;

			frame.PaddingLeft	=	60-16; // to fix gap in text
			frame.PaddingBottom	=	10;

			return frame;
		}



		Frame CreateMenu ()
		{
			var frame = new Frame(Frames);

			frame.BackColor		=	MenuTheme.Transparent;

			frame.PaddingLeft	=	60;
			frame.PaddingBottom	=	20;
			frame.PaddingTop	=	20;

			frame.Layout		=	new StackLayout() { EqualWidth=true, Interval=0 };

			int height			=	40;
			int width			=	0;

			frame.Add( new BigButton(Frames, "Game"			, 0,0, width, height, ()=>Log.Message("Game") ) );
			frame.Add( new BigButton(Frames, "Multiplayer"	, 0,0, width, height, ()=>Log.Message("Game") ) );
			frame.Add( new BigButton(Frames, "Options"		, 0,0, width, height, ()=>Log.Message("Game") ) );
			frame.Add( new BigButton(Frames, "Credits"		, 0,0, width, height, ()=>Log.Message("Game") ) );
			frame.Add( new BigButton(Frames, "Exit"			, 0,0, width, height, ()=>Log.Message("Game") ) );

			return frame;
		}




		Frame CreateFooter (string text, Alignment alignment)
		{
			var frame = new Frame(Frames);

			frame.BackColor		=	MenuTheme.BackgroundColor;
			frame.BorderTop		=	1;
			frame.BorderColor	=	MenuTheme.BorderColor;
			frame.Font			=	MenuTheme.SmallFont;
			frame.Text			=	text;
			frame.TextAlignment	=	alignment;
			frame.ForeColor		=	MenuTheme.TextColorDimmed;

			frame.PaddingLeft	=	60;
			frame.PaddingRight	=	60;

			return frame;
		}



	}
}
