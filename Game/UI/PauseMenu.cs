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
using IronStar.SinglePlayer;

namespace IronStar.UI {

	public class PauseMenu : Panel {

		public PauseMenu( UIState ui ) : base(ui, 0,0,400,600)
		{
			BackColor	=	MenuTheme.BackColor;

			Layout	=	new PageLayout()
					.Margin(  MenuTheme.Margin )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					.AddRow(  40f, new float[] { -1 } )
					;

			//	create menu :

			Add( CreateHeader("GAME PAUSED" ) );

			Add( new Button( ui, "Resume", 0,0,0,0, Resume ) );
			Add( new Button( ui, "Options", 0,0,0,0, OptionsDialog ) );
			Add( new Button( ui, "Restart Checkpoint", 0,0,0,0, ()=> Log.Warning("Not implemented") ) );
			Add( new Button( ui, "Restart Level", 0,0,0,0, RestartLevel ) );
			Add( new Button( ui, "Exit To Menu", 0,0,0,0, ExitMenuDialog ) );
			Add( new Button( ui, "Exit To System", 0,0,0,0, ExitGameDialog ) );
		}


		void Resume ()
		{
			Game.GetService<Mission>().State.Continue();
		}

		void RestartLevel ()
		{
			Game.GetService<Mission>().State.Exit();
			Game.Invoker.ExecuteString("map " + IronStar.LastMapName);
		}

		void ExitToMenu ()
		{
			Game.GetService<Mission>().State.Exit();
		}

		void ExitMenuDialog ()
		{
			MessageBox.ShowQuestion( this, "EXIT MENU", "Are you sure you want to exit to main menu?", ExitToMenu, null, "Exit", "Cancel" );
		}


		void ExitGameDialog ()
		{
			#warning Check whether GameWorld is properly disposed before exit!
			MessageBox.ShowQuestion( this, "EXIT SYSTEM", "Are you sure you want to exit to system?", () => Game.Exit(), null, "Exit Game", "Cancel" );
		}


		void OptionsDialog ()
		{
			var video		=	Game.GetService<RenderSystem>();
			var audio		=	Game.GetService<SoundSystem>();
			var gameplay	=	(object)null;
			var controls	=	(object)null;
			
			OptionsBox.ShowDialog( this, video, audio, gameplay, controls );
		}
	}
}
