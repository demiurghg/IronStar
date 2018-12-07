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

namespace SpaceMarines {
	partial class SpaceMarines : Game
	{
		class MapCommand : CommandNoHistory {

			readonly SpaceMarines game;

			[CommandLineParser.Name("mapname")]
			[CommandLineParser.Required]
			public string MapName { get; set; }


			public MapCommand ( SpaceMarines game )
			{
				this.game	=	game;
			}


			public override object Execute()
			{
				game.Map( MapName );

				return null;
			}

		}

		class ContentBuildCommand : CommandNoHistory {
			Game	 game;
			
			[CommandLineParser.Option]
			[CommandLineParser.Name("force")]
			public bool	Force { get; set; }

			[CommandLineParser.Option]
			[CommandLineParser.Name("clean")]
			public string Clean { get; set; }

			public ContentBuildCommand ( Game game )
			{
				this.game	=	game;
			}

			public override object Execute ()
			{
				Builder.SafeBuild( Force, Clean, new string[0] );
				game.Reload();	
				return null;		
			}
		}



		class ContentFileCommand : CommandNoHistory {
			public override object Execute()
			{
				return Builder.Options.ContentIniFile;
			}
		}



		class ContentReportCommand : CommandNoHistory {

			[CommandLineParser.Option]
			[CommandLineParser.Name("reportFile")]
			public string ReportFile { get; set; }


			public ContentReportCommand()
			{
			}

			public override object Execute() 
			{
				Builder.OpenReport( ReportFile );
				return null;
			}

		}
	}
}
