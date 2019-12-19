using Fusion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using Fusion.Engine.Frames;
using Fusion.Engine.Tools;
using Fusion;
using Fusion.Core.Shell;
using IronStar.Editor;
using Fusion.Build;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Common;
using IronStar.SFX;
using System.IO;
using System.Runtime.CompilerServices;
using Fusion.Core.Input;
using Fusion.Widgets;
using IronStar.SinglePlayer;

namespace IronStar {
	partial class IronStar : Game
	{
		const string ConfigFile = "Config.ini";

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public IronStar() : base("IronStar", "IronStar")
		{
			this.Exiting += IronStarGame_Exiting;
			this.Components.ComponentAdded += Components_ComponentAdded;
			this.Components.ComponentRemoved += Components_ComponentRemoved;

			this.Config.LoadSettings(ConfigFile);

			this.AddServiceAndComponent( 100, new RenderSystem(this, true) );
			this.AddServiceAndComponent( 200, new SoundSystem(this) );
			this.AddServiceAndComponent( 300, new GameConsole( this ) );
			this.AddServiceAndComponent( 900, new FrameProcessor(this) );
			this.AddServiceAndComponent( 400, new Network( this ) );
			this.AddServiceAndComponent( 500, new GameClient( this ) );
			this.AddServiceAndComponent( 600, new GameServer( this ) );
			this.AddServiceAndComponent( 800, new UserInterface( this, new ShooterInterface(this) ) );
			this.AddServiceAndComponent( 700, new Mission( this ) );
			this.AddServiceAndComponent(1000, new JsonFactory( this ) );

			this.GetService<FrameProcessor>().LayerOrder = 100;
			this.GetService<GameConsole>().LayerOrder = 200;

			Invoker.RegisterCommand("map",				() => new MapCommand(this) );
			Invoker.RegisterCommand("killEditor",		() => new KillEditorCommand(this) );
			Invoker.RegisterCommand("killServer",		() => new KillServerCommand(this) );
			Invoker.RegisterCommand("connect",			() => new ConnectCommand(this) );
			Invoker.RegisterCommand("disconnect",		() => new DisconnectCommand(this) );
			Invoker.RegisterCommand("contentBuild",		() => new ContentBuildCommand(this) );
			Invoker.RegisterCommand("contentFile",		() => new ContentFileCommand() );
			Invoker.RegisterCommand("contentReport",	() => new ContentReportCommand() );
		}


		private void Components_ComponentAdded(object sender, GameComponentCollectionEventArgs e)
		{
			var name = e.GameComponent.GetType().Name;
			Log.Message("Component added: {0}", name );
			this.Config.ApplySettings( name, e.GameComponent );
		}


		private void Components_ComponentRemoved(object sender, GameComponentCollectionEventArgs e)
		{
			var name = e.GameComponent.GetType().Name;
			Log.Message("Component removed: {0}", name );
			this.Config.RetrieveSettings( name, e.GameComponent );
		}


		private void IronStarGame_Exiting(object sender, EventArgs e)
		{
			//	wait for server and client tasks, 
			//	stop game if neccessary
			this.GetService<GameClient>().Wait();
			this.GetService<GameServer>().Wait();

			//	save components' configuration
			foreach ( var component in Components ) {
				this.Config.RetrieveSettings( component.GetType().Name, component );
			}

			//	save settings to file and unload content
			this.Config.SaveSettings(ConfigFile);
			this.Content.Unload();
		}


		protected override void Initialize()
		{
			base.Initialize();

			Mouse.SetCursorImage( Properties.Resources.cursor );
			
			var ss = this.GetService<SoundSystem>();
			ss.LoadSoundBank(Content, @"audio\desktop\master.strings"	);
			ss.LoadSoundBank(Content, @"audio\desktop\master"			);
			ss.LoadSoundBank(Content, @"audio\desktop\env"				);
			ss.LoadSoundBank(Content, @"audio\desktop\music"			);
			ss.LoadSoundBank(Content, @"audio\desktop\vo"				);

			// HACK : preload editor fonts...
			ColorTheme.NormalFont	=	Content.Load<SpriteFont>(@"fonts\editorOpenSans");
			ColorTheme.BoldFont		=	Content.Load<SpriteFont>(@"fonts\editorOpenSansBold");
			ColorTheme.Monospaced	=	Content.Load<SpriteFont>(@"fonts\editorInconsolata");

			Keyboard.KeyDown +=Keyboard_KeyDown;
		}



		private void Keyboard_KeyDown( object sender, KeyEventArgs e )
		{
			if (e.Key==Keys.F5) {
				throw new NotImplementedException();
				// >>>>>>> Builder.SafeBuild(false, null, null);
				Reload();	
			}

			if (e.Key==Keys.F11) {
				this.GetService<RenderSystem>().Screenshot();
			}

			if (e.Key==Keys.F2) {
				
				var vsync = this.GetService<RenderSystem>().VSyncInterval;

				if (vsync==0) {
					vsync = 1;
				} else {	
					vsync = 0;
				}

				this.GetService<RenderSystem>().VSyncInterval = vsync;
			}


			if (e.Key==Keys.F1) {

				var frames = this.GetService<FrameProcessor>();
				var parent = frames.RootFrame;
				
				if (assetExplorer==null) {

					assetExplorer		= MapEditor.CreateAssetExplorer( parent );
					frames.TargetFrame	= assetExplorer;

				} else {

					assetExplorer.Visible = true;
					frames.TargetFrame	= assetExplorer;
					
				}
			}

		}


		protected override void Dispose(bool disposing)
		{
			if (disposing) {
			
			}
			base.Dispose(disposing);
		}


		Frame assetExplorer;

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Invoker.ExecuteDeferredCommands();
		}





		protected void StartEditor ( string mapname )
		{
			var editor = new MapEditor( this, mapname );
			editor.Initialize();

			this.AddServiceAndComponent( editor );
		}


		protected void StopEditor ( )
		{
			//	try to stop editor :
			var editor = this.GetService<MapEditor>();

			if (editor!=null) {
				Log.Message("Stopping map editor...");
				Services.RemoveService( editor.GetType() );
				Components.Remove( editor );
				SafeDispose( ref editor );
			} else {
				Log.Warning("Editor is not running");
			}
		}


		protected void StartServer ( string mapname, bool dedicated )
		{
			var sv = this.GetService<GameServer>();
			var cl = this.GetService<GameClient>();
			var nt = this.GetService<Network>();

			var svInstance	=	new ShooterServer( sv, null, mapname );

			if (dedicated) {
				sv.Start( svInstance );
			} else {
				if (sv.Start( svInstance )) {
					Connect( "127.0.0.1", nt.Port );
				}
			}
		}


		protected void KillServer ()
		{
			this.GetService<GameServer>().Kill();
		}



		protected void Connect ( string host, int port )
		{
			var cl = this.GetService<GameClient>();
			var nt = this.GetService<Network>();

			var clInstance	=	new ShooterClient( cl, null, Guid.NewGuid() );

			if (!this.GetService<GameClient>().Connect(host, port, clInstance)) {
				clInstance.Dispose();
			}
		}


		protected void Disconnect ( string message )
		{
			this.GetService<GameClient>().Disconnect(message);
		}
	}
}
