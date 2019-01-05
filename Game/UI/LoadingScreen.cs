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
using IronStar.UI.Controls.Dialogs;
using System.Net;
using Fusion.Engine.Audio;

namespace IronStar.UI {

	public class LoadingScreen : Frame {

		static Random rand = new Random();

		public string StatusText { 
			get { 
				return header.Text;
			}
			set {
				header.Text = value;
			}
		}

		string[] quotes = {
			"Roman, remember that you shall rule the nations by your authority, for this is to be your skill, to make peace the custom, to spare the conquered, and to wage war until the haughty are brought low.\r\nVirgil, Aeneid",
			"Alea iacta est (The die is cast).\r\nGaius Julius Caesar before crossing the Rubicon",
			"Laws are silent in times of war\r\nCicero",
			"War gives the right of the conquerors to impose any conditions they please upon the vanquished.\r\nGaius Julius Caesar",
			"The outcome corresponds less to expectations in war than in any other case whatsoever.\r\nLivy",
			"A bad peace is even worse than war.\r\nTacitus",
			"Veni, Vidi, Vici.\r\nGaius Julius Caesar",
			"I found Rome made of brick, I leave her clad in marble.\r\nCaesar Augustus",
		};


		Frame header;
		Frame footer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="frames"></param>
		public LoadingScreen( FrameProcessor frames ) : base(frames)
		{
			Anchor	=	FrameAnchor.All;

			BackColor	=	Color.Black;

			Image		=	frames.Game.Content.Load<DiscTexture>(@"ui\loading");
			ImageColor	=	new Color( 64,64,64,255 );

			X		=	0;
			Y		=	0;
			Width	=	frames.RootFrame.Width;
			Height	=	frames.RootFrame.Height;
			
			var pageLayout		=	new PageLayout();
			pageLayout.Margin	=	0;
			pageLayout.AddRow(  120f, new float[] { -1 } );
			pageLayout.AddRow( -1.0f, new float[] { -1 } );
			pageLayout.AddRow(  120f, new float[] { -1 } );

			this.Layout	=	pageLayout;

			//	header & footer

			header				=	new Frame( Frames, 0,0,0,0, "Loading", MenuTheme.BackColor ) {
				ForeColor		=	MenuTheme.AccentColor,
				Font			=	MenuTheme.HeaderFont,
				TextAlignment	=	Alignment.BottomLeft,
				PaddingTop		=	30,
				PaddingBottom	=	30,
				PaddingLeft		=	120,
				PaddingRight	=	120,
			};

			footer				=	new Frame( Frames, 0,0,0,0, "Some cool quote", MenuTheme.BackColor ) {
				ForeColor		=	MenuTheme.TextColorNormal,
				Font			=	MenuTheme.SmallFont,
				TextAlignment	=	Alignment.TopRight,
				PaddingTop		=	30,
				PaddingBottom	=	30,
				PaddingLeft		=	120,
				PaddingRight	=	120,
			};

			//	structure :

			Add( header );
			Add( CreateEmptyFrame(frames) );
			Add( footer );

			//	set quote :

			footer.Text	=	quotes[ rand.Next(quotes.Length) ];
		}



	}
}
