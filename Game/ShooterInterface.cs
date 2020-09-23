using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Core.Input;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.Configuration;
using Fusion;
using Fusion.Engine.Client;
using Fusion.Engine.Common;
using Fusion.Engine.Server;
using Fusion.Core.Extensions;
using IronStar.UI;
using Fusion.Engine.Frames;
using System.Runtime.CompilerServices;
using IronStar.Editor;
using IronStar.SinglePlayer;
using IronStar.UI.HUD;
using IronStar.UI.Controls;

namespace IronStar {

	class ShooterInterface : IUserInterface {

		readonly Game	Game;
		FrameProcessor	frames;
		MainMenu		mainMenu;
		LoadingScreen	loadingScreen;
		HudFrame		hudFrame;
		PauseMenu		pauseMenu;

		UIContext		uiContext;

		public HudFrame HudFrame
		{
			get { return hudFrame; }
		}

        /// <summary>
        /// Creates instance of ShooterDemoUserInterface
        /// </summary>
        /// <param name="engine"></param>
        public ShooterInterface ( Game game )
		{
			this.Game	=	game;
        }



		/// <summary>
		/// Called after the ShooterDemoUserInterface is created,
		/// </summary>
		public void Initialize ()
		{
			LoadContent();
			Game.Reloading += (s,e) => LoadContent();

			Game.GetService<GameClient>().ClientStateChanged += GameClient_ClientStateChanged;
			Game.GetService<Mission>().MissionStateChanged +=ShooterInterface_MissionStateChanged;

			frames	=	Game.GetService<FrameProcessor>();

			MenuTheme.BigFont		=	frames.Game.Content.Load<SpriteFont>(@"fonts\amdrtg100");
			MenuTheme.NormalFont	=	frames.Game.Content.Load<SpriteFont>(@"fonts\armata20");
			MenuTheme.HeaderFont	=	frames.Game.Content.Load<SpriteFont>(@"fonts\armata28");
			MenuTheme.SmallFont		=	frames.Game.Content.Load<SpriteFont>(@"fonts\armata14");
			MenuTheme.ArrowDown		=	frames.Game.Content.Load<DiscTexture>(@"ui\arrowDown");


			mainMenu				=	new MainMenu( frames );
			loadingScreen			=	new LoadingScreen( frames );
			hudFrame				=	new HudFrame( frames );
			pauseMenu				=	new PauseMenu( frames );

			//	push empty frame :
			uiContext = frames.ShowFullscreenFrame( Frame.CreateBlackFrame(frames) );
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
		}


		/// <summary>
		/// Overloaded. Immediately releases the unmanaged resources used by this object. 
		/// </summary>
		protected virtual void Dispose ( bool disposing )
		{
		}


		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
		}


		/// <summary>
		/// Called when the game has determined that UI logic needs to be processed.
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
			if (AllowGameInput()) {
				Game.Mouse.IsMouseCentered	=	true;
				Game.Mouse.IsMouseClipped	=	true;
				Game.Mouse.IsMouseHidden	=	true;
			} else {
				Game.Mouse.IsMouseCentered	=	false;
				Game.Mouse.IsMouseClipped	=	false;
				Game.Mouse.IsMouseHidden	=	false;
			}
			//	update console :
			Game.Console.Update( gameTime );

			//	HACK:
			if (Game.GetService<GameClient>().ClientState==ClientState.StandBy) {
				mainMenu.Visible	=	Game.Services.GetService(typeof(MapEditor))==null;
			}
        }


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool AllowGameInput()
		{
			bool frameTarget	=	false;//frames.TargetFrame!=null && frames.TargetFrame.IsActuallyVisible();

			bool menuVisible	=	mainMenu.IsActuallyVisible();
				 menuVisible	|=	loadingScreen.IsActuallyVisible();
				 menuVisible	|=	pauseMenu.IsActuallyVisible();

			bool consoleVisible	=	Game.Console.IsShown;
			bool editorRunning	=	Game.GetService<MapEditor>() != null;

			return !(frameTarget || menuVisible  || consoleVisible || editorRunning);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ShooterInterface_MissionStateChanged( object sender, Mission.MissionEventArgs e )
		{
			switch (e.State) {
				
				case MissionState.StandBy:
					frames.Stack.PopUIContext( ref uiContext );
					uiContext = frames.ShowFullscreenFrame( mainMenu );
				break;

				case MissionState.Loading:
					frames.Stack.PopUIContext( ref uiContext );
					uiContext = frames.ShowFullscreenFrame( loadingScreen );
					loadingScreen.StatusText	=	"LOADING";
				break;

				case MissionState.Waiting:
					frames.Stack.PopUIContext( ref uiContext );
					uiContext = frames.ShowFullscreenFrame( loadingScreen );
					loadingScreen.StatusText	=	"Press [ENTER] to continue... ";
				break;

				case MissionState.Briefing:
					//loadingScreen.StatusText	=	"AWAITING SNAPSHOT...";
				break;

				case MissionState.Active:
					frames.Stack.PopUIContext( ref uiContext );
					uiContext = frames.ShowFullscreenFrame( hudFrame );
				break;

				case MissionState.Paused:
					frames.Stack.PopUIContext( ref uiContext );
					uiContext = frames.ShowDialogCentered( pauseMenu );
				break;

				case MissionState.Debriefing:
					//loadingScreen.StatusText	=	"AWAITING SNAPSHOT...";
				break;
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void GameClient_ClientStateChanged ( object sender, GameClient.ClientEventArgs e )
		{
			Game.Console.Hide();

			switch (e.ClientState) {
				case ClientState.StandBy:
					mainMenu.Visible = true;
				break;
				case ClientState.Connecting:
					mainMenu.Visible = false;
					loadingScreen.Visible		=	true;
					loadingScreen.StatusText	=	"CONNECTING...";
				break;
				case ClientState.Loading:
					loadingScreen.StatusText	=	"LOADING...";
				break;
				case ClientState.Awaiting:
					loadingScreen.StatusText	=	"AWAITING SNAPSHOT...";
				break;
				case ClientState.Active:
					loadingScreen.Visible		=	false;
					mainMenu.Visible = false;
				break;
				case ClientState.Disconnected:
					mainMenu.Visible = true;
				break;
			}
		}


		/// <summary>
		/// Called when user closes game window using Close button or Alt+F4.
		/// </summary>
		public void RequestToExit ()
		{
			Game.Exit();
		}


		/// <summary>
		/// Called when discovery respone arrives.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="serverInfo"></param>
		public void DiscoveryResponse ( System.Net.IPEndPoint endPoint, string serverInfo )
		{
			Log.Message( "DISCOVERY : {0} - {1}", endPoint.ToString(), serverInfo );
		}
	}
}
