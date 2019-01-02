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

namespace IronStar {

	class ShooterInterface : IUserInterface {

		readonly Game Game;


        private ClientState previousClientState;
        private bool firstStart = true;

		MainMenu menu;


        /// <summary>
        /// Creates instance of ShooterDemoUserInterface
        /// </summary>
        /// <param name="engine"></param>
        public ShooterInterface ( Game game )
		{
			this.Game	=	game;
			ShowMenu	=	true;
        }



		/// <summary>
		/// Called after the ShooterDemoUserInterface is created,
		/// </summary>
		public void Initialize ()
		{
			LoadContent();
			Game.Reloading += (s,e) => LoadContent();

			Game.GetService<GameClient>().ClientStateChanged += GameClient_ClientStateChanged;

			menu	=	new MainMenu( Game.GetService<FrameProcessor>() );
			Game.GetService<FrameProcessor>().RootFrame.Add( menu );

		}



		void GameClient_ClientStateChanged ( object sender, GameClient.ClientEventArgs e )
		{
			Game.Console.Hide();
		}



		void LoadContent ()
		{
		}



		/// <summary>
		/// Overloaded. Immediately releases the unmanaged resources used by this object. 
		/// </summary>
		protected virtual void Dispose ( bool disposing )
		{
		}


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
			//	update console :
			Game.Console.Update( gameTime );
        }




		public bool ShowMenu {
			get; set;
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
