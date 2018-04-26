using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Reflection;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using Fusion.Drivers.Input;
using System.IO;
using System.Diagnostics;
using Fusion.Drivers.Graphics;
using SharpDX.Windows;
using Fusion.Core;
using Fusion.Development;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Configuration;
using Fusion.Core.Shell;
using Fusion.Core.IniParser;
using Fusion.Engine.Graphics;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Lidgren.Network;
using Fusion.Engine.Storage;
using Fusion.Engine.Audio;
using Fusion.Engine.Tools;
using Fusion.Engine.Frames;
using System.ComponentModel;
using Fusion.Build;

namespace Fusion.Engine.Common {

	/// <summary>
	/// Provides basic graphics device initialization, game logic, and rendering code. 
	/// </summary>
	public abstract class Game : DisposableBase {

		/// <summary>
		/// Game instance.
		/// </summary>
		public static Game Instance = null;

		/// <summary>
		/// Gets settings.
		/// </summary>
		public ConfigManager Config { get { return config; } }

		/// <summary>
		/// Gets the current input device
		/// </summary>
		internal InputDevice	InputDevice { get { return inputDevice; } }

		/// <summary>
		/// Gets the current graphics device
		/// </summary>
		internal GraphicsDevice GraphicsDevice { get { return graphicsDevice; } }

		/// <summary>
		/// Gets the render system
		/// </summary>
		public	RenderSystem RenderSystem { get { return renderSystem; } }

		/// <summary>
		/// Gets the sound system
		/// </summary>
		public SoundSystem SoundSystem { get { return soundSystem; } }

		/// <summary>
		/// Gets the network system.
		/// Actually used only for configuration both client and server.
		/// </summary>
		public Network Network { get { return network; } }

		/// <summary>
		/// Gets current content manager
		/// </summary>
		public ContentManager Content { get { return content; } }

		/// <summary>
		/// Gets keyboard.
		/// </summary>
		public Keyboard Keyboard { get { return keyboard; } }

		/// <summary>
		/// Gets mouse.
		/// </summary>
		public Mouse Mouse { get { return mouse; } }

		/// <summary>
		/// Gets mouse.
		/// </summary>
		public Touch Touch { get { return touch; } }

		/// <summary>
		/// Gets gamepads
		/// </summary>
		public GamepadCollection Gamepads { get { return gamepads; } }

		/// <summary>
		/// Gets invoker
		/// </summary>
		public	Invoker Invoker { get { return invoker; } }

		/// <summary>
		/// Gets user storage.
		/// </summary>
		public IStorage UserStorage { get { return userStorage; } }

		/// <summary>
		/// Gets console
		/// </summary>
		public GameConsole Console { get { return console; } }

		/// <summary>
		/// Gets frame processor
		/// </summary>
		public FrameProcessor Frames { get { return frames; } }


		/// <summary>
		/// Sets and gets game window icon.
		/// </summary>
		public System.Drawing.Icon Icon {
			get {
				return windowIcon;
			}
			set {
				if (IsInitialized) {
					throw new InvalidOperationException("Can not set Icon after game engine initialization");
				}
				windowIcon = value;
			}
		}
		System.Drawing.Icon windowIcon = null;


		/// <summary>
		/// Gets and sets game window title.
		/// </summary>
		public string GameTitle { 
			get {
				return gameTitle;
			} 
			set {
				if (value==null) {
					throw new ArgumentNullException();
				}
				if (string.IsNullOrWhiteSpace(value)) {
					throw new ArgumentException("GameTitle must be readable string", "value");
				}
				if (IsInitialized) {
					throw new InvalidOperationException("Can not set GameTitle after game engine initialization");
				}
				gameTitle = value;
			} 
		}
		string gameTitle = Path.GetFileNameWithoutExtension( Process.GetCurrentProcess().ProcessName );


		/// <summary>
		/// Enable COM object tracking
		/// </summary>
		public bool TrackObjects {
			get {
				return SharpDX.Configuration.EnableObjectTracking;
			} 
			set {
				SharpDX.Configuration.EnableObjectTracking = value;
			}
		}


		/// <summary>
		/// Indicates whether the game is initialized.
		/// </summary>
		public	bool IsInitialized { get { return initialized; } }

		/// <summary>
		/// Indicates whether Game.Update and Game.Draw should be called on each frame.
		/// </summary>
		public	bool Enabled { get; set; }

		/// <summary>
		/// Raised when the game exiting BEFORE disposing.
		/// </summary>
		public event	EventHandler Exiting;

		/// <summary>
		/// Raised after Game.Reload() called.
		/// This event used primarily for developement puprpose.
		/// </summary>
		public event	EventHandler Reloading;


		/// <summary>
		/// Raised when the game gains focus.
		/// </summary>
		public event	EventHandler Activated;

		/// <summary>
		/// Raised when the game loses focus.
		/// </summary>
		public event	EventHandler Deactivated;


		bool	initialized		=	false;
		bool	requestExit		=	false;
		bool	requestReload	=	false;

		internal bool ExitRequested { get { return requestExit; } }


		ConfigManager		config		;
		InputDevice			inputDevice		;
		GraphicsDevice		graphicsDevice	;
		RenderSystem		renderSystem	;
		SoundSystem			soundSystem		;
		Network				network			;
		ContentManager		content			;
		Invoker				invoker			;
		Keyboard			keyboard		;
		Mouse				mouse			;
		Touch				touch;
		GamepadCollection	gamepads		;
		UserStorage			userStorage		;
		GameConsole		console;
		FrameProcessor		frames;


		GameTime	gameTimeInternal;

		GameServer	sv;
		GameClient cl;
		UserInterface ui;
		GameEditor	ed;


		/// <summary>
		/// Current game server.
		/// </summary>
		public GameServer GameServer { 
			get { return sv; } 
		}
		
		/// <summary>
		/// Current game client.
		/// </summary>
		public GameClient GameClient {
			get { return cl; } 
		}

		/// <summary>
		/// Current game interface.
		/// </summary>
		public UserInterface UserInterface {
			get { return ui; } 
		}

		/// <summary>
		/// Current game interface.
		/// </summary>
		public GameEditor GameEditor {
			get { return ed; } 
		}


		public abstract IClientInstance	CreateClient ( Game game, IMessageService msgsvc, Guid clientGuid );
		public abstract IServerInstance CreateServer ( Game game, IMessageService msgsvc, string map, string options );
		public abstract IUserInterface	CreateUI ( Game game );	 
		public abstract IEditorInstance	CreateEditor ( Game game, string map );
		public abstract void LoadConfig ();
		public abstract void SaveConfig ();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		/// <param name="sv"></param>
		/// <param name="cl"></param>
		/// <param name="gi"></param>
		public void Run ()
		{
			//	init game and subsystems :
			InitInternal();

			//	run game loop :
			RenderLoop.Run( GraphicsDevice.Display.Window, UpdateInternal );

			//	wait for server 
			//	if it is still running :
			cl.Wait();
			sv.Wait();

			//	call exit event :
			Exiting?.Invoke( this, EventArgs.Empty );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string GetReleaseInfo ()
		{
			return string.Format("{0} {1} {2} {3}", 
				Assembly.GetExecutingAssembly().GetName().Name, 
				Assembly.GetExecutingAssembly().GetName().Version,
				#if DEBUG
					"debug",
				#else
					"release",
				#endif
				(IntPtr.Size==4? "x86" : "x64")
			);
		}



		/// <summary>
		/// Game ID is used for networking as application identifier.
		/// </summary>
		public string GameID {
			get { return gameId; }
		}
		readonly string gameId;


		/// <summary>
		/// Initializes a new instance of this class, which provides 
		/// basic graphics device initialization, game logic, rendering code, and a game loop.
		/// </summary>
		public Game ( string gameId )
		{
			this.gameId	=	gameId;
			Enabled	=	true;

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += currentDomain_UnhandledException;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			CultureInfo.DefaultThreadCurrentCulture	=	CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentCulture		=	CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture	=	CultureInfo.InvariantCulture;


			//ForceAssemblies();


			Debug.Assert( Instance == null );

			Instance	=	this;

			Log.Message(GetReleaseInfo());
			Log.Message("Startup directory : {0}", AppDomain.CurrentDomain.BaseDirectory );
			Log.Message("Current directory : {0}", Directory.GetCurrentDirectory() );

			//	For animation rendering applications :
			//	http://msdn.microsoft.com/en-us/library/bb384202.aspx
			GCSettings.LatencyMode	=	GCLatencyMode.Interactive;

			config				=	new ConfigManager( this );
			userStorage			=	new UserStorage(this);

			LoadConfig();

			invoker				=	new Invoker();
			inputDevice			=	new InputDevice( this );
			graphicsDevice		=	new GraphicsDevice( this );
			renderSystem		=	new RenderSystem( this );
			soundSystem			=	new SoundSystem( this );
			network				=	new Network( this );
			content				=	new ContentManager( this );
			gameTimeInternal	=	new GameTime();
			console				=	new GameConsole(this);

			keyboard			=	new Keyboard(this);
			mouse				=	new Mouse(this);
			touch				=	new Touch(this);
			gamepads			=	new GamepadCollection(this);

			frames				=	new FrameProcessor(this);


			//	create SV, CL and UI instances :
			sv = new GameServer( this );
			cl = new GameClient( this );
			ui = new UserInterface( this );
			ed = new GameEditor( this );

			RegisterCommands();

			config.ApplySettings( SoundSystem	);
			config.ApplySettings( RenderSystem	);
			config.ApplySettings( Frames		);
			config.ApplySettings( Console		);
			config.ApplySettings( Network		);
			config.ApplySettings( Keyboard		);
			config.ApplySettings( Touch			);	  
			config.ApplySettings( Mouse			);
			config.ApplySettings( sv			);
			config.ApplySettings( cl			);
			config.ApplySettings( ui			);
			config.ApplySettings( ed			);
		}



		void currentDomain_UnhandledException ( object sender, UnhandledExceptionEventArgs e )
		{
			ExceptionDialog.Show( (Exception) e.ExceptionObject );
		}



		
		/// <summary>
		/// Manage game to raise Reloading event.
		/// </summary>
		public void Reload()
		{
			if (!IsInitialized) {
				throw new InvalidOperationException("Game is not initialized");
			}
			requestReload = true;
		}



		/// <summary>
		/// Request game to exit.
		/// Game will quit when update & draw loop will be completed.
		/// </summary>
		public void Exit ()
		{
			if (!IsInitialized) {
				Log.Warning("Game is not initialized");
				return;
			}
			requestExit	=	true;
		}


		bool requestFullscreenOnStartup = false;



		/// <summary>
		/// InitInternal
		/// </summary>
		internal bool InitInternal ()
		{
			Log.Message("");
			Log.Message("-------- Game Initializing --------");

			var p = new GraphicsParameters();
			RenderSystem.ApplyParameters( ref p );

			//	going to fullscreen immediatly on startup lead to 
			//	bugs and inconsistnecy for diferrent stereo modes,
			//	so we store fullscreen mode and apply it on next update step.
			requestFullscreenOnStartup	=	p.FullScreen;
			p.FullScreen = false;


			//	initialize drivers :
			GraphicsDevice.Initialize(p);
			InputDevice.Initialize();
																		   
			//	initiliaze core systems :
			Initialize( SoundSystem );
			Initialize( RenderSystem );
			Initialize( Keyboard );
			Initialize( Mouse );
			Initialize( Touch );

			//	initialize additional systems :
			Initialize( Console );
			Initialize( Frames );

			//	initialize game-specific systems :
			Initialize( UserInterface );
			Initialize( GameClient );
			Initialize( GameServer );
			Initialize( GameEditor );

			//	init game :
			Log.Message("");

			//	attach console sprite layer :
			Console.ConsoleSpriteLayer.Order = int.MaxValue / 2;
			RenderSystem.SpriteLayers.Add( Console.ConsoleSpriteLayer );

			Frames.FramesSpriteLayer.Order = int.MaxValue / 2 - 1;
			RenderSystem.SpriteLayers.Add( Frames.FramesSpriteLayer );

			initialized	=	true;

			Log.Message("-----------------------------------------");
			Log.Message("");

			Exiting+=Game_Exiting;

			return true;
		}


		private void Game_Exiting( object sender, EventArgs e )
		{
			config.RetrieveSettings( SoundSystem	);
			config.RetrieveSettings( RenderSystem	);
			config.RetrieveSettings( Frames			);
			config.RetrieveSettings( Console		);
			config.RetrieveSettings( Network		);
			config.RetrieveSettings( Keyboard		);
			config.RetrieveSettings( Touch			);
			config.RetrieveSettings( Mouse			);
			config.RetrieveSettings( sv				);
			config.RetrieveSettings( cl				);
			config.RetrieveSettings( ui				);
			config.RetrieveSettings( ed				);
		}

		
		Stack<GameComponent> modules = new Stack<GameComponent>();


		void Initialize ( GameComponent module )
		{
			Log.Message( "---- Init : {0} ----", module.GetType().Name);
			module.Initialize();

			modules.Push( module );
		}





		/// <summary>
		/// Overloaded. Immediately releases the unmanaged resources used by this object. 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (!initialized) {
				return;
			}

			Log.Message("");
			Log.Message("-------- Game Shutting Down --------");

			if (disposing) {

				while ( modules.Any() ) {
					var module = modules.Pop();
					Log.Message("Disposing : {0}", module.GetType().Name );
					module.Dispose();
				}

				Log.Message("Disposing : Content");
				SafeDispose( ref content );

				Log.Message("Disposing : InputDevice");
				SafeDispose( ref inputDevice );

				Log.Message("Disposing : GraphicsDevice");
				SafeDispose( ref graphicsDevice );

				SaveConfig();

				Log.Message("Disposing : UserStorage");
				SafeDispose( ref userStorage );
			}

			base.Dispose(disposing);

			Log.Message("------------------------------------------");
			Log.Message("");

			ReportActiveComObjects();
		}



		/// <summary>
		/// Print warning message if leaked objectes detected.
		/// Works only if GameParameters.TrackObjects set.
		/// </summary>
		public void ReportActiveComObjects ()
		{
			if (SharpDX.Configuration.EnableObjectTracking) {
				if (SharpDX.Diagnostics.ObjectTracker.FindActiveObjects().Any()) {
					Log.Warning("{0}", SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects() );
				} else {
					Log.Message("Leaked COM objects are not detected.");
				}
				SharpDX.Configuration.EnableObjectTracking = false;
			} else {
				Log.Message("Object tracking disabled.");
			}
		}



		/// <summary>
		/// Returns true if game is active and receive user input
		/// </summary>
		public bool IsActive {
			get {
				return GraphicsDevice.Display.Window.Focused;
			}
		}




		bool isActiveLastFrame = true;


		/// <summary>
		/// 
		/// </summary>
		internal void UpdateInternal ()
		{
			/*if (skipFirstFrame) {
				skipFirstFrame = false;
				Thread.Sleep(1);
				return;
			} */

			if (IsDisposed) {
				throw new ObjectDisposedException("Game");
			}

			if (!IsInitialized) {
				throw new InvalidOperationException("Game is not initialized");
			}

			bool isActive = IsActive;  // to reduce access to winforms.
			if (isActive!=isActiveLastFrame) {
				isActiveLastFrame = isActive;
				if (isActive) {
					Activated?.Invoke( this, EventArgs.Empty );
				} else {
					Deactivated?.Invoke( this, EventArgs.Empty );
				}
			}

			if (Enabled) {

				if (requestFullscreenOnStartup) {
					graphicsDevice.FullScreen = requestFullscreenOnStartup;
					requestFullscreenOnStartup = false;
				}

				if (requestReload) {
					Reloading?.Invoke( this, EventArgs.Empty );
					requestReload = false;
				}

				graphicsDevice.Prepare();
				graphicsDevice.Display.Prepare();

				//	pre update :
				gameTimeInternal.Update();

				InputDevice.UpdateInput();

				Frames.Update( gameTimeInternal );
				Console.Update( gameTimeInternal );

				//GIS.Update(gameTimeInternal);

				UpdateClientServerGame( gameTimeInternal );

				//
				//	Sound :
				//
				SoundSystem.Update( gameTimeInternal );

				//
				//	Render :
				//
				var eyeList	= graphicsDevice.Display.StereoEyeList;

				foreach ( var eye in eyeList ) {

					GraphicsDevice.ResetStates();

					GraphicsDevice.Display.TargetEye = eye;

					GraphicsDevice.RestoreBackbuffer();

					GraphicsDevice.ClearBackbuffer(Color.Zero);

					this.Draw( gameTimeInternal, eye );
				}

				GraphicsDevice.Present(RenderSystem.VSyncInterval);

				InputDevice.EndUpdateInput();
			}

			try {
				invoker.ExecuteDeferredCommands();
			} catch ( Exception e ) {
				Log.Error( e.ToString() );
			}

			CheckExitInternal();
		}



		/// <summary>
		/// Called when the game determines it is time to draw a frame.
		/// In stereo mode this method will be called twice to render left and right parts of stereo image.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		protected virtual void Draw ( GameTime gameTime, StereoEye stereoEye )
		{
			//GIS.Draw(gameTime, stereoEye);

			RenderSystem.Draw( gameTime, stereoEye );

			GraphicsDevice.ResetStates();
			GraphicsDevice.RestoreBackbuffer();
		}
		

		
		/// <summary>
		/// Performs check and does exit
		/// </summary>
		private void CheckExitInternal () 
		{
			if (requestExit) {
				GraphicsDevice.Display.Window.Close();
			}
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Component model :
		 * 
		-----------------------------------------------------------------------------------------*/

		readonly List<GameComponent> components = new List<GameComponent>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="component"></param>
		/// <param name="niceName"></param>
		/// <param name="shortName"></param>
		public void AttachComponent ( GameComponent component, string niceName, string shortName )
		{
			lock (components) {
				if ( components.IndexOf(component) < 0 ) {
					components.Add( component );
				} else {	
					Log.Warning("Component {0} is already added.", component.GetType().ToString() );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void InitializeComponents ()
		{
			lock (components) {
				foreach ( var component in components ) {
					component.Initialize();
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void DisposeComponents ()
		{
			lock (components) {

				components.Reverse();

				foreach ( var component in components ) {

					var disposable = component as IDisposable;
					disposable?.Dispose();

				}

				components.Clear();
			}

		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Commands :
		 * 
		-----------------------------------------------------------------------------------------*/

		void RegisterCommands ()
		{
			invoker.RegisterCommand("quit",			(args)=>new CmdQuit(this) );
			invoker.RegisterCommand("map",			(args)=>new CmdMap(this, args) );
			invoker.RegisterCommand("killserver",	(args)=>new CmdKillServer(this, args) );
			invoker.RegisterCommand("connect",		(args)=>new CmdConnect(this, args) );
			invoker.RegisterCommand("disconnect",	(args)=>new CmdDisconnect(this, args) );
			invoker.RegisterCommand("editorMap",	(args)=>new CmdEditorMap(this, args) );
			invoker.RegisterCommand("editorQuit",	(args)=>new CmdEditorQuit(this, args) );
			invoker.RegisterCommand("contentBuild",	(args)=>new CmdContentBuild(this, args) );
			invoker.RegisterCommand("contentFile",	(args)=>new CmdContentFile() );
			invoker.RegisterCommand("contentReport",(args)=>new CmdContentReport(args) );
		}


		class CmdQuit : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;

			public CmdQuit ( Game game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.Exit();
				return null;
			}
		}


		class CmdMap : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;
			readonly string map;

			public CmdMap ( Game game, ArgList args )
			{
				if (args.Count<2) {
					throw new Exception("Missing command line arguments: map");
				}

				this.game = game;
				this.map = args[1];
			}

			public object Execute()
			{
				game.StartServer( map, false );
				return null;
			}
		}


		class CmdKillServer : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;

			public CmdKillServer ( Game game, ArgList args )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.KillServer();
				return null;
			}
		}


		class CmdConnect : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;
			readonly string host;
			readonly int port;

			public CmdConnect ( Game game, ArgList args )
			{
				if (args.Count<3) {
					throw new Exception("Missing command line arguments: host and port");
				}

				this.game	=	game;
				this.host	=	args[1];
				this.port	=	int.Parse(args[2]);
			}

			public object Execute()
			{
				game.Connect( host, port );
				return null;
			}
		}


		class CmdDisconnect : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;
			readonly string message = "";

			public CmdDisconnect ( Game game, ArgList args )
			{
				this.game	=	game;

				if (args.Count>1) {
					this.message = args[2];
				}
			}

			public object Execute()
			{
				game.Disconnect(message);
				return null;
			}
		}


		class CmdEditorMap : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;
			readonly string map;

			public CmdEditorMap ( Game game, ArgList args )
			{
				this.game = game;

				if (args.Count<2) {
					throw new Exception("Missing command line arguments: map");
				}

				map = args[1];
			}

			public object Execute()
			{
				game.GameEditor.Start( map );
				return null;
			}
		}


		class CmdEditorQuit : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			readonly Game game;
			readonly string map;

			public CmdEditorQuit ( Game game, ArgList args )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.GameEditor.Stop();
				return null;
			}
		}


		#if false
		[Command("cmd")]
		[Description("sends command to remote server")]
		string Cmd_f ( string[] args )
		{
			var rcmd = string.Join( " ", args.Skip(1) );

			GameClient.NotifyServer("*cmd " + rcmd);

			return null;
		}
		#endif


		class CmdContentBuild : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			Game	 game;
			bool	 force;
			string[] files;
			string	 clean;

			public CmdContentBuild ( Game game, ArgList args )
			{
				this.game	=	game;
				this.force	=	args.Contains("/force");
				this.files	=	args.Skip(1).Where( s=>!s.StartsWith("/") ).ToArray();
				this.clean	=	args.FirstOrDefault( a => a.StartsWith("/clean:"))?.Replace("/clean:","");
			}


			public object Execute ()
			{
				Builder.SafeBuild(force, clean, files);
				game.Reload();	
				return null;		
			}
		}


		class CmdContentFile : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			public object Execute ()
			{
				return Builder.Options.ContentIniFile;
			}
		}


		class CmdContentReport : ICommand {
			public bool IsRollbackable() { return false; }
			public void Rollback() {}

			string reportFile;

			public CmdContentReport( ArgList args )
			{
				if (args.Count<2) {
					throw new Exception("Missing command line arguments: filename");
				}
				reportFile = args[1];
			}

			public object Execute() 
			{
				Builder.OpenReport( reportFile );
				return false;
			}

		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Client-server stuff :
		 * 
		-----------------------------------------------------------------------------------------*/

		
		/// <summary>
		/// Updates game logic and client-server interaction.
		/// </summary>
		/// <param name="gameTime"></param>
		void UpdateClientServerGame ( GameTime gameTime )
		{
			cl.Update( gameTime );

			ui.Update( gameTime );

			ed.Update( gameTime );
		}



		public void StartServer ( string map, bool dedicated )
		{
			//	Disconnect!

			if (dedicated) {
				GameServer.Start( map, null );
			} else {
				if (GameServer.Start( map, null )) {
					GameClient.Connect( "127.0.0.1", Network.Port );
				}
			}
		}


		public void KillServer ()
		{
			GameServer.Kill();
		}



		public void Connect ( string host, int port )
		{
			GameClient.Connect(host, port);
			//	Kill server!
		}


		public void Disconnect ( string message )
		{
			GameClient.Disconnect(message);
			//	Kill server!
		}
	}
}
