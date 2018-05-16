﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion;
using Fusion.Build;
using Fusion.Development;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Core.Shell;
using Fusion.Core.Utils;
using Fusion.Engine.Imaging;
using IronStar.Editors;
using IronStar.Core;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using Fusion.Build.Mapping;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using IronStar.Editor2;
using Fusion.Core.Mathematics;

namespace IronStar {

	class Program {

		class IronStarGame : Game {

			public IronStarGame ( ) : base("IronStar")
			{
			}

			/*public override void LoadConfig()
			{
				Config.LoadSettings("Config.ini");
			}

			public override void SaveConfig()
			{
				Config.SaveSettings("Config.ini");
			} */

			/*public override IClientInstance CreateClient( Game game, IMessageService msgsvc, Guid clientGuid )
			{
				return new ShooterClient( game.GameClient, msgsvc, clientGuid );
			}

			public override IServerInstance CreateServer( Game game, IMessageService msgsvc, string map, string options )
			{
				return new ShooterServer( game.GameServer, msgsvc, map );
			}

			public override IUserInterface CreateUI( Game game )
			{
				return new ShooterInterface( game );
			}

			public override IEditorInstance CreateEditor( Game game, string map )
			{
				return new Editor2.MapEditor( game.GameEditor, map );
			}  */
		}



		[Command("editor")]
		static string Editor_f ( string[] args )
		{
			Editor.Run( Game.Instance );
			return null;
		}



		[STAThread]
		static int Main ( string[] args )
		{
			// 	colored console output :
			Log.AddListener( new ColoredLogListener() );

			//	output for in-game console :
			Log.AddListener( new LogRecorder() );

			//	set verbosity :
			Log.VerbosityLevel = LogMessageType.Verbose;

			/*Allocator2D.RunTest(512, 1024, @"E:\GITHUB\_alloc_test");
			return 0; */

			//
			//	Build content on startup.
			//	Remove this line in release code.
			//
			Builder.Options.InputDirectory = @"..\..\..\..\Content";
			Builder.Options.TempDirectory = @"..\..\..\..\Temp";
			Builder.Options.OutputDirectory = @"Content";
			Builder.SafeBuild();


			//
			//	Run game :
			//
			using (var game = new IronStarGame()) {

				//	enable and disable debug direct3d device :
				game.RenderSystem.UseDebugDevice = false;
				game.RenderSystem.Fullscreen	= false;

				//	enable and disable object tracking :
				game.TrackObjects = false;

				//	set game title :
				game.GameTitle = "IronStar";

				//	apply command-line options here:
				//	...
				if (!LaunchBox.ShowDialog(game, "Config.ini", ()=>Editor.Run(game))) {
					return 0;
				}

				//	run:
				game.Run();

				//	close editors :
				Editor.CloseAll();
			}

			return 0;
		}
	}
}
