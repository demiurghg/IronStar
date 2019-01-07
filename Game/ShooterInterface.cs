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

namespace IronStar {

	class ShooterInterface : IUserInterface {

		readonly Game Game;
		FrameProcessor frames;


        private ClientState previousClientState;
        private bool firstStart = true;


		MainMenu		mainMenu;
		LoadingScreen	loadingScreen;


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

			frames	=	Game.GetService<FrameProcessor>();

			mainMenu				=	new MainMenu( frames );
			mainMenu.Visible		=	true;

			loadingScreen			=	new LoadingScreen( frames );
			loadingScreen.Visible	=	false;

			frames.ShowFullscreenFrame( mainMenu );
			frames.RootFrame.Add( loadingScreen );
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
		[MethodImpl(MethodImplOptions.NoOptimization)]
		public bool AllowGameInput()
		{
			bool frameTarget	=	frames.TargetFrame!=null && frames.TargetFrame.IsActuallyVisible();
			bool menuVisible	=	mainMenu.Visible;
			bool loadingVisible	=	loadingScreen.Visible;
			bool consoleVisible	=	Game.Console.IsShown;
			bool editorRunning	=	Game.GetService<MapEditor>() != null;

			return !(frameTarget || menuVisible || loadingVisible || consoleVisible || editorRunning);
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
