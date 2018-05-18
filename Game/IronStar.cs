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

namespace IronStar {
	class IronStar : Game
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

			Invoker.RegisterCommand("map",			(args) => new MapCommand(this, args) );
			Invoker.RegisterCommand("killeditor",	(args) => new KillEditorCommand(this, args) );
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



		class MapCommand : CommandNoHistory {

			readonly IronStar game;
			readonly string mapname;
			readonly bool edit;

			public MapCommand ( IronStar game, ArgList args )
			{
				this.game	=	game;

				args.Usage("map <mapname> [/edit]")
					.Require("mapname"	, out mapname	)
					.Option	("edit"		, out edit )
					.Apply();
			}

			public override object Execute()
			{
				if (edit) {
					game.RunEditor(mapname);
				}
				return null;
			}

		}


		class KillServerCommand : CommandNoHistory {

			readonly IronStar game;

			public KillServerCommand ( IronStar game, ArgList args )
			{
				this.game = game;
			}

			public override object Execute()
			{
				throw new NotImplementedException();
			}
		}


		class KillEditorCommand : CommandNoHistory {

			readonly IronStar game;

			public KillEditorCommand ( IronStar game, ArgList args )
			{
				this.game = game;
			}

			public override object Execute()
			{
				game.KillEditor();
				return null;
			}
		}

		//class EditorMap : ICommand {
		//	public void Rollback() {}
		//	public bool IsHistoryOn() { return false; }

		//	public object Execute()
		//	{
		//		throw new NotImplementedException();
		//	}
		//}


		//class EditorQuit : ICommand {
		//	public void Rollback() {}
		//	public bool IsHistoryOn() { return false; }

		//	public object Execute()
		//	{
		//		throw new NotImplementedException();
		//	}
		//}
	}
}
