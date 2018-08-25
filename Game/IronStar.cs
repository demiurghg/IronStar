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

			this.AddServiceAndComponent( new RenderSystem(this) );
			this.AddServiceAndComponent( new SoundSystem(this) );
			this.AddServiceAndComponent( new FrameProcessor(this) );
			this.AddServiceAndComponent( new GameConsole( this ) );
			this.AddServiceAndComponent( new Network( this ) );
			this.AddServiceAndComponent( new GameClient( this ) );
			this.AddServiceAndComponent( new GameServer( this ) );
			this.AddServiceAndComponent( new Factory( this ) );

			this.GetService<FrameProcessor>().LayerOrder = 100;
			this.GetService<GameConsole>().LayerOrder = 200;

			Invoker.RegisterCommand("map",				(args) => new MapCommand(this, args) );
			Invoker.RegisterCommand("killEditor",		(args) => new KillEditorCommand(this, args) );
			Invoker.RegisterCommand("killServer",		(args) => new KillServerCommand(this, args) );
			Invoker.RegisterCommand("connect",			(args) => new ConnectCommand(this, args) );
			Invoker.RegisterCommand("disconnect",		(args) => new DisconnectCommand(this, args) );
			Invoker.RegisterCommand("contentBuild",		(args) => new ContentBuildCommand(this, args) );
			Invoker.RegisterCommand("contentFile",		(args) => new ContentFileCommand() );
			Invoker.RegisterCommand("contentReport",	(args) => new ContentReportCommand(args) );
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

			Keyboard.KeyDown +=Keyboard_KeyDown;
		}


		private void Keyboard_KeyDown( object sender, KeyEventArgs e )
		{
			if (e.Key==Keys.F5) {
				Builder.SafeBuild(false, null, null);
				Reload();	
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
				
				if (assetExplorer!=null) {
					var frames = this.GetService<FrameProcessor>().RootFrame;
					assetExplorer = MapEditor.CreateAssetExplorer( frames );
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


		protected void StartLevel ( string mapname )
		{
			var campaign = new ShooterCampaign( this, mapname );
			this.AddServiceAndComponent( campaign );
			campaign.Initialize();
		}



		protected void StopLevel ()
		{
			//	try to stop editor :
			var campaign = this.GetService<ShooterCampaign>();

			if (campaign!=null) {
				Log.Message("Stopping map campaign...");
				Services.RemoveService( campaign.GetType() );
				Components.Remove( campaign );
				SafeDispose( ref campaign );
			} else {
				Log.Warning("Campaign is not running");
			}
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
