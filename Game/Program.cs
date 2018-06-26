using System;
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
using IronStar.Core;
using IronStar.Mapping;
using Fusion.Core.Extensions;
using Fusion.Build.Mapping;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using IronStar.Editor;
using Fusion.Core.Mathematics;

namespace IronStar {

	class Program {

		[STAThread]
		static int Main ( string[] args )
		{
			// 	colored console output :
			Log.AddListener( new ColoredLogListener() );

			//	output for in-game console :
			Log.AddListener( new LogRecorder() );

			//	set verbosity :
			Log.VerbosityLevel = LogMessageType.Verbose;

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
			using (var game = new IronStar()) {

				//	enable and disable debug direct3d device :
				game.RenderSystem.UseDebugDevice = false;
				game.RenderSystem.Fullscreen	= false;

				//	enable and disable object tracking :
				game.TrackObjects = false;

				//	apply command-line options here:
				//	...
				if (!LaunchBox.ShowDialog(game, "Config.ini", ()=>Log.Warning("Editor is in-game only"))) {
					return 0;
				}

				//	run:
				game.Run();
			}

			return 0;
		}
	}
}
