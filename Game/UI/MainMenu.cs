using System;
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
using Fusion.Engine.Common;
using Fusion;
using Fusion.Build;
using Fusion.Engine.Graphics;
using IronStar.UI.Controls;
using IronStar.UI.Controls.Dialogs;
using System.Net;
using Fusion.Engine.Audio;

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

		public MainMenu( UIState ui ) : base(ui)
		{
			Anchor	=	FrameAnchor.All;

			BackColor	=	Color.Black;

			Image		=	ui.Game.Content.Load<DiscTexture>(@"ui\background");
			ImageColor	=	new Color( 64,64,64,255 );
			ImageMode	=	FrameImageMode.Stretched;

			OverallColor	=	Color.Gray;
			ContextActivated	+=	(s,e) => { OverallColor = Color.White; Log.Message("Main Menu: Activated"); };
			ContextDeactivated	+=	(s,e) => { OverallColor = Color.Gray;  Log.Message("Main Menu: Deactivated"); };

			Layout	=	new PageLayout()
					.AddRow(  0.5f, new float[] {  -1			} )
					.AddRow( -1.0f, new float[] { 320, -1		} )
					.AddRow( 80.0f, new float[] {  -1, -1, -1	} )
					;

			//	create menu :

			Add( CreateLogo() );

			Add( CreateMenu() );
			Add( CreateEmptyFrame(ui) );

			Add( CreateFooter( textFooterLeft,		Alignment.MiddleLeft ) );
			Add( CreateFooter( textFooterCenter,	Alignment.MiddleCenter ) );
			Add( CreateFooter( textFooterRight,		Alignment.MiddleRight ) );
		}



		Frame CreateLogo ()
		{
			var frame = new Frame(ui);

			frame.BackColor		=	MenuTheme.Transparent;
			frame.Font			=	MenuTheme.BigFont;
			frame.Text			=	"ironstar";
			frame.TextAlignment	=	Alignment.BottomLeft;
			frame.ForeColor		=	MenuTheme.TextColorNormal;

			frame.PaddingLeft	=	MenuTheme.MainContentPadding-16; // to fix gap in text
			frame.PaddingBottom	=	10;

			return frame;
		}



		Frame CreateMenu ()
		{
			var frame = new Frame(ui);

			frame.BackColor		=	MenuTheme.Transparent;

			frame.PaddingLeft	=	MenuTheme.MainContentPadding;
			frame.PaddingBottom	=	20;
			frame.PaddingTop	=	20;

			frame.Layout		=	new StackLayout() { EqualWidth=true, Interval=0 };

			int height			=	MenuTheme.ElementHeight;
			int width			=	0;

			frame.Add( new BigButton(ui, "Game"			, 0,0, width, height, SelectLevel ) );
			frame.Add( new BigButton(ui, "Map Editor"	, 0,0, width, height, EditLevel ) );
			frame.Add( new BigButton(ui, "Options"		, 0,0, width, height, OptionsDialog ) );
			frame.Add( new BigButton(ui, "Credits"		, 0,0, width, height, ()=>Log.Message("Game") ) );
			frame.Add( new BigButton(ui, "Exit"			, 0,0, width, height, ExitDialog ) );

			return frame;
		}


		void SelectLevel ()
		{
			LevelBox.Show(ui);
		}


		void EditLevel ()
		{
			ui.ShowDialogCentered( new RunEditorBox(ui) );
		}


		void ExitDialog ()
		{
			MessageBox.ShowQuestion( this, "EXIT", "Are you sure you want to exit the game?", ()=>Game.Exit(), null, "Exit game", "Cancel" );
		}



		void OptionsDialog ()
		{
			var video		=	Game.GetService<RenderSystem>();
			var audio		=	Game.GetService<SoundSystem>();
			var gameplay	=	(object)null;
			var controls	=	(object)null;
			
			OptionsBox.ShowDialog( this, video, audio, gameplay, controls );
		}



		Frame CreateFooter (string text, Alignment alignment)
		{
			var frame = new Frame(ui);

			frame.BackColor		=	MenuTheme.BackColor;
			frame.BorderTop		=	1;
			// #TODO #UI -- should I use accent color?
			frame.BorderColor	=	MenuTheme.ElementColor;
			frame.Font			=	MenuTheme.SmallFont;
			frame.Text			=	text;
			frame.TextAlignment	=	alignment;
			// #TODO #UI -- should I use text color?
			frame.ForeColor		=	MenuTheme.TextColorDimmed;

			frame.PaddingLeft	=	MenuTheme.MainContentPadding;
			frame.PaddingRight	=	MenuTheme.MainContentPadding;

			return frame;
		}



	}
}
