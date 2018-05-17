﻿using Fusion.Core;
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

namespace IronStar {
	class IronStarGame : Game
	{
		const string ConfigFile = "Config.ini";

		public IronStarGame() : base("IronStar", "IronStar")
		{
			this.Exiting += IronStarGame_Exiting;
			this.Components.ComponentAdded += Components_ComponentAdded;
			this.Components.ComponentRemoved += Components_ComponentRemoved;

			this.Config.LoadSettings(ConfigFile);

			this.AddServiceAndComponent( new RenderSystem(this) );
			this.AddServiceAndComponent( new SoundSystem(this) );
			this.AddServiceAndComponent( new FrameProcessor(this) );
			this.AddServiceAndComponent( new GameConsole(this) );
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



		class Map : ICommand {
			public void Rollback() {}
			public bool IsHistoryOn() { return false; }

			readonly IronStarGame game;
			readonly string mapname;
			readonly bool edit;

			public Map ( IronStarGame game, ArgList args )
			{
				this.game	=	game;

				args.Usage("map <mapname> [/edit]")
					.Require("mapname"	, out mapname	)
					.Option	("edit"		, out edit )
					.Apply();
			}

			public object Execute()
			{
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
