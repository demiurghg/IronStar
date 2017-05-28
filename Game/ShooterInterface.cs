using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Input;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.Configuration;
using Fusion;
using Fusion.Engine.Client;
using Fusion.Engine.Common;
using Fusion.Engine.Server;
using IronStar.UI.Controls;
using IronStar.UI.Generators;
using IronStar.UI.Pages;
using IronStar.UI;

namespace IronStar {

	class ShooterInterface : IUserInterface {

		readonly Game Game;
        readonly IMenuGenerator menuGenerator;


        public Dictionary<string, Page> Menus { get; set; } = new Dictionary<string, Page>();
        public Page ActiveMenu
        {
            get
            {
                return activeMenu;
            }
            set
            {
                if (activeMenu != null)
                {
                    activeMenu.Visible = false;
                    activeMenu.Enabled = false;

                }
                activeMenu = value;
                if (activeMenu != null)
                {
                    activeMenu.Visible = true;
                    activeMenu.Enabled = true;
                }
            }
        }


        private Page activeMenu;

        private Dictionary<string, IPageOption> requiredMenus;
        private ClientState previousClientState;
        private bool firstStart = true;

        SpriteLayer uiLayer;

        /// <summary>
        /// Creates instance of ShooterDemoUserInterface
        /// </summary>
        /// <param name="engine"></param>
        public ShooterInterface ( Game game )
		{
			this.Game	=	game;
			ShowMenu	=	true;
            menuGenerator = new MenuGenerator(game);

        }



		/// <summary>
		/// Called after the ShooterDemoUserInterface is created,
		/// </summary>
		public void Initialize ()
		{
			uiLayer	=	new SpriteLayer(Game.RenderSystem, 1024);


			//	add console sprite layer to master view layer :
			Game.RenderSystem.SpriteLayers.Add( uiLayer );


			LoadContent();
			Game.Reloading += (s,e) => LoadContent();

			Game.GameClient.ClientStateChanged += GameClient_ClientStateChanged;

            requiredMenus  = new Dictionary<string, IPageOption>()
            {
                { "StartMenu",  new StartPageOptions(Game)},
                { "MainMenu",  new MainPageOptions(Game)},
                { "SettingsMenu",  new SettingsPageOptions(Game)},
                { "AudioSettingsMenu",  new AudioSettingsPageOptions(Game)},
                { "LoadingMenu",  new LoadingPageOptions(Game)},
                { "ConnectingMenu",  new ConnectingPageOptions(Game)},
                { "AwaitingMenu",  new AwaitingPageOptions(Game)},
                { "GameMenu",  new GameMenuPageOptions(Game)},
            };


            Game.Frames.DefaultFont = Game.Content.Load<SpriteFont>
                ($@"fonts\\{MenuGenerator.MainFont}{MenuGenerator.GetMainFontSize(Game.RenderSystem.DisplayBounds.Width,
                                                                                  Game.RenderSystem.DisplayBounds.Height)}");

            InitializeMenus();
            ActiveMenu = Menus["StartMenu"];

            Game.Frames.RootFrame.Resize += (s, e) =>
            {
                Game.Frames.DefaultFont = Game.Content.Load<SpriteFont>
                ($@"fonts\\{MenuGenerator.MainFont}{MenuGenerator.GetMainFontSize(Game.RenderSystem.DisplayBounds.Width,
                                                                                  Game.RenderSystem.DisplayBounds.Height)}");
                foreach (var menu in Menus.Values)
                {
                    Game.Frames.RootFrame.Remove(menu);
                }
                Menus.Clear();
                InitializeMenus();
            };
		}



        private void InitializeMenus()
        {
            foreach (var menuName in requiredMenus.Keys)
            {
                var menu = menuGenerator.CreateMenu(menuName, requiredMenus[menuName]);
                menu.Visible = false;
                Menus[menuName] = menu;
                Game.Frames.RootFrame.Add(menu);
            }

            if (ActiveMenu != null)
            {
                ActiveMenu = Menus[ActiveMenu.Name];
            }
        }
		
        public void SetActiveMenu(string name)
        {
            ActiveMenu = Menus[name];
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
			if (disposing) {
				uiLayer?.Dispose();
			}
		}


		public void Dispose()
		{
			Dispose(true);
		}


		float dofFactor = 0;



		/// <summary>
		/// Called when the game has determined that UI logic needs to be processed.
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( GameTime gameTime )
		{
			//	update console :
			Game.Console.Update( gameTime );

			uiLayer.Clear();


         


            if (Game.GameEditor.Instance != null) {
                foreach (var menu in Menus.Values)
                {
                    menu.Visible = false;
                }
				return;
			}

			var clientState	=	Game.GameClient.ClientState;

            #region RStoMENU
            dofFactor	=	MathUtil.Lerp( (float)dofFactor, Game.Keyboard.IsKeyDown(Keys.Q) ? 1.0f : 0.0f, 0.1f );

			Game.RenderSystem.RenderWorld.DofSettings.PlaneInFocus	=	7;
			Game.RenderSystem.RenderWorld.DofSettings.FocalLength	=	0.1f;
			Game.RenderSystem.RenderWorld.DofSettings.Enabled		=	dofFactor > 0.01f;
			Game.RenderSystem.RenderWorld.DofSettings.Aperture		=	dofFactor * 20;
            #endregion

            

            switch (clientState) {
				case ClientState.StandBy:

                    if (firstStart)
                    {
                        if (previousClientState != ClientState.StandBy)
                        {
                            SetActiveMenu("StartMenu");
                        }
                        if (Game.Keyboard.IsKeyDown(Keys.Enter))
                        {
                            firstStart = false;
                            SetActiveMenu("MainMenu");
                        }
                        break;
                    }

                    if (previousClientState != ClientState.StandBy)
                    {
                        SetActiveMenu("MainMenu");
                    } 

                    if (Game.Keyboard.IsKeyDown(Keys.Escape))
                    {
                        ActiveMenu = Menus["MainMenu"];
                    }
                    break;
				case ClientState.Connecting:
                    if (previousClientState != ClientState.Connecting)
                    {
                        SetActiveMenu("ConnectingMenu");
                    }
                    break;
				case ClientState.Loading:
                    if (previousClientState != ClientState.Loading)
                    {
                        SetActiveMenu("LoadingMenu");
                    }
                    break;
				case ClientState.Awaiting:
                    if (previousClientState != ClientState.Loading)
                    {
                        SetActiveMenu("AwaitingMenu");
                    }
                    break;
				case ClientState.Disconnected:
                    if (previousClientState != ClientState.Loading)
                    {
                        SetActiveMenu("LoadingMenu");
                    }
                    break;
				case ClientState.Active:
                    if (!ShowMenu)
                    {
                        ActiveMenu = null;
                    }

                    if (Game.Keyboard.IsKeyDown(Keys.Escape))
                    {
                        SetActiveMenu("GameMenu");
                        ShowMenu = true;
                    }
                    break;
			}

			if (ShowMenu) { 
                Game.Mouse.IsMouseCentered	=	false;
				Game.Mouse.IsMouseClipped	=	false;
				Game.Mouse.IsMouseHidden	=	false;

			} else {
				if (!Game.Console.IsShown) {
					Game.Keyboard.ScanKeyboard	=	true;
					Game.Mouse.IsMouseCentered	=	true;
					Game.Mouse.IsMouseClipped	=	true;
					Game.Mouse.IsMouseHidden	=	true;
				} else {
					Game.Keyboard.ScanKeyboard	=	false;
					Game.Mouse.IsMouseCentered	=	false;
					Game.Mouse.IsMouseClipped	=	false;
					Game.Mouse.IsMouseHidden	=	false;
				}
			}

            previousClientState = clientState;

        }



		/// <summary>
		/// Draw loading screen
		/// </summary>
		/// <param name="message"></param>
        /// 

        /*
		void DrawLoadingScreen ( string message )
		{

			uiLayer.Draw( null, 0,vp.Height/4, vp.Width, vp.Height/2, new Color(0,0,0,192) );

			var h = textFont.LineHeight;

			//titleFont.DrawString( uiLayer, message, 100,vp.Height/2 - h*2, new Color(242,242,242) );
			textFont.DrawString( uiLayer, message, 100,vp.Height/2 - h, new Color(220,20,60) );
		}*/


		/// <summary>
		/// Draws stand-by screen
		/// </summary>
        /// 
        
        /*
		void DrawStandByScreen ()
		{
			var vp = Game.RenderSystem.DisplayBounds;

			uiLayer.Draw( background, 0,0, vp.Width, vp.Height, Color.White );


			uiLayer.Draw( null, 0,vp.Height/4, vp.Width, vp.Height/2, new Color(0,0,0,192) );

			var h = textFont.LineHeight;
			//titleFont.DrawString( uiLayer, "SHOOTER DEMO", 100,vp.Height/2 - h*2, new Color(242,242,242) );
			titleFont.DrawString( uiLayer, "HEROES OF THE SHOOTER AGE", 100,vp.Height/2 - h*2, new Color(242,242,242) );
			textFont.DrawString( uiLayer, "Fusion Engine Test Project", 100,vp.Height/2 - h, new Color(220,20,60) );

			textFont.DrawString( uiLayer, "Press [~] to open console:", 100,vp.Height/2 + h, new Color(242,242,242) );
			textFont.DrawString( uiLayer, "   - Enter \"map base1\" to start the game.", 100,vp.Height/2 + h*2, new Color(242,242,242) );
			textFont.DrawString( uiLayer, "   - Enter \"killserver\" to stop the game.", 100,vp.Height/2 + h*3, new Color(242,242,242) );
			textFont.DrawString( uiLayer, "   - Enter \"connect <IP:port>\" to connect to the remote game.", 100,vp.Height/2 + h*4, new Color(242,242,242) );
		}*/




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
