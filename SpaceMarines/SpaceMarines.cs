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
using Fusion.Build;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Common;
using System.IO;
using System.Runtime.CompilerServices;
using Fusion.Core.Input;
using SpaceMarines.SFX;

namespace SpaceMarines {
	partial class SpaceMarines : Game
	{
		const string ConfigFile = "Config.ini";

		[MethodImpl(MethodImplOptions.NoOptimization)]
		public SpaceMarines() : base("SpaceMarines", "SpaceMarines")
		{
			this.Exiting += IronStarGame_Exiting;
			this.Components.ComponentAdded += Components_ComponentAdded;
			this.Components.ComponentRemoved += Components_ComponentRemoved;

			this.Config.LoadSettings(ConfigFile);

			this.AddServiceAndComponent( new RenderSystem(this, false) );
			this.AddServiceAndComponent( new SoundSystem(this) );
			this.AddServiceAndComponent( new FrameProcessor(this) );
			this.AddServiceAndComponent( new GameConsole( this ) );
			this.AddServiceAndComponent( new Network( this ) );
			this.AddServiceAndComponent( new GameClient( this ) );
			this.AddServiceAndComponent( new GameServer( this ) );
			this.AddServiceAndComponent( new JsonFactory( this ) );

			this.GetService<FrameProcessor>().LayerOrder = 100;
			this.GetService<GameConsole>().LayerOrder = 200;

			this.AddServiceAndComponent( new ViewWorld(this) );
			this.AddServiceAndComponent( new SinglePlayer(this) );

			Invoker.RegisterCommand("map",				() => new MapCommand(this) );
			/*Invoker.RegisterCommand("killEditor",		() => new KillEditorCommand(this) );
			Invoker.RegisterCommand("killServer",		() => new KillServerCommand(this) );
			Invoker.RegisterCommand("connect",			() => new ConnectCommand(this) );
			Invoker.RegisterCommand("disconnect",		() => new DisconnectCommand(this) );*/
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
			this.GetService<GameClient>()?.Wait();
			this.GetService<GameServer>()?.Wait();

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

			if (e.Key==Keys.F11) {
				this.GetService<RenderSystem>().Screenshot();
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


			/*if (e.Key==Keys.F1) {

				var frames = this.GetService<FrameProcessor>();
				var parent = frames.RootFrame;
				
				if (assetExplorer==null) {

					assetExplorer		= MapEditor.CreateAssetExplorer( parent );
					frames.TargetFrame	= assetExplorer;

				} else {

					assetExplorer.Visible = true;
					frames.TargetFrame	= assetExplorer;
					
				}
			} */

		}


		protected override void Dispose(bool disposing)
		{
			if (disposing) {
			
			}
			base.Dispose(disposing);
		}


		Frame assetExplorer;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Invoker.ExecuteDeferredCommands();
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Stuff
		 * 
		-----------------------------------------------------------------------------------------------*/

		void Map ( string mapname )
		{
			this.GetService<SinglePlayer>().StartMap( mapname );
		}
	}
}
