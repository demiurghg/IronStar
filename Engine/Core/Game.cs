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
using Fusion.Core.Extensions;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Lidgren.Network;
using Fusion.Engine.Audio;
using Fusion.Engine.Tools;
using Fusion.Engine.Frames;
using System.ComponentModel;
using Fusion.Build;
using Fusion.Engine.Common;

namespace Fusion.Core {

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
		public GraphicsDevice GraphicsDevice { get { return graphicsDevice; } }

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
		/// Gets game component collection
		/// </summary>
		public GameComponentCollection Components { get; private set; } = new GameComponentCollection();

		/// <summary>
		/// Gets game services
		/// </summary>
		public GameServiceContainer Services { get; private set; } = new GameServiceContainer();

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
		}
		string gameTitle;

		/// <summary>
		/// Enable COM object tracking
		/// </summary>
		public bool TrackObjects {
			get {
				return SharpDX.Configuration.EnableObjectTracking;
			} 
			set {
				SharpDX.Configuration.EnableObjectTracking = value;
				SharpDX.Configuration.UseThreadStaticObjectTracking = true;
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


		volatile bool	initialized		=	false;
		volatile bool	requestExit		=	false;
		volatile bool	requestReload	=	false;

		internal bool ExitRequested { get { return requestExit; } }


		ConfigManager		config			;
		InputDevice			inputDevice		;
		GraphicsDevice		graphicsDevice	;
		ContentManager		content			;
		Invoker				invoker			;
		Keyboard			keyboard		;
		Mouse				mouse			;
		Touch				touch			;
		GamepadCollection	gamepads		;
		UserStorage			userStorage		;


		GameTime	gameTimeInternal;


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
			RenderLoop.Run( GraphicsDevice.Display.Window, UpdateAndDrawInternal );

			//	call exit event :
			Exiting?.Invoke( this, EventArgs.Empty );

			//	unload content :
			Content.Unload();
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
		public Game ( string gameId, string gameTitle )
		{
			this.gameTitle	=	gameTitle;
			this.gameId		=	gameId;
			Enabled			=	true;

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.UnhandledException += currentDomain_UnhandledException;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			CultureInfo.DefaultThreadCurrentCulture	=	CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentCulture		=	CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture	=	CultureInfo.InvariantCulture;


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

			invoker				=	new Invoker( this );
			inputDevice			=	new InputDevice( this );
			graphicsDevice		=	new GraphicsDevice( this );
			content				=	new ContentManager( this );
			gameTimeInternal	=	GameTime.Start();

			keyboard			=	new Keyboard(this);
			mouse				=	new Mouse(this);
			touch				=	new Touch(this);
			gamepads			=	new GamepadCollection(this);

			invoker.RegisterCommand("quit", () => new Quit(this) );
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
				Log.Warning("Game is not initialized");
			} else {
				requestReload = true;
			}
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


		volatile bool requestFullscreenOnStartup = false;



		/// <summary>
		/// InitInternal
		/// </summary>
		internal bool InitInternal ()
		{
			Log.Message("");
			Log.Message("-------- Game Initializing --------");

			var p = new GraphicsParameters();

			var rs = Services.GetService<IRenderSystem>();

			if (rs==null) {
				throw new InvalidOperationException("There is no IRenderSystem");
			}

			rs.ApplyParameters( ref p );

			//	going to fullscreen immediatly on startup lead to 
			//	bugs and inconsistnecy for different stereo modes,
			//	so we store fullscreen mode and apply it on next update step.
			requestFullscreenOnStartup	=	p.FullScreen;
			p.FullScreen = false;

			//	initialize drivers :
			graphicsDevice.Initialize(p);
			inputDevice.Initialize();
			keyboard.Initialize();
			mouse.Initialize();
			touch.Initialize();

			Initialize();

			initialized	=	true;

			Log.Message("-----------------------------------------");
			Log.Message("");

			return true;
		}



		/// <summary>
		/// Initialize games
		/// </summary>
		protected virtual void Initialize ()
		{
			Invoker.ExecuteDeferredCommands();
		
			var components = Components.ToArrayThreadSafe();

			foreach ( var component in components ) 
			{
				Log.Message("Initialize :  {0}", component.GetType().Name);
				component.Initialize();
			}
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

			if (disposing) {

				Log.Message("");
				Log.Message("-------- Game Shutting Down --------");

				for (int i=Components.Count-1; i>=0; i-- ) {
					Log.Message("Dispose : {0}", Components[i].GetType().Name );
					(Components[i] as IDisposable)?.Dispose();
				}
				Components.Clear();

				SafeDispose( ref touch );
				SafeDispose( ref keyboard );
				SafeDispose( ref mouse );
				SafeDispose( ref inputDevice );
				SafeDispose( ref graphicsDevice );

				Log.Message("------------------------------------------");
				Log.Message("");
			}

			base.Dispose(disposing);

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
		internal void UpdateAndDrawInternal ()
		{
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
					graphicsDevice.FullScreen = true;
					requestFullscreenOnStartup = false;
				}

				if (requestReload) {
					Reloading?.Invoke( this, EventArgs.Empty );
					requestReload = false;
				}

				graphicsDevice.Prepare();
				graphicsDevice.Display.Prepare();

				//	pre update :
				gameTimeInternal = gameTimeInternal.Next();
				InputDevice.UpdateInput();

				//	update game components :
				Update( gameTimeInternal );
												
				//
				//	Render :
				//
				DrawInternal( gameTimeInternal );

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
		/// Updates game 
		/// </summary>
		protected virtual void Update ( GameTime gameTime )
		{
			var components  =	Components.ToArrayThreadSafe();

			var updatables  =	components
								.Select( c1 => c1 as IUpdatable )
								.Where ( c2 => c2 != null )
								.OrderBy( c3 => c3.UpdateOrder )
								.ToArray();

			foreach ( var updatable in updatables ) {
				updatable.Update( gameTime );
			}
		}



		/// <summary>
		/// Called when the game determines it is time to draw a frame.
		/// In stereo mode this method will be called twice to render left and right parts of stereo image.
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="stereoEye"></param>
		void DrawInternal ( GameTime gameTime )
		{
			var eyeList	= graphicsDevice.Display.StereoEyeList;

			var rs = this.GetService<IRenderSystem>();

			foreach ( var eye in eyeList ) {

				GraphicsDevice.ResetStates();
				GraphicsDevice.Display.TargetEye = eye;
				GraphicsDevice.RestoreBackbuffer();

				GraphicsDevice.ClearBackbuffer(Color.Zero);

				rs.RenderView( gameTime, eye );
			}

			GraphicsDevice.ResetStates();
			GraphicsDevice.RestoreBackbuffer();

			GraphicsDevice.Present(rs.VSyncInterval);
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



		/// <summary>
		/// Quit command
		/// </summary>
		class Quit : ICommand {
			public void Rollback() {}
			public bool IsHistoryOn() { return false; }
			readonly Game game;

			public Quit ( Game game )
			{
				this.game = game;
			}

			public object Execute()
			{
				game.Exit();
				return null;
			}
		}



		public SoundSystem SoundSystem { get { return Services.GetService<SoundSystem>(); } }
		public RenderSystem RenderSystem { get { return Services.GetService<RenderSystem>(); } }
		public GameConsole Console { get { return Services.GetService<GameConsole>(); } }
		public Network Network { get { return Services.GetService<Network>(); } }
		public FrameProcessor Frames { get { return Services.GetService<FrameProcessor>(); } }
	}
}
