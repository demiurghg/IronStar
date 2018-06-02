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
using IronStar.Editor2;
using Fusion.Build;

namespace IronStar {
	partial class IronStar : Game
	{
		const string ConfigFile = "Config.ini";

		public IronStar() : base("IronStar", "IronStar")
		{
			this.Exiting += IronStarGame_Exiting;
			this.Components.ComponentAdded += Components_ComponentAdded;
			this.Components.ComponentRemoved += Components_ComponentRemoved;

			this.Config.LoadSettings(ConfigFile);

			this.AddServiceAndComponent( new RenderSystem(this) );
			this.AddServiceAndComponent( new SoundSystem(this) );
			this.AddServiceAndComponent( new FrameProcessor(this) );
			this.AddServiceAndComponent( new GameConsole(this) );

			this.GetService<FrameProcessor>().LayerOrder = 100;
			this.GetService<GameConsole>().LayerOrder = 200;

			Invoker.RegisterCommand("map",				(args) => new MapCommand(this, args) );
			Invoker.RegisterCommand("killEditor",		(args) => new KillEditorCommand(this, args) );
			Invoker.RegisterCommand("killGame",			(args) => new KillGameCommand(this, args) );
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
			foreach ( var component in Components ) {
				this.Config.RetrieveSettings( component.GetType().Name, component );
			}

			this.Config.SaveSettings(ConfigFile);
			this.Content.Unload();
		}


		protected override void Initialize()
		{
			base.Initialize();
		}


		protected override void Dispose(bool disposing)
		{
			if (disposing) {
			
			}
			base.Dispose(disposing);
		}


		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}


		protected void RunEditor ( string mapname )
		{
			var editor = new MapEditor( this, mapname );
			editor.Initialize();

			this.AddServiceAndComponent( editor );
		}


		protected void KillEditor ( )
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


		protected void RunGame ( string mapname )
		{
			var sp = new SinglePlayer(this, mapname);
			sp.Initialize();

			this.AddServiceAndComponent(sp);
		}


		protected void KillGame ()
		{
			var sp = this.GetService<SinglePlayer>();

			if (sp!=null) {
				Log.Message("Stopping game...");
				Services.RemoveService( sp.GetType() );
				Components.Remove( sp );
				SafeDispose( ref sp );
			} else {
				Log.Warning("Game is not running");
			}
		}
	}
}
