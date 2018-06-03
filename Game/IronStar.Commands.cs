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
		class MapCommand : CommandNoHistory {

			readonly IronStar game;
			readonly string mapname;
			readonly bool edit;
			readonly bool dedicated;

			public MapCommand ( IronStar game, ArgList args )
			{
				this.game	=	game;

				args.Usage("map <mapname> [/edit]")
					.Require("mapname"		, out mapname	)
					.Option	("/edit"		, out edit		)
					.Option ("/dedicated"	, out dedicated )
					.Apply();
			}

			public override object Execute()
			{
				if (edit) {
					game.StartEditor(mapname);
				} else {
					game.StartServer(mapname, dedicated);
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
				game.KillServer();
				return null;
			}
		}



		class ConnectCommand : CommandNoHistory {

			readonly IronStar game;
			readonly string host;
			readonly int port;

			public ConnectCommand ( IronStar game, ArgList args )
			{
				this.game = game;

				args.Usage("connect <host> <port>")
					.Require("host", out host)
					.Require("port", out port)
					.Apply();
			}

			public override object Execute()
			{
				game.Connect( host, port );
				return null;
			}
		}



		class DisconnectCommand : CommandNoHistory {

			readonly IronStar game;

			public DisconnectCommand ( IronStar game, ArgList args )
			{
				this.game = game;
			}

			public override object Execute()
			{
				game.Disconnect( "disconnect by user request" );
				return null;
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



		class ContentBuildCommand : CommandNoHistory {
			Game	 game;
			bool	 force;
			string[] files;
			string	 clean;

			public ContentBuildCommand ( Game game, ArgList args )
			{
				this.game	=	game;
				this.force	=	args.Contains("/force");
				this.files	=	args.Skip(1).Where( s=>!s.StartsWith("/") ).ToArray();
				this.clean	=	args.FirstOrDefault( a => a.StartsWith("/clean:"))?.Replace("/clean:","");
			}

			public override object Execute ()
			{
				Builder.SafeBuild(force, clean, files);
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
			string reportFile;

			public ContentReportCommand( ArgList args )
			{
				if (args.Count<2) {
					throw new Exception("Missing command line arguments: filename");
				}
				reportFile = args[1];
			}

			public override object Execute() 
			{
				Builder.OpenReport( reportFile );
				return null;
			}

		}
	}
}
